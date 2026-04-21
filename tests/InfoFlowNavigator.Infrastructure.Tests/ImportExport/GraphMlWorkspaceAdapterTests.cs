using System.Text.Json;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Hypotheses;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.ImportExport;

namespace InfoFlowNavigator.Infrastructure.Tests.ImportExport;

public sealed class GraphMlWorkspaceAdapterTests
{
    [Fact]
    public async Task ExportAsync_WritesMedWFriendlyNetworkJsonForWorkspace()
    {
        var adapter = new GraphMlWorkspaceAdapter();
        var workspace = AnalysisWorkspace.CreateNew("Export Workspace");
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person"));
        workspace = workspace.AddEntity(Entity.Create("Contoso", "Organization"));
        workspace = workspace.AddRelationship(Relationship.Create(workspace.Entities[0].Id, workspace.Entities[1].Id, "works_for", confidence: 0.8));
        workspace = workspace.AddEvent(Event.Create("Interview", DateTimeOffset.Parse("2026-04-20T12:00:00Z"), confidence: 0.7));
        workspace = workspace.AddEventParticipant(EventParticipant.Create(workspace.Events[0].Id, workspace.Entities[0].Id, "interviewee", 0.75));
        workspace = workspace.AddHypothesis(Hypothesis.Create("Employment", "Alice works for Contoso.", HypothesisStatus.Active, 0.8));
        workspace = workspace.AddClaim(Claim.Create("Alice works for Contoso.", ClaimType.Relationship, ClaimStatus.Active, 0.8, targetKind: ClaimTargetKind.Relationship, targetId: workspace.Relationships[0].Id, hypothesisId: workspace.Hypotheses[0].Id));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview Note", "INT-001", "Notes", 0.8));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Claim, workspace.Claims[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Strong));

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.network.json");

        try
        {
            await adapter.ExportAsync(workspace, path);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            var root = document.RootElement;

            Assert.True(root.TryGetProperty("name", out var nameProperty));
            Assert.Equal("Export Workspace", nameProperty.GetString());
            Assert.True(root.TryGetProperty("nodes", out var nodesProperty));
            Assert.True(root.TryGetProperty("edges", out var edgesProperty));

            var nodeIds = nodesProperty.EnumerateArray()
                .Select(node => node.GetProperty("id").GetString())
                .ToArray();
            var edgeIds = edgesProperty.EnumerateArray()
                .Select(edge => edge.GetProperty("id").GetString())
                .ToArray();

            Assert.Contains($"entity:{workspace.Entities[0].Id:N}", nodeIds);
            Assert.Contains($"event:{workspace.Events[0].Id:N}", nodeIds);
            Assert.Contains($"hypothesis:{workspace.Hypotheses[0].Id:N}", nodeIds);
            Assert.Contains($"claim:{workspace.Claims[0].Id:N}", nodeIds);
            Assert.Contains($"relationship-node:{workspace.Relationships[0].Id:N}", nodeIds);
            Assert.Contains($"participant:{workspace.EventParticipants[0].Id:N}", edgeIds);
            Assert.Contains($"evidence-link:{workspace.EvidenceLinks[0].Id:N}", edgeIds);

            var firstNode = nodesProperty.EnumerateArray().First();
            Assert.True(firstNode.TryGetProperty("trafficProfiles", out var trafficProfilesProperty));
            Assert.Equal(JsonValueKind.Array, trafficProfilesProperty.ValueKind);

            var participantEdge = edgesProperty.EnumerateArray().First(edge => edge.GetProperty("id").GetString() == $"participant:{workspace.EventParticipants[0].Id:N}");
            Assert.Equal($"entity:{workspace.Entities[0].Id:N}", participantEdge.GetProperty("fromNodeId").GetString());
            Assert.Equal($"event:{workspace.Events[0].Id:N}", participantEdge.GetProperty("toNodeId").GetString());
            Assert.Equal("participation", participantEdge.GetProperty("routeType").GetString());
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
