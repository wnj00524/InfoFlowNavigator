using Avalonia.Controls;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Application.Reporting;
using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.UI.ViewModels;

namespace InfoFlowNavigator.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
        : this(new WorkspaceShellViewModel(
            new WorkspaceApplicationService(new DesignTimeWorkspaceRepository()),
            new DesignTimeAnalysisService(),
            new DesignTimeReportGenerator(),
            new DesignTimeWorkspaceExportService()))
    {
    }

    public MainWindow(WorkspaceShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private sealed class DesignTimeWorkspaceRepository : IWorkspaceRepository
    {
        public Task<AnalysisWorkspace> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(AnalysisWorkspace.CreateNew("Design Workspace"));

        public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class DesignTimeAnalysisService : IAnalysisService
    {
        public Task<WorkspaceAnalysisResult> SummarizeAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default)
        {
            var findings = new[] { new AnalysisFinding("Design-time finding", "Findings will appear here when the workspace has data.") };

            return Task.FromResult(WorkspaceAnalysisResultFactory.Empty(
                workspace.Entities.Count,
                workspace.Relationships.Count,
                workspace.Events.Count,
                workspace.EventParticipants.Count,
                workspace.Claims.Count,
                workspace.Hypotheses.Count,
                workspace.Evidence.Count,
                workspace.EvidenceLinks.Count,
                findings));
        }
    }

    private sealed class DesignTimeReportGenerator : IReportGenerator
    {
        public Task<ReportArtifact> GenerateAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ReportArtifact("workspace-briefing.txt", "text/plain", "Design-time briefing"));
    }

    private sealed class DesignTimeWorkspaceExportService : IWorkspaceExportService
    {
        public Task ExportAsync(AnalysisWorkspace workspace, string path, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
