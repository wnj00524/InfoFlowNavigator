using InfoFlowNavigator.Domain.Common;
using InfoFlowNavigator.Domain.Entities;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;
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
    IReadOnlyList<Hypothesis> Hypotheses,
    IReadOnlyList<WorkspaceEvidence> Evidence,
    IReadOnlyList<EvidenceLink> EvidenceLinks)
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

    public AnalysisWorkspace UpdateEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Entities.Any(existing => existing.Id == entity.Id))
        {
            throw new InvalidOperationException($"Entity '{entity.Id}' does not exist in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Entities = Entities.Select(existing => existing.Id == entity.Id ? entity : existing).ToArray()
        };
    }

    public AnalysisWorkspace RemoveEntity(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Entity id is required.", nameof(entityId));
        }

        if (!Entities.Any(entity => entity.Id == entityId))
        {
            throw new InvalidOperationException($"Entity '{entityId}' does not exist in the workspace.");
        }

        if (Relationships.Any(relationship => relationship.SourceEntityId == entityId || relationship.TargetEntityId == entityId))
        {
            throw new InvalidOperationException("Cannot remove entity while relationships still reference it.");
        }

        if (EvidenceLinks.Any(link => link.TargetKind == EvidenceLinkTargetKind.Entity && link.TargetId == entityId))
        {
            throw new InvalidOperationException("Cannot remove entity while evidence assessments still reference it.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Entities = Entities.Where(entity => entity.Id != entityId).ToArray()
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

    public AnalysisWorkspace RemoveRelationship(Guid relationshipId)
    {
        if (relationshipId == Guid.Empty)
        {
            throw new ArgumentException("Relationship id is required.", nameof(relationshipId));
        }

        if (!Relationships.Any(relationship => relationship.Id == relationshipId))
        {
            throw new InvalidOperationException($"Relationship '{relationshipId}' does not exist in the workspace.");
        }

        if (EvidenceLinks.Any(link => link.TargetKind == EvidenceLinkTargetKind.Relationship && link.TargetId == relationshipId))
        {
            throw new InvalidOperationException("Cannot remove relationship while evidence assessments still reference it.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Relationships = Relationships.Where(relationship => relationship.Id != relationshipId).ToArray()
        };
    }

    public AnalysisWorkspace AddEvent(Event @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (Events.Any(existing => existing.Id == @event.Id))
        {
            throw new InvalidOperationException($"Event '{@event.Id}' already exists in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Events = Events.Concat([@event]).ToArray()
        };
    }

    public AnalysisWorkspace UpdateEvent(Event @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (!Events.Any(existing => existing.Id == @event.Id))
        {
            throw new InvalidOperationException($"Event '{@event.Id}' does not exist in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Events = Events.Select(existing => existing.Id == @event.Id ? @event : existing).ToArray()
        };
    }

    public AnalysisWorkspace RemoveEvent(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id is required.", nameof(eventId));
        }

        if (!Events.Any(@event => @event.Id == eventId))
        {
            throw new InvalidOperationException($"Event '{eventId}' does not exist in the workspace.");
        }

        if (EvidenceLinks.Any(link => link.TargetKind == EvidenceLinkTargetKind.Event && link.TargetId == eventId))
        {
            throw new InvalidOperationException("Cannot remove event while evidence assessments still reference it.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Events = Events.Where(@event => @event.Id != eventId).ToArray()
        };
    }

    public AnalysisWorkspace AddHypothesis(Hypothesis hypothesis)
    {
        ArgumentNullException.ThrowIfNull(hypothesis);

        if (Hypotheses.Any(existing => existing.Id == hypothesis.Id))
        {
            throw new InvalidOperationException($"Hypothesis '{hypothesis.Id}' already exists in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Hypotheses = Hypotheses.Concat([hypothesis]).ToArray()
        };
    }

    public AnalysisWorkspace UpdateHypothesis(Hypothesis hypothesis)
    {
        ArgumentNullException.ThrowIfNull(hypothesis);

        if (!Hypotheses.Any(existing => existing.Id == hypothesis.Id))
        {
            throw new InvalidOperationException($"Hypothesis '{hypothesis.Id}' does not exist in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Hypotheses = Hypotheses.Select(existing => existing.Id == hypothesis.Id ? hypothesis : existing).ToArray()
        };
    }

    public AnalysisWorkspace RemoveHypothesis(Guid hypothesisId)
    {
        if (hypothesisId == Guid.Empty)
        {
            throw new ArgumentException("Hypothesis id is required.", nameof(hypothesisId));
        }

        if (!Hypotheses.Any(hypothesis => hypothesis.Id == hypothesisId))
        {
            throw new InvalidOperationException($"Hypothesis '{hypothesisId}' does not exist in the workspace.");
        }

        if (EvidenceLinks.Any(link => link.TargetKind == EvidenceLinkTargetKind.Hypothesis && link.TargetId == hypothesisId))
        {
            throw new InvalidOperationException("Cannot remove hypothesis while evidence assessments still reference it.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Hypotheses = Hypotheses.Where(hypothesis => hypothesis.Id != hypothesisId).ToArray()
        };
    }

    public AnalysisWorkspace AddEvidence(WorkspaceEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        if (Evidence.Any(existing => existing.Id == evidence.Id))
        {
            throw new InvalidOperationException($"Evidence '{evidence.Id}' already exists in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Evidence = Evidence.Concat([evidence]).ToArray()
        };
    }

    public AnalysisWorkspace UpdateEvidence(WorkspaceEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        if (!Evidence.Any(existing => existing.Id == evidence.Id))
        {
            throw new InvalidOperationException($"Evidence '{evidence.Id}' does not exist in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Evidence = Evidence.Select(existing => existing.Id == evidence.Id ? evidence : existing).ToArray()
        };
    }

    public AnalysisWorkspace RemoveEvidence(Guid evidenceId)
    {
        if (evidenceId == Guid.Empty)
        {
            throw new ArgumentException("Evidence id is required.", nameof(evidenceId));
        }

        if (!Evidence.Any(evidence => evidence.Id == evidenceId))
        {
            throw new InvalidOperationException($"Evidence '{evidenceId}' does not exist in the workspace.");
        }

        if (EvidenceLinks.Any(link => link.EvidenceId == evidenceId))
        {
            throw new InvalidOperationException("Cannot remove evidence while evidence assessments still reference it.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            Evidence = Evidence.Where(evidence => evidence.Id != evidenceId).ToArray()
        };
    }

    public AnalysisWorkspace AddEvidenceLink(EvidenceLink evidenceLink)
    {
        ArgumentNullException.ThrowIfNull(evidenceLink);

        ValidateEvidenceLinkReferences(evidenceLink);

        if (EvidenceLinks.Any(existing => existing.Id == evidenceLink.Id))
        {
            throw new InvalidOperationException($"Evidence assessment '{evidenceLink.Id}' already exists in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            EvidenceLinks = EvidenceLinks.Concat([evidenceLink]).ToArray()
        };
    }

    public AnalysisWorkspace RemoveEvidenceLink(Guid evidenceLinkId)
    {
        if (evidenceLinkId == Guid.Empty)
        {
            throw new ArgumentException("Evidence assessment id is required.", nameof(evidenceLinkId));
        }

        if (!EvidenceLinks.Any(link => link.Id == evidenceLinkId))
        {
            throw new InvalidOperationException($"Evidence assessment '{evidenceLinkId}' does not exist in the workspace.");
        }

        return this with
        {
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            EvidenceLinks = EvidenceLinks.Where(link => link.Id != evidenceLinkId).ToArray()
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

        foreach (var hypothesis in Hypotheses)
        {
            if (string.IsNullOrWhiteSpace(hypothesis.Title) || string.IsNullOrWhiteSpace(hypothesis.Statement))
            {
                throw new InvalidOperationException("Workspace contains a hypothesis with missing required fields.");
            }
        }

        foreach (var evidenceLink in EvidenceLinks)
        {
            ValidateEvidenceLinkReferences(evidenceLink);
        }

        return this with
        {
            Name = Name.Trim(),
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            Tags = DomainValidation.NormalizeTags(Tags),
            UpdatedAtUtc = UpdatedAtUtc == default ? CreatedAtUtc : UpdatedAtUtc,
            Hypotheses = Hypotheses.ToArray(),
            EvidenceLinks = EvidenceLinks.ToArray()
        };
    }

    private void ValidateEvidenceLinkReferences(EvidenceLink evidenceLink)
    {
        if (!Evidence.Any(evidence => evidence.Id == evidenceLink.EvidenceId))
        {
            throw new InvalidOperationException("Evidence assessment must reference existing evidence.");
        }

        var targetExists = evidenceLink.TargetKind switch
        {
            EvidenceLinkTargetKind.Entity => Entities.Any(entity => entity.Id == evidenceLink.TargetId),
            EvidenceLinkTargetKind.Relationship => Relationships.Any(relationship => relationship.Id == evidenceLink.TargetId),
            EvidenceLinkTargetKind.Event => Events.Any(@event => @event.Id == evidenceLink.TargetId),
            EvidenceLinkTargetKind.Hypothesis => Hypotheses.Any(hypothesis => hypothesis.Id == evidenceLink.TargetId),
            _ => false
        };

        if (!targetExists)
        {
            throw new InvalidOperationException($"Evidence assessment target '{evidenceLink.TargetId}' does not exist for target kind '{evidenceLink.TargetKind}'.");
        }
    }
}
