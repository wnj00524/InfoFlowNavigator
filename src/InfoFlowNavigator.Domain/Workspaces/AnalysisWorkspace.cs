using InfoFlowNavigator.Domain.Entities;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Sources;

namespace InfoFlowNavigator.Domain.Workspaces;

public sealed record AnalysisWorkspace(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<Entity> Entities,
    IReadOnlyList<Relationship> Relationships,
    IReadOnlyList<Event> Events,
    IReadOnlyList<SourceReference> Sources)
{
    public static AnalysisWorkspace CreateNew(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workspace name is required.", nameof(name));
        }

        return new AnalysisWorkspace(
            Guid.NewGuid(),
            name.Trim(),
            DateTimeOffset.UtcNow,
            [],
            [],
            [],
            []);
    }
}
