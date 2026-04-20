using Avalonia.Controls;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.UI.ViewModels;

namespace InfoFlowNavigator.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
        : this(new WorkspaceShellViewModel(new WorkspaceApplicationService(new DesignTimeWorkspaceRepository()), new DesignTimeAnalysisService()))
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
        public Task<WorkspaceAnalysisResult> SummarizeAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkspaceAnalysisResult(
                workspace.Entities.Count,
                workspace.Relationships.Count,
                workspace.Events.Count,
                workspace.Evidence.Count,
                workspace.EvidenceLinks.Count,
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                new EvidenceAnalysisSummary(0, 0, 0, 0, 0),
                [new AnalysisFinding("Design-time finding", "Findings will appear here when the workspace has data.")]));
    }
}
