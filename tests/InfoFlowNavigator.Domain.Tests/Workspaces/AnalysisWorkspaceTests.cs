using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Domain.Tests.Workspaces;

public sealed class AnalysisWorkspaceTests
{
    [Fact]
    public void CreateNew_InitializesEmptyCollections()
    {
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha");

        Assert.Equal("Case Alpha", workspace.Name);
        Assert.Empty(workspace.Entities);
        Assert.Empty(workspace.Relationships);
        Assert.Empty(workspace.Events);
        Assert.Empty(workspace.Sources);
    }
}
