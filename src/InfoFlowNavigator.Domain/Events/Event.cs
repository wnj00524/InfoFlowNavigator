namespace InfoFlowNavigator.Domain.Events;

public sealed record Event(
    Guid Id,
    string Title,
    DateTimeOffset? OccurredAtUtc = null,
    string? Summary = null);
