using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Hypotheses;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.Persistence;

namespace InfoFlowNavigator.Infrastructure.Tests.Persistence;

public sealed class JsonWorkspaceRepositoryTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsWorkspaceIncludingHypothesesAndAssessments()
    {
        var repository = new JsonWorkspaceRepository();
        var workspace = AnalysisWorkspace.CreateNew("Round Trip Workspace", "Investigation notes", ["priority", "external"]);
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person", "Primary subject", 0.8));
        workspace = workspace.AddEntity(Entity.Create("Contoso", "Organization", "Employer", 0.7));
        workspace = workspace.AddRelationship(Relationship.Create(workspace.Entities[0].Id, workspace.Entities[1].Id, "employed_by", "Confirmed through interview", 0.9));
        workspace = workspace.AddEvent(Event.Create("Interview conducted", DateTimeOffset.Parse("2026-04-20T12:00:00Z"), "Analyst interview", 0.8));
        workspace = workspace.AddHypothesis(Hypothesis.Create("Employment", "Alice works for Contoso.", HypothesisStatus.Active, 0.7));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview Summary", "INT-001", "Alice confirmed her role at Contoso.", 0.85));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Relationship, workspace.Relationships[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Strong, "Direct support"));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Hypothesis, workspace.Hypotheses[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Moderate, "Feeds the hypothesis"));

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ifn.json");

        try
        {
            await repository.SaveAsync(path, workspace);
            var reloaded = await repository.LoadAsync(path);
            var json = await File.ReadAllTextAsync(path);

            Assert.Equal(workspace.Name, reloaded.Name);
            Assert.Single(reloaded.Hypotheses);
            Assert.Equal(2, reloaded.EvidenceLinks.Count);
            Assert.Equal(EvidenceRelationToTarget.Supports, reloaded.EvidenceLinks[0].RelationToTarget);
            Assert.Contains("\"hypotheses\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"relationToTarget\": \"Supports\"", json, StringComparison.Ordinal);
            Assert.Contains("\"strength\": \"Strong\"", json, StringComparison.Ordinal);
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
