namespace InfoFlowNavigator.Domain.Entities;

public sealed record Entity(
    Guid Id,
    string Name,
    string EntityType,
    string? Summary = null);
