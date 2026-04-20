using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Application.Tests.Workspaces;

public sealed class WorkspaceApplicationServiceTests
{
    [Fact]
    public void CreateWorkspace_ReturnsNewWorkspaceWithRequestedName()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());

        var workspace = service.CreateWorkspace("Bootstrap Workspace");

        Assert.Equal("Bootstrap Workspace", workspace.Name);
    }

    [Fact]
    public void AddUpdateAndRemoveHypothesis_ThroughApplicationService_UpdatesWorkspace()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");

        workspace = service.AddHypothesis(workspace, "Employment", "Alice works for Contoso.");
        workspace = service.UpdateHypothesis(workspace, workspace.Hypotheses[0].Id, "Employment revised", "Alice likely works for Contoso.", HypothesisStatus.Active, 0.75, "Needs corroboration");
        workspace = service.RemoveHypothesis(workspace, workspace.Hypotheses[0].Id);

        Assert.Empty(workspace.Hypotheses);
    }

    [Fact]
    public void AddAssessmentAndQuerySupportAndContradiction_Work()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");
        workspace = service.AddHypothesis(workspace, "Employment", "Alice works for Contoso.");
        workspace = service.AddEvidence(workspace, "Interview Summary", "INT-001", "Notes", 0.8);
        workspace = service.AddEvidence(workspace, "Payroll Record", "PAY-007", "Contradictory record", 0.9);

        workspace = service.AddHypothesisEvidenceLink(
            workspace,
            workspace.Evidence[0].Id,
            workspace.Hypotheses[0].Id,
            EvidenceRelationToTarget.Supports,
            EvidenceStrength.Strong,
            "Direct corroboration",
            0.8);

        workspace = service.AddHypothesisEvidenceLink(
            workspace,
            workspace.Evidence[1].Id,
            workspace.Hypotheses[0].Id,
            EvidenceRelationToTarget.Contradicts,
            EvidenceStrength.Moderate,
            "Record mismatch",
            0.7);

        var support = service.GetSupportingEvidenceByTarget(workspace, EvidenceLinkTargetKind.Hypothesis, workspace.Hypotheses[0].Id);
        var contradiction = service.GetContradictingEvidenceByTarget(workspace, EvidenceLinkTargetKind.Hypothesis, workspace.Hypotheses[0].Id);
        var summary = service.GetHypothesisEvidenceSummary(workspace, workspace.Hypotheses[0].Id);

        Assert.Single(support);
        Assert.Single(contradiction);
        Assert.Single(summary.SupportingEvidence);
        Assert.Single(summary.ContradictingEvidence);
    }

    private sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
    {
        public Task<AnalysisWorkspace> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(AnalysisWorkspace.CreateNew(path));

        public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
