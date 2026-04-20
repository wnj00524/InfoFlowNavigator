using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Domain.Tests.Workspaces;

public sealed class AnalysisWorkspaceTests
{
    [Fact]
    public void CreateNew_InitializesEmptyCollectionsAndSchema()
    {
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha");

        Assert.Equal(AnalysisWorkspace.CurrentSchemaVersion, workspace.SchemaVersion);
        Assert.Equal("Case Alpha", workspace.Name);
        Assert.Empty(workspace.Entities);
        Assert.Empty(workspace.Relationships);
        Assert.Empty(workspace.Events);
        Assert.Empty(workspace.Evidence);
        Assert.Empty(workspace.EvidenceLinks);
    }

    [Fact]
    public void UpdateEntity_UpdatesValuesAndWorkspaceUpdatedAtUtc()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var entity = new Entity(Guid.NewGuid(), "Alice", "Person", "Initial notes", 0.2, [], new Dictionary<string, string>(), createdAt, createdAt);
        var workspace = new AnalysisWorkspace(
            AnalysisWorkspace.CurrentSchemaVersion,
            Guid.NewGuid(),
            "Case Alpha",
            null,
            [],
            createdAt,
            createdAt,
            [entity],
            [],
            [],
            [],
            []);

        var updatedEntity = entity.Update("Alice Smith", "Subject", "Updated notes", 0.8);
        var updatedWorkspace = workspace.UpdateEntity(updatedEntity);

        Assert.Equal("Alice Smith", updatedWorkspace.Entities[0].Name);
        Assert.Equal("Subject", updatedWorkspace.Entities[0].EntityType);
        Assert.Equal("Updated notes", updatedWorkspace.Entities[0].Notes);
        Assert.Equal(0.8, updatedWorkspace.Entities[0].Confidence);
        Assert.True(updatedWorkspace.Entities[0].UpdatedAtUtc > entity.UpdatedAtUtc);
        Assert.True(updatedWorkspace.UpdatedAtUtc > workspace.UpdatedAtUtc);
    }

    [Fact]
    public void AddAndUpdateEvent_Succeeds()
    {
        var occurredAt = DateTimeOffset.Parse("2026-04-20T12:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind);
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha");

        var added = workspace.AddEvent(Event.Create("Meeting", occurredAt, "Observed"));
        var updated = added.UpdateEvent(added.Events[0].Update("Meeting revised", occurredAt.AddHours(1), "Revised", 0.8));

        Assert.Single(updated.Events);
        Assert.Equal("Meeting revised", updated.Events[0].Title);
        Assert.Equal("Revised", updated.Events[0].Notes);
        Assert.Equal(0.8, updated.Events[0].Confidence);
    }

    [Fact]
    public void RemoveEvent_WithReferencingEvidenceLink_Throws()
    {
        var evidence = WorkspaceEvidence.Create("Interview");
        var @event = Event.Create("Meeting");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha")
            .AddEvidence(evidence)
            .AddEvent(@event)
            .AddEvidenceLink(EvidenceLink.Create(evidence.Id, EvidenceLinkTargetKind.Event, @event.Id));

        var act = () => workspace.RemoveEvent(@event.Id);

        var ex = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("evidence links still reference", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddRelationship_WhenEntityMissing_Throws()
    {
        var entityA = Entity.Create("Alice", "Person");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha").AddEntity(entityA);

        var act = () => workspace.AddRelationship(Relationship.Create(entityA.Id, Guid.NewGuid(), "linked_to"));

        var ex = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("target entity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RemoveEntity_WithReferencingRelationship_Throws()
    {
        var entityA = Entity.Create("Alice", "Person");
        var entityB = Entity.Create("Contoso", "Organization");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha")
            .AddEntity(entityA)
            .AddEntity(entityB)
            .AddRelationship(Relationship.Create(entityA.Id, entityB.Id, "works_for"));

        var act = () => workspace.RemoveEntity(entityA.Id);

        var ex = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("relationships still reference", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddEvidenceLink_WithMissingEvidence_Throws()
    {
        var entity = Entity.Create("Alice", "Person");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha").AddEntity(entity);

        var act = () => workspace.AddEvidenceLink(EvidenceLink.Create(Guid.NewGuid(), EvidenceLinkTargetKind.Entity, entity.Id));

        var ex = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("existing evidence", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddAndRemoveEvidenceLink_Succeeds()
    {
        var entity = Entity.Create("Alice", "Person");
        var evidence = WorkspaceEvidence.Create("Interview");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha")
            .AddEntity(entity)
            .AddEvidence(evidence);

        var withLink = workspace.AddEvidenceLink(EvidenceLink.Create(evidence.Id, EvidenceLinkTargetKind.Entity, entity.Id, "supports"));
        var removed = withLink.RemoveEvidenceLink(withLink.EvidenceLinks[0].Id);

        Assert.Single(withLink.EvidenceLinks);
        Assert.Empty(removed.EvidenceLinks);
    }

    [Fact]
    public void RemoveEvidence_WithReferencingEvidenceLink_Throws()
    {
        var entity = Entity.Create("Alice", "Person");
        var evidence = WorkspaceEvidence.Create("Interview");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha")
            .AddEntity(entity)
            .AddEvidence(evidence)
            .AddEvidenceLink(EvidenceLink.Create(evidence.Id, EvidenceLinkTargetKind.Entity, entity.Id));

        var act = () => workspace.RemoveEvidence(evidence.Id);

        var ex = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("evidence links still reference", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
