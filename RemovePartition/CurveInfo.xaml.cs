using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using static RemovePartition.FluentGas;

namespace RemovePartition;

/// <summary>
/// CurveInfo.xaml 的交互逻辑
/// </summary>
public partial class CurveInfo : UserControl
{
    public static readonly DependencyProperty ColorBrushProperty =
        DependencyProperty.Register(nameof(ColorBrush), typeof(Brush), typeof(CurveInfo),
            new PropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty RhsMethodProperty =
        DependencyProperty.Register(nameof(MainMethod), typeof(MainMethodEnum), typeof(CurveInfo),
            new PropertyMetadata(default(MainMethodEnum)));

    public static readonly DependencyProperty FluxMethodProperty =
        DependencyProperty.Register(nameof(FluxMethod), typeof(FluxMethodEnum), typeof(CurveInfo),
            new PropertyMetadata(default(FluxMethodEnum)));

    public static readonly DependencyProperty TimeMethodProperty =
        DependencyProperty.Register(nameof(TimeMethod), typeof(TimeMethodEnum), typeof(CurveInfo),
            new PropertyMetadata(default(TimeMethodEnum)));

    public Brush ColorBrush
    {
        get => (Brush)GetValue(ColorBrushProperty);
        set => SetValue(ColorBrushProperty, value);
    }

    public MainMethodEnum MainMethod
    {
        get => (MainMethodEnum)GetValue(RhsMethodProperty);
        set => SetValue(RhsMethodProperty, value);
    }

    public FluxMethodEnum FluxMethod
    {
        get => (FluxMethodEnum)GetValue(FluxMethodProperty);
        set => SetValue(FluxMethodProperty, value);
    }

    public TimeMethodEnum TimeMethod
    {
        get => (TimeMethodEnum)GetValue(TimeMethodProperty);
        set => SetValue(TimeMethodProperty, value);
    }

    public CurveInfo()
    {
        InitializeComponent();
    }
}

