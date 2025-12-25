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
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    enum StateEnum
    {
        Idle,
        Working,
        Busy
    }
    StateEnum state;
    StateEnum State
    {
        get => state; set
        {
            state = value;
            switch(value)
            {
                case StateEnum.Idle:
                    // 空闲时可以调整输入
                    PartitionPositionSlider.IsEnabled = true;
                    LDensityLTB.TextBoxPart.IsEnabled = true;
                    RDensityLTB.TextBoxPart.IsEnabled = true;
                    LPressureLTB.TextBoxPart.IsEnabled = true;
                    RPressureLTB.TextBoxPart.IsEnabled = true;
                    Delta_tTextBox.IsEnabled = true;
                    Delta_xTextBox.IsEnabled = true;
                    // 空闲时（初始状态）才能移除隔板
                    RemoveButton.IsEnabled = true;
                    // 还没开始推进
                    NextStepButton.IsEnabled = false;
                    NextManyButton.IsEnabled = false;
                    // 可以重置
                    ResetButton.IsEnabled = true;
                    break;
                case StateEnum.Working:
                    // 工作时不许调整输入
                    PartitionPositionSlider.IsEnabled = false;
                    LDensityLTB.TextBoxPart.IsEnabled = false;
                    RDensityLTB.TextBoxPart.IsEnabled = false;
                    LPressureLTB.TextBoxPart.IsEnabled = false;
                    RPressureLTB.TextBoxPart.IsEnabled = false;
                    Delta_tTextBox.IsEnabled = false;
                    Delta_xTextBox.IsEnabled = false;
                    // 无法移走已经被移走的隔板
                    RemoveButton.IsEnabled = false;
                    // 可以推进
                    NextStepButton.IsEnabled = true;
                    NextManyButton.IsEnabled = true;
                    // 可以重置
                    ResetButton.IsEnabled = true;
                    break;
                case StateEnum.Busy:
                    // 耐心等待计算完成，什么都不要干
                    PartitionPositionSlider.IsEnabled = false;
                    LDensityLTB.TextBoxPart.IsEnabled = false;
                    RDensityLTB.TextBoxPart.IsEnabled = false;
                    LPressureLTB.TextBoxPart.IsEnabled = false;
                    RPressureLTB.TextBoxPart.IsEnabled = false;
                    Delta_tTextBox.IsEnabled = false;
                    Delta_xTextBox.IsEnabled = false;
                    RemoveButton.IsEnabled = false;
                    NextStepButton.IsEnabled = false;
                    NextManyButton.IsEnabled = false;
                    ResetButton.IsEnabled = false;
                    break;
            }
        }
    }

    IGas gas = new PartitionedGas() { PointsCount = IGas.DEFAULT_POINTS_COUNT };

    public MainWindow()
    {
        InitializeComponent();

        // 等所有控件加载完毕后再绑定事件
        // 否则加载时会触发 ValueChanged 事件
        // 此时画布还是 null

        RoutedEventHandler? onLoaded = null;
        onLoaded = new RoutedEventHandler((s, e) =>
        {
            PartitionPositionSlider.ValueChanged += Slider_ValueChanged;
            Draw();
            Loaded -= onLoaded!;
        });

        Loaded += onLoaded;

        LDensityLTB.TextBoxPart.TextChanged += (_, _) =>
        {
            if(double.TryParse(LDensityLTB.TextBoxPart.Text, out var rhoL))
            {
                (gas as PartitionedGas)!.LGas.Density = rhoL;
                Draw();
            }
        };
        RDensityLTB.TextBoxPart.TextChanged += (_, _) =>
        {
            if(double.TryParse(RDensityLTB.TextBoxPart.Text, out var rhoR))
            {
                (gas as PartitionedGas)!.RGas.Density = rhoR;
                Draw();
            }
        };
        LPressureLTB.TextBoxPart.TextChanged += (_, _) =>
        {
            if(double.TryParse(LPressureLTB.TextBoxPart.Text, out var pL))
            {
                (gas as PartitionedGas)!.LGas.Pressure = pL;
                Draw();
            }
        };
        RPressureLTB.TextBoxPart.TextChanged += (_, _) =>
        {
            if(double.TryParse(RPressureLTB.TextBoxPart.Text, out var pR))
            {
                (gas as PartitionedGas)!.RGas.Pressure = pR;
                Draw();
            }
        };


        // 状态初始化

        State = StateEnum.Idle;
        (gas as PartitionedGas)!.LGas = new InitialGas
        {
            Density = double.Parse(LDensityLTB.Text),
            Pressure = double.Parse(LPressureLTB.Text),
        };
        (gas as PartitionedGas)!.RGas = new InitialGas
        {
            Density = double.Parse(RDensityLTB.Text),
            Pressure = double.Parse(RPressureLTB.Text),
        };

        Draw();

        //下拉菜单
        foreach(var method in Enum.GetValues<FluentGas.MainMethodEnum>())
        {
            MainMethodComBox.Items.Add(new ComboBoxItem
            {
                Content = $"{method}",
            });
        }
        foreach(var method in Enum.GetValues<FluentGas.FluxMethodEnum>())
        {
            FluxMethodComBox.Items.Add(new ComboBoxItem
            {
                Content = $"{method}",
            });
        }
        foreach(var method in Enum.GetValues<FluentGas.TimeAdvanceMethodEnum>())
        {
            AdvanceMethodComBox.Items.Add(new ComboBoxItem
            {
                Content = $"{method}",
            });
        }
    }

    private void Draw()
    {
        foreach(var canvas in new Canvas[3] { DensityCanvas, PressureCanvas, VelocityCanvas })
        {
            canvas.Children.Clear();
        }

        var rhoMax = 1.1 * gas.Densitys.Max();
        DensityCanvas.Draw(gas.Densitys, (0, rhoMax));
        DensityMaxTextBlock.Text = $"{rhoMax}";

        var pMax = 1.1 * gas.Pressures.Max();
        PressureCanvas.Draw(gas.Pressures, (0, pMax));
        PressureMaxTextBlock.Text = $"{pMax}";

        var (vMin, vMax) = (gas.Velocitys.Min(), gas.Velocitys.Max());
        vMax *= vMax >= 0 ? 1.1 : 0.9;
        vMin *= vMin <= 0 ? 1.1 : 0.9;

        VelocityCanvas.Draw(gas.Velocitys, (vMin, vMax));
        VelocityMinTextBlock.Text = $"{vMin}";
        VelocityMaxTextBlock.Text = $"{vMax}";

    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        (gas as PartitionedGas)!.PartitionIndex = (int)e.NewValue;
        Draw();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if(true // 对齐用
            && double.TryParse(Delta_tTextBox.Text, out var dt)
            && double.TryParse(Delta_xTextBox.Text, out var dx)
            && double.TryParse(CFL_TextBox.Text, out var cfl))
        {
            State = StateEnum.Working;
            gas = new FluentGas(gas)
            {
                Delta_t = dt,
                Delta_x = dx,
                CFL = cfl,
            };
            Draw();
        }
        else
        {
            MessageBox.Show("请检查输入框");
        }
    }

    async Task Next()
    {
        await Task.Run((gas as FluentGas)!.ForwardEular);

        if(MainMethodComBox.SelectedItem is ComboBoxItem mainMethodItem
            && Enum.TryParse<FluentGas.MainMethodEnum>(mainMethodItem.Content.ToString(), out var mainMethod))
        {
            switch(mainMethod)
            {
                case FluentGas.MainMethodEnum.Godunov:
                    await Task.Run(() =>
                    {
                        (gas as FluentGas)!.Godunov(TheRoeFluxCalculator.RoeFluxCalculator);
                    });
                    break;
                case FluentGas.MainMethodEnum.DG:
                    break;
                default: MessageBox.Show("未知主方法"); break;
            }

            Delta_tTextBox.Text = $"{(gas as FluentGas)!.Delta_t}";
            Draw();
        }
    }

    private async void NextStepButton_Click(object sender, RoutedEventArgs e)
    {
        State = StateEnum.Busy;
        await Next();
        State = StateEnum.Working;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        State = StateEnum.Idle;
        gas = new PartitionedGas();
        PartitionPositionSlider.Value = 50;
        LDensityLTB.TextBoxPart.Text = $"{(gas as PartitionedGas)!.LGas.Density}";
        RDensityLTB.TextBoxPart.Text = $"{(gas as PartitionedGas)!.RGas.Density}";
        LPressureLTB.TextBoxPart.Text = $"{(gas as PartitionedGas)!.LGas.Pressure}";
        RPressureLTB.TextBoxPart.Text = $"{(gas as PartitionedGas)!.RGas.Pressure}";
        Draw();
    }

    private async void NextManyButton_Click(object sender, RoutedEventArgs e)
    {
        const int MAX_STEPS = 10000;
        State = StateEnum.Busy;
        for(int i = 1; i <= MAX_STEPS; i++)
        {
            StepCountTextBlock.Text = $"{i}/{MAX_STEPS}";
            await Next();
        }
        State = StateEnum.Working;
        StepCountTextBlock.Text = "";
    }

    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(string.Join("\t", gas.Velocitys.Select(v => $"{v}")));
    }

}
