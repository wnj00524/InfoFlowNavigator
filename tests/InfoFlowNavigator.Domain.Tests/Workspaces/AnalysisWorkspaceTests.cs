using InfoFlowNavigator.Domain.Entities;
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
}
