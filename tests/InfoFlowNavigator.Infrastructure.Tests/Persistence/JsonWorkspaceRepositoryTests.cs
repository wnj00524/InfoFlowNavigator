using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.Persistence;

namespace InfoFlowNavigator.Infrastructure.Tests.Persistence;

public sealed class JsonWorkspaceRepositoryTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsWorkspaceIncludingEventsAndEvidenceLinks()
    {
        var repository = new JsonWorkspaceRepository();
        var workspace = AnalysisWorkspace.CreateNew("Round Trip Workspace", "Investigation notes", ["priority", "external"]);
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person", "Primary subject", 0.8));
        workspace = workspace.AddEntity(Entity.Create("Contoso", "Organization", "Employer", 0.7));
        workspace = workspace.AddRelationship(Relationship.Create(workspace.Entities[0].Id, workspace.Entities[1].Id, "employed_by", "Confirmed through interview", 0.9));
        workspace = workspace.AddEvent(Event.Create("Interview conducted", DateTimeOffset.Parse("2026-04-20T12:00:00Z"), "Analyst interview", 0.8));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview Summary", "INT-001", "Alice confirmed her role at Contoso.", 0.85));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Relationship, workspace.Relationships[0].Id, "supports"));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Event, workspace.Events[0].Id, "documents"));

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ifn.json");

        try
        {
            await repository.SaveAsync(path, workspace);
            var reloaded = await repository.LoadAsync(path);
            var json = await File.ReadAllTextAsync(path);

            Assert.Equal(workspace.Name, reloaded.Name);
            Assert.Equal(2, reloaded.Entities.Count);
            Assert.Single(reloaded.Relationships);
            Assert.Single(reloaded.Events);
            Assert.Single(reloaded.Evidence);
            Assert.Equal(2, reloaded.EvidenceLinks.Count);
            Assert.Equal("Interview conducted", reloaded.Events[0].Title);
            Assert.Equal(EvidenceLinkTargetKind.Relationship, reloaded.EvidenceLinks[0].TargetKind);
            Assert.Contains("\"events\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"evidenceLinks\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"targetKind\": \"Relationship\"", json, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
