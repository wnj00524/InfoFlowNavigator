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
        var exportService = new GraphMlWorkspaceAdapter();
        var reportGenerator = new PlainTextReportGenerator(analysisService);

        var shellViewModel = new WorkspaceShellViewModel(workspaceService, analysisService, reportGenerator, exportService);
        return new MainWindow(shellViewModel);
    }
}
