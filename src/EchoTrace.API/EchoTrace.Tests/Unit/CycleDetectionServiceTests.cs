using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;
using EchoTrace.Infrastructure.Services;
using FluentAssertions;
using NSubstitute;

namespace EchoTrace.Tests.Unit;

public class CycleDetectionServiceTests
{
    private readonly ISupplyChainRepository _repo = Substitute.For<ISupplyChainRepository>();
    private readonly CycleDetectionService _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CycleDetectionServiceTests()
    {
        _sut = new CycleDetectionService(_repo);
    }

    private void GivenExistingEdges(params (Guid Parent, Guid Child)[] edges)
    {
        var entities = edges
            .Select(e => SupplyChainEdge.Create(_tenantId, e.Parent, e.Child))
            .ToList();
        _repo.GetActiveEdgesByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(entities);
    }

    [Fact]
    public async Task No_existing_edges_never_creates_a_cycle()
    {
        GivenExistingEdges();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var result = await _sut.WouldCreateCycleAsync(a, b, _tenantId);

        result.WouldCreateCycle.Should().BeFalse();
    }

    [Fact]
    public async Task Direct_reversal_of_an_existing_edge_is_a_cycle()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        GivenExistingEdges((a, b));

        var result = await _sut.WouldCreateCycleAsync(b, a, _tenantId);

        result.WouldCreateCycle.Should().BeTrue();
    }

    [Fact]
    public async Task Chain_of_depth_ten_closing_back_to_the_root_is_detected()
    {
        var nodes = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();
        var edges = nodes.Zip(nodes.Skip(1), (p, c) => (p, c)).ToArray();
        GivenExistingEdges(edges);

        // Proposing the edge that closes the loop: last node back to the first.
        var result = await _sut.WouldCreateCycleAsync(nodes[^1], nodes[0], _tenantId);

        result.WouldCreateCycle.Should().BeTrue();
    }

    [Fact]
    public async Task Chain_of_depth_ten_extended_further_is_not_a_cycle()
    {
        var nodes = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();
        var edges = nodes.Zip(nodes.Skip(1), (p, c) => (p, c)).ToArray();
        GivenExistingEdges(edges);

        var newNode = Guid.NewGuid();
        var result = await _sut.WouldCreateCycleAsync(nodes[^1], newNode, _tenantId);

        result.WouldCreateCycle.Should().BeFalse();
    }

    [Fact]
    public async Task Disconnected_branch_reversal_is_not_a_cycle()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var x = Guid.NewGuid();
        var y = Guid.NewGuid();
        GivenExistingEdges((a, b), (x, y));

        // Proposing y -> b: b has no path back to y since {a,b} and {x,y} are disconnected.
        var result = await _sut.WouldCreateCycleAsync(y, b, _tenantId);

        result.WouldCreateCycle.Should().BeFalse();
    }
}
