using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace InfoFlowNavigator.UI.Views;

public partial class SpotlightComposer : UserControl
{
    public SpotlightComposer()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
