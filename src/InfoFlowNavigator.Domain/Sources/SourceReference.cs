namespace InfoFlowNavigator.Domain.Sources;

public sealed record SourceReference(
    Guid Id,
    string Citation,
    string? Location = null,
    string? Notes = null);
