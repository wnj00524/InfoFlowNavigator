namespace InfoFlowNavigator.Domain.Relationships;

public sealed record Relationship(
    Guid Id,
    Guid SourceEntityId,
    Guid TargetEntityId,
    string RelationshipType,
    string? Summary = null);
