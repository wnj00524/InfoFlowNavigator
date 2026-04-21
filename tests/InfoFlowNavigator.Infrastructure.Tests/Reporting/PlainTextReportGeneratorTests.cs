using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Hypotheses;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.Analysis;
using InfoFlowNavigator.Infrastructure.Reporting;

namespace InfoFlowNavigator.Infrastructure.Tests.Reporting;

public sealed class PlainTextReportGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_IncludesAnalystBriefingSections()
    {
        var generator = new PlainTextReportGenerator(new WorkspaceAnalysisService());
        var workspace = AnalysisWorkspace.CreateNew("Case Report");
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person"));
        workspace = workspace.AddEvent(Event.Create("Interview", DateTimeOffset.Parse("2026-04-20T12:00:00Z")));
        workspace = workspace.AddEventParticipant(EventParticipant.Create(workspace.Events[0].Id, workspace.Entities[0].Id, "interviewee", 0.8));
        workspace = workspace.AddHypothesis(Hypothesis.Create("Employment", "Alice works for Contoso.", HypothesisStatus.Active, 0.75));
        workspace = workspace.AddClaim(Claim.Create("Alice works for Contoso.", ClaimType.Relationship, ClaimStatus.Active, 0.8, targetKind: ClaimTargetKind.Hypothesis, targetId: workspace.Hypotheses[0].Id, hypothesisId: workspace.Hypotheses[0].Id));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview Summary", "INT-001", "Analyst notes", 0.75));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Hypothesis, workspace.Hypotheses[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Strong, "Supports employment", 0.8));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Claim, workspace.Claims[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Strong, "Supports claim", 0.8));

        var artifact = await generator.GenerateAsync(workspace);

        Assert.Contains("Workspace Summary", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Key Entities", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Key Events", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Active Hypotheses", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Key Claims", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Strongest Support", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Collection Priorities / Next Steps", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Employment", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Alice works for Contoso.", artifact.Content, StringComparison.Ordinal);
    }
}
