using System.Text.Json;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Infrastructure.Persistence;

public sealed class JsonWorkspaceRepository : IWorkspaceRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<AnalysisWorkspace> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var workspace = await JsonSerializer.DeserializeAsync<AnalysisWorkspace>(stream, SerializerOptions, cancellationToken);

        return workspace ?? throw new InvalidDataException("Workspace file did not contain a valid workspace payload.");
    }

    public async Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, workspace, SerializerOptions, cancellationToken);
    }
}
