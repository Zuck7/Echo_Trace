import cytoscape, { type Core } from 'cytoscape';
import { useEffect, useRef } from 'react';
import type { SupplyChainTree } from '../types';

const DEPTH_COLORS = ['#2563eb', '#16a34a', '#d97706', '#dc2626', '#7c3aed'];

export function SupplyChainGraph({ tree, rootOrgId }: { tree: SupplyChainTree; rootOrgId: string }) {
  const containerRef = useRef<HTMLDivElement>(null);
  const cyRef = useRef<Core | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    const elements = [
      ...tree.nodes.map((n) => ({
        data: { id: n.orgId, label: `Tier ${n.depth}\n${n.legalName}`, depth: n.depth, status: n.status },
      })),
      ...tree.edges.map((e) => ({
        data: { id: e.edgeId, source: e.parentOrgId, target: e.childOrgId, label: e.relationshipType },
      })),
    ];

    const cy = cytoscape({
      container: containerRef.current,
      elements,
      style: [
        {
          selector: 'node',
          style: {
            label: 'data(label)',
            'background-color': (ele) => DEPTH_COLORS[(ele.data('depth') as number) % DEPTH_COLORS.length],
            color: '#fff',
            'text-valign': 'center',
            'text-halign': 'center',
            'text-wrap': 'wrap',
            'text-max-width': '84px',
            'line-height': 1.3,
            width: 100,
            height: 100,
            'font-size': 10.5,
            'border-width': (ele) => (ele.data('id') === rootOrgId ? 4 : 0),
            'border-color': '#111827',
          },
        },
        {
          selector: 'edge',
          style: {
            width: 2,
            'line-color': '#94a3b8',
            'target-arrow-color': '#94a3b8',
            'target-arrow-shape': 'triangle',
            'curve-style': 'bezier',
            label: 'data(label)',
            'font-size': 9,
            color: '#475569',
          },
        },
      ],
      layout: { name: 'breadthfirst', directed: true, spacingFactor: 1.9, roots: [rootOrgId] },
    });

    cyRef.current = cy;
    return () => cy.destroy();
  }, [tree, rootOrgId]);

  return <div ref={containerRef} className="graph-canvas" />;
}
