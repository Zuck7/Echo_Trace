using MediatR;

namespace EchoTrace.Application.SupplyChain.Commands.RemoveEdge;

public record RemoveEdgeCommand(Guid EdgeId) : IRequest;
