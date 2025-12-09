using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RemovePartition;
/// <summary>
/// LabelTextBox.xaml 的交互逻辑
/// </summary>
public partial class LabelTextBox : UserControl
{
    public LabelTextBox()
    {
        InitializeComponent();

        LabelPart.SetBinding(ContentProperty, new Binding("Label") { Source = this });
        TextBoxPart.SetBinding(TextBox.TextProperty, new Binding("Text") { Source = this, Mode = BindingMode.TwoWay });
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register("Label", typeof(string), typeof(LabelTextBox), new PropertyMetadata("Label"));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(LabelTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }


}
