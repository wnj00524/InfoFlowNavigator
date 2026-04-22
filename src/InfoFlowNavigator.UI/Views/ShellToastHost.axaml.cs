using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace InfoFlowNavigator.UI.Views;

public partial class ShellToastHost : UserControl
{
    public ShellToastHost()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
