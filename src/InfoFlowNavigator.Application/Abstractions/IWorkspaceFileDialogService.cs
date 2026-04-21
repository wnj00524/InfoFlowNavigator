namespace InfoFlowNavigator.Application.Abstractions;

public interface IWorkspaceFileDialogService
{
    Task<string?> PickOpenWorkspacePathAsync(CancellationToken cancellationToken = default);

    Task<string?> PickSaveWorkspacePathAsync(string suggestedFileName, CancellationToken cancellationToken = default);
}
