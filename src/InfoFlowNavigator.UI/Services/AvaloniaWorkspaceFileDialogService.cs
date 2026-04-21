using Avalonia.Controls;
using Avalonia.Platform.Storage;
using InfoFlowNavigator.Application.Abstractions;

namespace InfoFlowNavigator.UI.Services;

public sealed class AvaloniaWorkspaceFileDialogService : IWorkspaceFileDialogService
{
    private static readonly FilePickerFileType WorkspaceFileType = new("InfoFlowNavigator Workspace")
    {
        Patterns = ["*.ifn.json", "*.json"]
    };

    private readonly Func<Window?> _windowAccessor;

    public AvaloniaWorkspaceFileDialogService(Func<Window?> windowAccessor)
    {
        _windowAccessor = windowAccessor;
    }

    public async Task<string?> PickOpenWorkspacePathAsync(CancellationToken cancellationToken = default)
    {
        var window = _windowAccessor();
        if (window?.StorageProvider is null)
        {
            return null;
        }

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Workspace",
            AllowMultiple = false,
            FileTypeFilter = [WorkspaceFileType]
        });

        return files.Count == 0 ? null : ResolveLocalPath(files[0]);
    }

    public async Task<string?> PickSaveWorkspacePathAsync(string suggestedFileName, CancellationToken cancellationToken = default)
    {
        var window = _windowAccessor();
        if (window?.StorageProvider is null)
        {
            return null;
        }

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Workspace",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "ifn.json",
            FileTypeChoices = [WorkspaceFileType]
        });

        return file is null ? null : ResolveLocalPath(file);
    }

    private static string? ResolveLocalPath(IStorageItem item)
    {
        var localPath = item.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            return localPath;
        }

        if (item.Path.IsFile)
        {
            return item.Path.LocalPath;
        }

        return null;
    }
}
