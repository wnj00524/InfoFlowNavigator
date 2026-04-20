using InfoFlowNavigator.Domain.Common;
using InfoFlowNavigator.Domain.Entities;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Relationships;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;

namespace InfoFlowNavigator.Domain.Workspaces;

public sealed record AnalysisWorkspace(
    int SchemaVersion,
    Guid Id,
    string Name,
    string? Notes,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<Entity> Entities,
    IReadOnlyList<Relationship> Relationships,
    IReadOnlyList<Event> Events,
    IReadOnlyList<WorkspaceEvidence> Evidence)
{
    public const int CurrentSchemaVersion = 1;

    public static AnalysisWorkspace CreateNew(string name, string? notes = null, IEnumerable<string>? tags = null)
    {
        var now = DateTimeOffset.UtcNow;

        return new AnalysisWorkspace(
            CurrentSchemaVersion,
            Guid.NewGuid(),
            DomainValidation.Required(name, nameof(name), "Workspace name is required."),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DomainValidation.NormalizeTags(tags),
            now,
            now,
            [],
            [],
            [],
            []);
    }

    public AnalysisWorkspace Rename(string name) =>
        this with
        {
            Name = DomainValidation.Required(name, nameof(name), "Workspace name is required."),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

    public AnalysisWorkspace AddEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (Entities.Any(existing => existing.Id == entity.Id))
        {
            throw new InvalidOperationException($"Entity '{entity.Id}' already exists in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Entities = Entities.Concat([entity]).ToArray()
        };
    }

    public AnalysisWorkspace AddRelationship(Relationship relationship)
    {
        ArgumentNullException.ThrowIfNull(relationship);

        if (!Entities.Any(entity => entity.Id == relationship.SourceEntityId))
        {
            throw new InvalidOperationException("Relationship source entity must exist in the workspace.");
        }

        if (!Entities.Any(entity => entity.Id == relationship.TargetEntityId))
        {
            throw new InvalidOperationException("Relationship target entity must exist in the workspace.");
        }

        if (Relationships.Any(existing => existing.Id == relationship.Id))
        {
            throw new InvalidOperationException($"Relationship '{relationship.Id}' already exists in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Relationships = Relationships.Concat([relationship]).ToArray()
        };
    }

    public AnalysisWorkspace Restore()
    {
        if (SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidOperationException($"Unsupported workspace schema version '{SchemaVersion}'.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Workspace name is required.");
        }

        var entityIds = Entities.Select(static entity => entity.Id).ToHashSet();

        foreach (var relationship in Relationships)
        {
            if (!entityIds.Contains(relationship.SourceEntityId) || !entityIds.Contains(relationship.TargetEntityId))
            {
                throw new InvalidOperationException("Workspace contains a relationship that references a missing entity.");
            }
        }

        return this with
        {
            Name = Name.Trim(),
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            Tags = DomainValidation.NormalizeTags(Tags),
            UpdatedAtUtc = UpdatedAtUtc == default ? CreatedAtUtc : UpdatedAtUtc
        };
    }
}
