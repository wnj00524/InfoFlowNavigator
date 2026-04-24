using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.Claims;
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
        workspace = workspace.AddEventParticipant(EventParticipant.Create(workspace.Events[0].Id, workspace.Entities[0].Id, "interviewee", 0.7));
        workspace = workspace.AddHypothesis(Hypothesis.Create("Employment", "Alice works for Contoso.", HypothesisStatus.Active, 0.7));
        workspace = workspace.AddClaim(Claim.Create("Alice works for Contoso.", ClaimType.Relationship, ClaimStatus.Active, 0.75, targetKind: ClaimTargetKind.Relationship, targetId: workspace.Relationships[0].Id, hypothesisId: workspace.Hypotheses[0].Id));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview Summary", "INT-001", "Alice confirmed her role at Contoso.", 0.85));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Relationship, workspace.Relationships[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Strong, "Direct support"));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Hypothesis, workspace.Hypotheses[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Moderate, "Feeds the hypothesis"));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Claim, workspace.Claims[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Strong, "Directly supports the claim"));

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ifn.json");

        try
        {
            await repository.SaveAsync(path, workspace);
            var reloaded = await repository.LoadAsync(path);
            var json = await File.ReadAllTextAsync(path);

            Assert.Equal(workspace.Name, reloaded.Name);
            Assert.Single(reloaded.Hypotheses);
            Assert.Single(reloaded.Claims);
            Assert.Single(reloaded.EventParticipants);
            Assert.Equal(EventEntityLinkCategory.Participant, reloaded.EventParticipants[0].Category);
            Assert.Equal(3, reloaded.EvidenceLinks.Count);
            Assert.Equal(EvidenceRelationToTarget.Supports, reloaded.EvidenceLinks[0].RelationToTarget);
            Assert.Contains("\"hypotheses\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"claims\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"eventParticipants\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"category\": \"Participant\"", json, StringComparison.Ordinal);
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

    [Fact]
    public async Task LoadAsync_LegacyRoleOnlyParticipantDocument_InfersStructuredCategory()
    {
        var repository = new JsonWorkspaceRepository();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ifn.json");
        var workspaceId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var timestamp = DateTimeOffset.Parse("2026-04-22T10:15:00Z");

        var json = $$"""
        {
          "schemaVersion": 1,
          "id": "{{workspaceId}}",
          "name": "Legacy Workspace",
          "tags": [],
          "createdAtUtc": "{{timestamp:O}}",
          "updatedAtUtc": "{{timestamp:O}}",
          "entities": [
            {
              "id": "{{entityId}}",
              "name": "Transit Van",
              "entityType": "Vehicle",
              "tags": [],
              "metadata": {},
              "createdAtUtc": "{{timestamp:O}}",
              "updatedAtUtc": "{{timestamp:O}}"
            }
          ],
          "relationships": [],
          "events": [
            {
              "id": "{{eventId}}",
              "title": "Departure",
              "tags": [],
              "metadata": {},
              "createdAtUtc": "{{timestamp:O}}",
              "updatedAtUtc": "{{timestamp:O}}"
            }
          ],
          "eventParticipants": [
            {
              "id": "{{participantId}}",
              "eventId": "{{eventId}}",
              "entityId": "{{entityId}}",
              "role": "vehicle",
              "createdAtUtc": "{{timestamp:O}}",
              "updatedAtUtc": "{{timestamp:O}}"
            }
          ],
          "claims": [],
          "hypotheses": [],
          "evidence": [],
          "evidenceLinks": []
        }
        """;

        try
        {
            await File.WriteAllTextAsync(path, json);
            var loaded = await repository.LoadAsync(path);

            Assert.Single(loaded.EventParticipants);
            Assert.Equal(EventEntityLinkCategory.Vehicle, loaded.EventParticipants[0].Category);
            Assert.Null(loaded.EventParticipants[0].RoleDetail);
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
