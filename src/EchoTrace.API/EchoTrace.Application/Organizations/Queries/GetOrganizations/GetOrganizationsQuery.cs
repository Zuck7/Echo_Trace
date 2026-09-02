using EchoTrace.Application.Organizations.Queries.GetOrganizationById;
using MediatR;

namespace EchoTrace.Application.Organizations.Queries.GetOrganizations;

public record GetOrganizationsQuery : IRequest<List<OrganizationDto>>;
