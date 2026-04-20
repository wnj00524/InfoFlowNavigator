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

    private ShellViewModel ViewModel => (ShellViewModel)DataContext!;

    private void CreateNewWorkspace_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TryRun(() => ViewModel.CreateNewWorkspace());
    }

    private async void OpenWorkspace_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await TryRunAsync(() => ViewModel.OpenWorkspaceAsync());
    }

    private async void SaveWorkspace_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await TryRunAsync(() => ViewModel.SaveWorkspaceAsync());
    }

    private void AddEntity_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TryRun(() => ViewModel.AddEntity());
    }

    private void AddRelationship_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TryRun(() => ViewModel.AddRelationship());
    }

    private void TryRun(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ViewModel.SetStatus(ex.Message);
        }
    }

    private async Task TryRunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ViewModel.SetStatus(ex.Message);
        }
    }

    private sealed class DesignTimeWorkspaceRepository : IWorkspaceRepository
    {
        public Task<AnalysisWorkspace> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(AnalysisWorkspace.CreateNew("Design Workspace"));

        public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
