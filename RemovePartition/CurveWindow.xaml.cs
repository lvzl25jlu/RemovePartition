using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static RemovePartition.FluentGas;

namespace RemovePartition;

/// <summary>
/// CurveWindow.xaml 的交互逻辑
/// </summary>
public partial class CurveWindow : Window
{
    public MainMethodEnum SelectedRhsMethod { get; private set; }
    public FluxMethodEnum SelectedFluxMethod { get; private set; }
    public TimeMethodEnum SelectedTimeMethod { get; private set; }
    public Color? SelectedColor { get; private set; }

    public CurveWindow()
    {
        InitializeComponent();

        foreach(var method in Enum.GetValues<MainMethodEnum>())
        {
            MainMethod_CB.Items.Add(new ComboBoxItem { Content = $"{method}" });
        }
        foreach(var method in Enum.GetValues<FluxMethodEnum>())
        {
            FluxMethod_CB.Items.Add(new ComboBoxItem { Content = $"{method}" });
        }
        foreach(var method in Enum.GetValues<TimeMethodEnum>())
        {
            AdvanceMethod_CB.Items.Add(new ComboBoxItem { Content = $"{method}" });
        }

        MainMethod_CB.SelectedIndex = 0;
        FluxMethod_CB.SelectedIndex = 0;
        AdvanceMethod_CB.SelectedIndex = 0;
    }

    private void OK_BT_Click(object sender, RoutedEventArgs e)
    {
        SelectedRhsMethod = GetEnum<MainMethodEnum>(MainMethod_CB);
        SelectedFluxMethod = GetEnum<FluxMethodEnum>(FluxMethod_CB);
        SelectedTimeMethod = GetEnum<TimeMethodEnum>(AdvanceMethod_CB);
        SelectedColor = CurveColor_CP.SelectedColor;

        DialogResult = true;
        Close();
    }

    private void KO_BT_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    static TEnum GetEnum<TEnum>(ComboBox combo)
        where TEnum : struct, Enum
    {
        if(combo.SelectedItem is ComboBoxItem item
            && Enum.TryParse<TEnum>(item.Content?.ToString(), out var value))
        {
            return value;
        }
        return default;
    }
}

