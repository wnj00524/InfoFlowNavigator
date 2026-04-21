using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Hypotheses;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.Analysis;

namespace InfoFlowNavigator.Infrastructure.Tests.Analysis;

public sealed class WorkspaceAnalysisServiceTests
{
    [Fact]
    public async Task SummarizeAsync_ReturnsHypothesisSupportContradictionAndGuidance()
    {
        var service = new WorkspaceAnalysisService();
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha");
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person"));
        workspace = workspace.AddEntity(Entity.Create("Contoso", "Organization"));
        workspace = workspace.AddRelationship(Relationship.Create(workspace.Entities[0].Id, workspace.Entities[1].Id, "works_for"));
        workspace = workspace.AddEvent(Event.Create("Interview", DateTimeOffset.Parse("2026-01-01T10:00:00Z")));
        workspace = workspace.AddEvent(Event.Create("Follow-up", DateTimeOffset.Parse("2026-03-15T10:00:00Z")));
        workspace = workspace.AddEventParticipant(EventParticipant.Create(workspace.Events[0].Id, workspace.Entities[0].Id, "interviewee", 0.8));
        workspace = workspace.AddEventParticipant(EventParticipant.Create(workspace.Events[0].Id, workspace.Entities[1].Id, "mentioned organization", 0.7));
        workspace = workspace.AddEventParticipant(EventParticipant.Create(workspace.Events[1].Id, workspace.Entities[0].Id, "follow-up subject", 0.8));
        workspace = workspace.AddHypothesis(Hypothesis.Create("Employment", "Alice works for Contoso.", HypothesisStatus.Active, 0.7));
        workspace = workspace.AddClaim(Claim.Create("Alice attended the interview.", ClaimType.EventParticipation, ClaimStatus.Active, 0.8, targetKind: ClaimTargetKind.Event, targetId: workspace.Events[0].Id, hypothesisId: workspace.Hypotheses[0].Id));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview Note", "INT-001", "Alice mentioned Contoso.", 0.8));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Payroll Record", "PAY-008", "Different employer listed.", 0.9));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Relationship, workspace.Relationships[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Moderate));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Hypothesis, workspace.Hypotheses[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Strong, "Interview corroborates", 0.9));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[1].Id, EvidenceLinkTargetKind.Hypothesis, workspace.Hypotheses[0].Id, EvidenceRelationToTarget.Contradicts, EvidenceStrength.Strong, "Payroll contradicts", 0.8));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Claim, workspace.Claims[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Strong, "Supports participation", 0.8));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[1].Id, EvidenceLinkTargetKind.Claim, workspace.Claims[0].Id, EvidenceRelationToTarget.Contradicts, EvidenceStrength.Moderate, "Contradicts participation", 0.7));

        var analysis = await service.SummarizeAsync(workspace);

        Assert.Equal(1, analysis.HypothesisCount);
        Assert.Single(analysis.HypothesisSummaries);
        Assert.Equal(1, analysis.HypothesisSummaries[0].SupportCount);
        Assert.Equal(1, analysis.HypothesisSummaries[0].ContradictionCount);
        Assert.Equal("Mixed", analysis.HypothesisSummaries[0].Posture);
        Assert.Single(analysis.ContradictoryClaims);
        Assert.Single(analysis.ClaimHypothesisImpacts);
        Assert.NotEmpty(analysis.TopEventParticipants);
        Assert.Single(analysis.HypothesisConflicts);
        Assert.NotEmpty(analysis.CollectionGuidance);
        Assert.Contains(analysis.Findings, finding => finding.Title == "Hypothesis conflict" && finding.Severity == FindingSeverity.Critical);
    }

    [Fact]
    public async Task SummarizeAsync_UnresolvedHypothesisProducesNextStepGuidance()
    {
        var service = new WorkspaceAnalysisService();
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha")
            .AddHypothesis(Hypothesis.Create("Meeting", "Alice met Contoso leadership.", HypothesisStatus.Active));

        var analysis = await service.SummarizeAsync(workspace);

        Assert.Single(analysis.UnresolvedHypotheses);
        Assert.NotEmpty(analysis.CollectionGuidance);
        Assert.Contains(analysis.Findings, finding => finding.Title == "Unresolved hypothesis" && finding.Category == FindingCategory.Hypothesis);
    }

    [Fact]
    public async Task SummarizeAsync_FindsUnsupportedClaimsCoOccurrenceAndParticipationGaps()
    {
        var service = new WorkspaceAnalysisService();
        var workspace = AnalysisWorkspace.CreateNew("Case Beta");
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person"));
        workspace = workspace.AddEntity(Entity.Create("Bob", "Person"));
        workspace = workspace.AddEvent(Event.Create("Meeting A", DateTimeOffset.Parse("2026-02-01T10:00:00Z")));
        workspace = workspace.AddEvent(Event.Create("Meeting B", DateTimeOffset.Parse("2026-02-10T10:00:00Z")));
        workspace = workspace.AddEventParticipant(EventParticipant.Create(workspace.Events[0].Id, workspace.Entities[0].Id, "attendee", 0.8));
        workspace = workspace.AddEventParticipant(EventParticipant.Create(workspace.Events[0].Id, workspace.Entities[1].Id, "attendee", 0.8));
        workspace = workspace.AddEventParticipant(EventParticipant.Create(workspace.Events[1].Id, workspace.Entities[0].Id, "attendee", 0.8));
        workspace = workspace.AddEventParticipant(EventParticipant.Create(workspace.Events[1].Id, workspace.Entities[1].Id, "attendee", 0.8));
        workspace = workspace.AddClaim(Claim.Create("Alice and Bob met repeatedly.", ClaimType.Activity, ClaimStatus.Active, 0.7));

        var analysis = await service.SummarizeAsync(workspace);

        Assert.Single(analysis.UnsupportedClaims);
        Assert.NotEmpty(analysis.RepeatedCoOccurrences);
        Assert.Contains(analysis.Findings, finding => finding.Category == FindingCategory.SupportGap && finding.TargetKind == "Claim");
    }
}
