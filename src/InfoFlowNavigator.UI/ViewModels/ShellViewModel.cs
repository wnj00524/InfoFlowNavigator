using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class ShellViewModel
{
    public ShellViewModel(WorkspaceApplicationService workspaceService)
    {
        Workspace = workspaceService.CreateWorkspace("Untitled Workspace");
    }

    public string Title => "Info Flow Navigator";

    public string Subtitle => "Offline-first intelligence analysis workspace";

    public AnalysisWorkspace Workspace { get; }

    public string[] PlannedCapabilities =>
    [
        "Entity and relationship modeling",
        "Timeline-ready event capture",
        "Geospatial overlay hooks",
        "Hypothesis and evidence workflows",
        "Human-readable export and reporting"
    ];
}
