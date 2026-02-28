using MediatR;

namespace EchoTrace.Application.SupplyChain.Commands.AddEdge;

public record AddEdgeCommand(
    Guid ChildOrgId,
    string RelationshipType,
    Guid? MaterialId,
    DateOnly? ValidFrom) : IRequest<AddEdgeResult>;

public record AddEdgeResult(
    Guid EdgeId,
    Guid ParentOrgId,
    Guid ChildOrgId,
    string RelationshipType,
    bool IsActive,
    DateTime CreatedAt);
