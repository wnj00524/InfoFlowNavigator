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
        var analysisService = new WorkspaceAnalysisService();

        _ = analysisService;
        _ = new GraphMlWorkspaceAdapter();
        _ = new PlainTextReportGenerator(analysisService);

        var shellViewModel = new WorkspaceShellViewModel(workspaceService, analysisService);
        return new MainWindow(shellViewModel);
    }
}
