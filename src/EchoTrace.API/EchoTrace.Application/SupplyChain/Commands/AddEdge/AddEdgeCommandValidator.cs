using FluentValidation;

namespace EchoTrace.Application.SupplyChain.Commands.AddEdge;

public class AddEdgeCommandValidator : AbstractValidator<AddEdgeCommand>
{
    private static readonly string[] ValidRelationshipTypes = ["SOURCES", "MANUFACTURES", "DISTRIBUTES"];

    public AddEdgeCommandValidator()
    {
        RuleFor(x => x.ChildOrgId)
            .NotEmpty().WithMessage("ChildOrgId is required.");

        RuleFor(x => x.RelationshipType)
            .NotEmpty()
            .Must(r => ValidRelationshipTypes.Contains(r))
            .WithMessage($"RelationshipType must be one of: {string.Join(", ", ValidRelationshipTypes)}");
    }
}
