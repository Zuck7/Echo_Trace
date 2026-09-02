using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.SupplyChain.Queries.GetSupplyChainTree;

public record GetSupplyChainTreeQuery(Guid OrgId, int Depth = -1) : IRequest<SupplyChainTreeResult>;
