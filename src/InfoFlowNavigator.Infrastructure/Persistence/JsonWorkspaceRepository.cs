using System.Text.Json;
using System.Text.Json.Serialization;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Domain.Entities;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;

namespace InfoFlowNavigator.Infrastructure.Persistence;

public sealed class JsonWorkspaceRepository : IWorkspaceRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AnalysisWorkspace> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<WorkspaceDocument>(stream, SerializerOptions, cancellationToken);

        if (document is null)
        {
            throw new InvalidDataException("Workspace file did not contain a valid workspace payload.");
        }

        return document.ToDomain().Restore();
    }

    public async Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = WorkspaceDocument.FromDomain(workspace.Restore());

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken);
    }

    private sealed record WorkspaceDocument(
        int SchemaVersion,
        Guid Id,
        string Name,
        string? Notes,
        IReadOnlyList<string> Tags,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        IReadOnlyList<EntityDocument> Entities,
        IReadOnlyList<RelationshipDocument> Relationships,
        IReadOnlyList<EventDocument> Events,
        IReadOnlyList<EvidenceDocument> Evidence)
    {
        public static WorkspaceDocument FromDomain(AnalysisWorkspace workspace) =>
            new(
                workspace.SchemaVersion,
                workspace.Id,
                workspace.Name,
                workspace.Notes,
                workspace.Tags,
                workspace.CreatedAtUtc,
                workspace.UpdatedAtUtc,
                workspace.Entities.Select(EntityDocument.FromDomain).ToArray(),
                workspace.Relationships.Select(RelationshipDocument.FromDomain).ToArray(),
                workspace.Events.Select(EventDocument.FromDomain).ToArray(),
                workspace.Evidence.Select(EvidenceDocument.FromDomain).ToArray());

        public AnalysisWorkspace ToDomain() =>
            new(
                SchemaVersion,
                Id,
                Name,
                Notes,
                Tags ?? [],
                CreatedAtUtc,
                UpdatedAtUtc,
                (Entities ?? []).Select(static entity => entity.ToDomain()).ToArray(),
                (Relationships ?? []).Select(static relationship => relationship.ToDomain()).ToArray(),
                (Events ?? []).Select(static @event => @event.ToDomain()).ToArray(),
                (Evidence ?? []).Select(static evidence => evidence.ToDomain()).ToArray());
    }

    private sealed record EntityDocument(
        Guid Id,
        string Name,
        string EntityType,
        string? Notes,
        double? Confidence,
        IReadOnlyList<string> Tags,
        IReadOnlyDictionary<string, string> Metadata,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc)
    {
        public static EntityDocument FromDomain(Entity entity) =>
            new(
                entity.Id,
                entity.Name,
                entity.EntityType,
                entity.Notes,
                entity.Confidence,
                entity.Tags,
                entity.Metadata,
                entity.CreatedAtUtc,
                entity.UpdatedAtUtc);

        public Entity ToDomain() =>
            new(
                Id,
                Name,
                EntityType,
                Notes,
                Confidence,
                Tags ?? [],
                Metadata ?? new Dictionary<string, string>(),
                CreatedAtUtc,
                UpdatedAtUtc);
    }

    private sealed record RelationshipDocument(
        Guid Id,
        Guid SourceEntityId,
        Guid TargetEntityId,
        string RelationshipType,
        string? Notes,
        double? Confidence,
        IReadOnlyList<string> Tags,
        IReadOnlyDictionary<string, string> Metadata,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc)
    {
        public static RelationshipDocument FromDomain(Relationship relationship) =>
            new(
                relationship.Id,
                relationship.SourceEntityId,
                relationship.TargetEntityId,
                relationship.RelationshipType,
                relationship.Notes,
                relationship.Confidence,
                relationship.Tags,
                relationship.Metadata,
                relationship.CreatedAtUtc,
                relationship.UpdatedAtUtc);

        public Relationship ToDomain() =>
            new(
                Id,
                SourceEntityId,
                TargetEntityId,
                RelationshipType,
                Notes,
                Confidence,
                Tags ?? [],
                Metadata ?? new Dictionary<string, string>(),
                CreatedAtUtc,
                UpdatedAtUtc);
    }

    private sealed record EventDocument(
        Guid Id,
        string Title,
        DateTimeOffset? OccurredAtUtc,
        string? Notes,
        double? Confidence,
        IReadOnlyList<string> Tags,
        IReadOnlyDictionary<string, string> Metadata,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc)
    {
        public static EventDocument FromDomain(Event @event) =>
            new(
                @event.Id,
                @event.Title,
                @event.OccurredAtUtc,
                @event.Notes,
                @event.Confidence,
                @event.Tags,
                @event.Metadata,
                @event.CreatedAtUtc,
                @event.UpdatedAtUtc);

        public Event ToDomain() =>
            new(
                Id,
                Title,
                OccurredAtUtc,
                Notes,
                Confidence,
                Tags ?? [],
                Metadata ?? new Dictionary<string, string>(),
                CreatedAtUtc,
                UpdatedAtUtc);
    }

    private sealed record EvidenceDocument(
        Guid Id,
        string Title,
        string? Citation,
        string? Notes,
        double? Confidence,
        IReadOnlyList<string> Tags,
        IReadOnlyDictionary<string, string> Metadata,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc)
    {
        public static EvidenceDocument FromDomain(WorkspaceEvidence evidence) =>
            new(
                evidence.Id,
                evidence.Title,
                evidence.Citation,
                evidence.Notes,
                evidence.Confidence,
                evidence.Tags,
                evidence.Metadata,
                evidence.CreatedAtUtc,
                evidence.UpdatedAtUtc);

        public WorkspaceEvidence ToDomain() =>
            new(
                Id,
                Title,
                Citation,
                Notes,
                Confidence,
                Tags ?? [],
                Metadata ?? new Dictionary<string, string>(),
                CreatedAtUtc,
                UpdatedAtUtc);
    }
}
