using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
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
    }

    [Fact]
    public void AddRelationship_WhenEntitiesExist_AddsRelationship()
    {
        var entityA = Entity.Create("Alice", "Person");
        var entityB = Entity.Create("Contoso", "Organization");
        var workspace = AnalysisWorkspace
            .CreateNew("Case Alpha")
            .AddEntity(entityA)
            .AddEntity(entityB);

        var updated = workspace.AddRelationship(Relationship.Create(entityA.Id, entityB.Id, "employed_by"));

        Assert.Single(updated.Relationships);
        Assert.Equal(entityA.Id, updated.Relationships[0].SourceEntityId);
        Assert.Equal(entityB.Id, updated.Relationships[0].TargetEntityId);
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
    public void UpdateEntity_UpdatesValuesAndWorkspaceUpdatedAtUtc()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var entity = new Entity(
            Guid.NewGuid(),
            "Alice",
            "Person",
            "Initial notes",
            0.2,
            [],
            new Dictionary<string, string>(),
            createdAt,
            createdAt);

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
    public void RemoveEntity_WithoutRelationships_Succeeds()
    {
        var entity = Entity.Create("Alice", "Person");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha").AddEntity(entity);

        var updated = workspace.RemoveEntity(entity.Id);

        Assert.Empty(updated.Entities);
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
    public void RemoveRelationship_Succeeds()
    {
        var entityA = Entity.Create("Alice", "Person");
        var entityB = Entity.Create("Contoso", "Organization");
        var relationship = Relationship.Create(entityA.Id, entityB.Id, "works_for");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha")
            .AddEntity(entityA)
            .AddEntity(entityB)
            .AddRelationship(relationship);

        var updated = workspace.RemoveRelationship(relationship.Id);

        Assert.Empty(updated.Relationships);
    }

    [Fact]
    public void AddEvidence_Succeeds()
    {
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha");

        var updated = workspace.AddEvidence(WorkspaceEvidence.Create("Analyst note", "Doc-1", "Observed pattern", 0.6));

        Assert.Single(updated.Evidence);
        Assert.Equal("Analyst note", updated.Evidence[0].Title);
    }

    [Fact]
    public void UpdateEvidence_Succeeds()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var evidence = new WorkspaceEvidence(
            Guid.NewGuid(),
            "Initial title",
            "Doc-1",
            "Original note",
            0.3,
            [],
            new Dictionary<string, string>(),
            createdAt,
            createdAt);

        var workspace = new AnalysisWorkspace(
            AnalysisWorkspace.CurrentSchemaVersion,
            Guid.NewGuid(),
            "Case Alpha",
            null,
            [],
            createdAt,
            createdAt,
            [],
            [],
            [],
            [evidence]);

        var updatedEvidence = evidence.Update("Revised title", "Doc-2", "Updated note", 0.9);
        var updatedWorkspace = workspace.UpdateEvidence(updatedEvidence);

        Assert.Equal("Revised title", updatedWorkspace.Evidence[0].Title);
        Assert.Equal("Doc-2", updatedWorkspace.Evidence[0].Citation);
        Assert.Equal("Updated note", updatedWorkspace.Evidence[0].Notes);
        Assert.Equal(0.9, updatedWorkspace.Evidence[0].Confidence);
        Assert.True(updatedWorkspace.Evidence[0].UpdatedAtUtc > evidence.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveEvidence_Succeeds()
    {
        var evidence = WorkspaceEvidence.Create("Analyst note");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha").AddEvidence(evidence);

        var updated = workspace.RemoveEvidence(evidence.Id);

        Assert.Empty(updated.Evidence);
    }
}
