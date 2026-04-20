using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Infrastructure.ImportExport;

public sealed class GraphMlWorkspaceAdapter : IWorkspaceImportService, IWorkspaceExportService
{
    public Task<AnalysisWorkspace> ImportAsync(string path, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("GraphML import is a planned interchange capability and is not implemented in the bootstrap skeleton.");

    public Task ExportAsync(AnalysisWorkspace workspace, string path, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("GraphML export is a planned interchange capability and is not implemented in the bootstrap skeleton.");
}
