using EchoTrace.Domain.Entities;
using FluentAssertions;

namespace EchoTrace.Tests.Unit;

public class SupplyChainEdgeTests
{
    [Fact]
    public void Create_rejects_a_self_referential_edge()
    {
        var orgId = Guid.NewGuid();

        var act = () => SupplyChainEdge.Create(Guid.NewGuid(), orgId, orgId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_allows_a_normal_parent_child_edge()
    {
        var edge = SupplyChainEdge.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        edge.IsActive.Should().BeTrue();
    }
}
