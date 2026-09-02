using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.SupplyChain.Queries.GetSupplyChainTree;

public sealed class GetSupplyChainTreeQueryHandler(
    ISupplyChainRepository supplyChainRepo,
    ICurrentTenantService tenant) : IRequestHandler<GetSupplyChainTreeQuery, SupplyChainTreeResult>
{
    public Task<SupplyChainTreeResult> Handle(GetSupplyChainTreeQuery request, CancellationToken ct) =>
        supplyChainRepo.GetSupplierTreeAsync(request.OrgId, tenant.TenantId, request.Depth, ct);
}
