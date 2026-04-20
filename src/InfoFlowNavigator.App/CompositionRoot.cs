using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Infrastructure.Analysis;
using InfoFlowNavigator.Infrastructure.ImportExport;
using InfoFlowNavigator.Infrastructure.Persistence;
using InfoFlowNavigator.Infrastructure.Reporting;
using InfoFlowNavigator.UI.ViewModels;
using InfoFlowNavigator.UI.Views;

namespace InfoFlowNavigator.App;

internal static class CompositionRoot
{
    public static MainWindow CreateMainWindow()
    {
        var workspaceRepository = new JsonWorkspaceRepository();
        var workspaceService = new WorkspaceApplicationService(workspaceRepository);

        _ = new PlaceholderAnalysisService();
        _ = new GraphMlWorkspaceAdapter();
        _ = new PlainTextReportGenerator();

        var shellViewModel = new ShellViewModel(workspaceService);
        return new MainWindow(shellViewModel);
    }
}
