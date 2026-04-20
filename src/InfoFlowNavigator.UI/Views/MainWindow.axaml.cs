using Avalonia.Controls;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.UI.ViewModels;

namespace InfoFlowNavigator.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
        : this(new ShellViewModel(new WorkspaceApplicationService(new DesignTimeWorkspaceRepository())))
    {
    }

    public MainWindow(ShellViewModel viewModel)
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
}
