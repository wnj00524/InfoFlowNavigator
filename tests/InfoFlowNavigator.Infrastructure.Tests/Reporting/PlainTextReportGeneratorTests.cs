using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.Analysis;
using InfoFlowNavigator.Infrastructure.Reporting;

namespace InfoFlowNavigator.Infrastructure.Tests.Reporting;

public sealed class PlainTextReportGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_IncludesHypothesesAndAssessmentDetails()
    {
        var generator = new PlainTextReportGenerator(new WorkspaceAnalysisService());
        var workspace = AnalysisWorkspace.CreateNew("Case Report");
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person"));
        workspace = workspace.AddHypothesis(Hypothesis.Create("Employment", "Alice works for Contoso.", HypothesisStatus.Active, 0.75));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview Summary", "INT-001", "Analyst notes", 0.75));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Hypothesis, workspace.Hypotheses[0].Id, EvidenceRelationToTarget.Supports, EvidenceStrength.Strong, "Supports employment", 0.8));

        var artifact = await generator.GenerateAsync(workspace);

        Assert.Contains("Hypothesis Inference:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Hypotheses:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Evidence Assessments:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Employment", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Relation: Supports", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Strength: Strong", artifact.Content, StringComparison.Ordinal);
    }
}
