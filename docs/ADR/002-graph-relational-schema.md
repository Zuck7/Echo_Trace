# ADR-002: Graph-Based Relational Schema (Adjacency List + Recursive CTE)

**Date:** 2026-02-26
**Status:** Accepted
**Deciders:** Architecture Team

---

## Context

The core data structure of Echo-Trace is a supply chain graph: organizations are nodes, supplier relationships are directed edges. We need to:

1. Efficiently traverse the graph downstream (who are my suppliers' suppliers?) and upstream (who buys from me?)
2. Store rich metadata on edges (material, relationship type, validity dates)
3. Detect cycles before adding new edges
4. Support supply chains up to ~30 tiers deep with acceptable query performance (< 5 seconds P95)
5. Keep the persistence layer within our existing SQL Server 2022 infrastructure (no additional services)

## Decision

Use a **Graph-Based Relational Schema** with the **Adjacency List** pattern:

- `Organizations` table = graph nodes
- `SupplyChainEdges` table = directed edges with `ParentOrgID` and `ChildOrgID` columns
- Tree traversal via **Recursive CTEs** in SQL Server
- Cycle detection performed at the application layer using DFS before any edge is persisted

## Considered Alternatives

### Option A: Dedicated Graph Database (Neo4j, AWS Neptune)
- **Pros:** Native graph traversal, Cypher/SPARQL queries are elegant for graph problems, excellent for deep traversals
- **Cons:** Adds an entirely new database technology to the stack; increases operational complexity and cost; requires the team to learn a new query language; cross-database joins with relational data become complex; overkill for supply chains that rarely exceed depth 20

### Option B: Adjacency List + Recursive CTE (chosen)
- **Pros:** Stays within SQL Server (single database system); recursive CTEs are well-supported and fast for depth ≤ 30; edge metadata is natural (single row per edge); team already knows SQL; no additional infrastructure
- **Cons:** CTE queries can be slower than native graph DB for very deep chains (> 50 tiers — not a realistic constraint here)

### Option C: Closure Table
- **Pros:** Fastest read performance for tree queries (no recursion needed — pre-materialized paths)
- **Cons:** Every edge addition/removal requires updating O(depth²) rows in the path table; for a dynamic supply chain with frequent relationship changes, this write overhead is unacceptable

### Option D: Nested Sets
- **Pros:** Fast subtree queries
- **Cons:** The "left/right" values must be recomputed on every structural change — extremely expensive for a graph that changes frequently (invitations, edge deactivations)

## Consequences

### Positive
- Single SQL Server database for the entire Core API — simpler operations
- Recursive CTE performance is proven to be adequate for realistic supply chain depths
- Rich edge metadata (materials, validity dates, relationship type) stored naturally in `SupplyChainEdges`
- EF Core supports raw SQL / `FromSqlInterpolated` for CTE execution

### Negative
- Recursive CTE syntax is less readable than Cypher graph traversal
- Cycle detection cannot be offloaded to the database — must be handled in the application layer before every edge write

### Mitigations
- Abstract CTE queries behind repository methods; callers never write raw SQL
- Cycle detection service is well-tested and runs in < 100ms for realistic tenant graph sizes (< 10,000 nodes)
- Add a `MAXRECURSION 100` option to CTEs as a safety guard against accidental infinite loops
