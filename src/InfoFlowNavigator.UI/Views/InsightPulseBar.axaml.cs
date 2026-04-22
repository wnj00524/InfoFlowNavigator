using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace InfoFlowNavigator.UI.Views;

public partial class InsightPulseBar : UserControl
{
    public InsightPulseBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
