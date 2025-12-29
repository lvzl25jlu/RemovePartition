using System.Diagnostics;
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
    StateEnum State
    {
        get; set
        {
            field = value;
            switch (value)
            {
                case StateEnum.Idle:
                    // 空闲时可以调整输入
                    PartitionPositionSlider.IsEnabled = true;
                    LDensity_TB.IsEnabled = true;
                    RDensity_TB.IsEnabled = true;
                    LPressure_TB.IsEnabled = true;
                    RPressure_TB.IsEnabled = true;
                    Delta_t_TB.IsEnabled = true;
                    Delta_x_TB.IsEnabled = true;
                    // 空闲时（初始状态）才能移除隔板
                    RemoveButton.IsEnabled = true;
                    // 还没开始推进
                    NextStepButton.IsEnabled = false;
                    NextManyButton.IsEnabled = false;
                    // 可以重置
                    ResetButton.IsEnabled = true;
                    // 可以选择方法
                    MainMethod_CB.IsEnabled = true;
                    FluxMethod_CB.IsEnabled = true;
                    AdvanceMethod_CB.IsEnabled = true;
                    break;
                case StateEnum.Working:
                    // 工作时不许调整输入
                    PartitionPositionSlider.IsEnabled = false;
                    LDensity_TB.IsEnabled = false;
                    RDensity_TB.IsEnabled = false;
                    LPressure_TB.IsEnabled = false;
                    RPressure_TB.IsEnabled = false;
                    Delta_t_TB.IsEnabled = false;
                    Delta_x_TB.IsEnabled = false;
                    // 无法移走已经被移走的隔板
                    RemoveButton.IsEnabled = false;
                    // 可以推进
                    NextStepButton.IsEnabled = true;
                    NextManyButton.IsEnabled = true;
                    // 可以重置
                    ResetButton.IsEnabled = true;
                    // 可以选择方法
                    MainMethod_CB.IsEnabled = true;
                    FluxMethod_CB.IsEnabled = true;
                    AdvanceMethod_CB.IsEnabled = true;
                    break;
                case StateEnum.Busy:
                    // 耐心等待计算完成，什么都不要干
                    PartitionPositionSlider.IsEnabled = false;
                    LDensity_TB.IsEnabled = false;
                    RDensity_TB.IsEnabled = false;
                    LPressure_TB.IsEnabled = false;
                    RPressure_TB.IsEnabled = false;
                    Delta_t_TB.IsEnabled = false;
                    Delta_x_TB.IsEnabled = false;
                    RemoveButton.IsEnabled = false;
                    NextStepButton.IsEnabled = false;
                    NextManyButton.IsEnabled = false;
                    ResetButton.IsEnabled = false;
                    MainMethod_CB.IsEnabled = false;
                    FluxMethod_CB.IsEnabled = false;
                    AdvanceMethod_CB.IsEnabled = false;
                    break;
            }
        }
    }

    IGas gas = new PartitionedGas() { PointsCount = IGas.DEFAULT_POINTS_COUNT };
    double curTime = 0;
    int stepCnt = 0;

    public MainWindow()
    {
        InitializeComponent();

        // 设定一些初始值
        LDensity_TB.Text = $"{PartitionedGas.DEFAULT_L_DENSITY}";
        LPressure_TB.Text = $"{PartitionedGas.DEFAULT_L_PRESSURE}";
        RDensity_TB.Text = $"{PartitionedGas.DEFAULT_R_DENSITY}";
        RPressure_TB.Text = $"{PartitionedGas.DEFAULT_R_PRESSURE}";
        Delta_t_TB.Text = $"{FluentGas.DEFAULT_DT}";
        Delta_x_TB.Text = $"{FluentGas.DEFAULT_DX}";
        CFL_TB.Text = $"{FluentGas.DEFAULT_CFL}";
        CurTime_TB.Text = "0";
        StepCnt_TB.Text = "0";

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

        LDensity_TB.TextChanged += (_, _) =>
        {
            if (double.TryParse(LDensity_TB.Text, out var rhoL))
            {
                (gas as PartitionedGas)!.LGas.Density = rhoL;
                Draw();
            }
        };
        RDensity_TB.TextChanged += (_, _) =>
        {
            if (double.TryParse(RDensity_TB.Text, out var rhoR))
            {
                (gas as PartitionedGas)!.RGas.Density = rhoR;
                Draw();
            }
        };
        LPressure_TB.TextChanged += (_, _) =>
        {
            if (double.TryParse(LPressure_TB.Text, out var pL))
            {
                (gas as PartitionedGas)!.LGas.Pressure = pL;
                Draw();
            }
        };
        RPressure_TB.TextChanged += (_, _) =>
        {
            if (double.TryParse(RPressure_TB.Text, out var pR))
            {
                (gas as PartitionedGas)!.RGas.Pressure = pR;
                Draw();
            }
        };

        // 状态初始化
        State = StateEnum.Idle;
        (gas as PartitionedGas)!.LGas = new InitialGas
        {
            Density = double.Parse(LDensity_TB.Text),
            Pressure = double.Parse(LPressure_TB.Text),
        };
        (gas as PartitionedGas)!.RGas = new InitialGas
        {
            Density = double.Parse(RDensity_TB.Text),
            Pressure = double.Parse(RPressure_TB.Text),
        };

        Draw();

        //下拉菜单
        foreach (var method in Enum.GetValues<FluentGas.MainMethodEnum>())
        {
            MainMethod_CB.Items.Add(new ComboBoxItem
            {
                Content = $"{method}",
            });
        }
        foreach (var method in Enum.GetValues<FluentGas.FluxMethodEnum>())
        {
            FluxMethod_CB.Items.Add(new ComboBoxItem
            {
                Content = $"{method}",
            });
        }
        foreach (var method in Enum.GetValues<FluentGas.TimeAdvanceMethodEnum>())
        {
            AdvanceMethod_CB.Items.Add(new ComboBoxItem
            {
                Content = $"{method}",
            });
        }
    }

    (double Min, double Max) DensiCanvasRange
    {
        get; set
        {
            field = value;
            DensityMax_TB.Text = $"{field.Max}";
        }
    } = (0, 2);
    (double Min, double Max) PressCanvasRangeRange
    {
        get; set
        {
            field = value;
            PressureMax_TB.Text = $"{field.Max}";
        }
    } = (0, 2);
    (double Min, double Max) VelocCanvasRange
    {
        get; set
        {
            field = value;
            VelocityMin_TB.Text = $"{field.Min}";
            VelocityMax_TB.Text = $"{field.Max}";
        }
    } = (-1, 1);

    private void Draw()
    {
        CurTime_TB.Text = $"{curTime:F4}";
        StepCnt_TB.Text = $"{stepCnt}";

        foreach (var canvas in new Canvas[3] { Density_Canvas, Pressure_Canvas, Velocity_Canvas })
        {
            canvas.Children.Clear();
        }

        var rhoMax = 1.1 * gas.Densitys.Max();
        if (Math.Abs(rhoMax - DensiCanvasRange.Max) / rhoMax > 0.1)
        {
            DensiCanvasRange = (0, rhoMax);
        }
        Density_Canvas.Draw(gas.Densitys, DensiCanvasRange);

        var pMax = 1.1 * gas.Pressures.Max();
        if (Math.Abs(pMax - PressCanvasRangeRange.Max) / pMax > 0.1)
        {
            PressCanvasRangeRange = (0, pMax);
        }
        Pressure_Canvas.Draw(gas.Pressures, PressCanvasRangeRange);

        var (uMin, uMax) = (gas.Velocitys.Min(), gas.Velocitys.Max());
        uMax *= uMax >= 0 ? 1.1 : 0.9;
        uMin *= uMin <= 0 ? 1.1 : 0.9;

        VelocCanvasRange = (Math.Abs(uMin - VelocCanvasRange.Min) / (Math.Abs(uMin) + 1e-10) > 0.1,
            Math.Abs(uMax - VelocCanvasRange.Max) / (Math.Abs(uMax) + 1e-10) > 0.1) switch
        {
            (true, true) => (uMin, uMax),
            (true, false) => (uMin, VelocCanvasRange.Max),
            (false, true) => (VelocCanvasRange.Min, uMax),
            (false, false) => VelocCanvasRange,
        };
        Velocity_Canvas.Draw(gas.Velocitys, VelocCanvasRange);
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        (gas as PartitionedGas)!.PartitionIndex = (int)e.NewValue;
        Draw();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (true // 对齐用
            && double.TryParse(Delta_t_TB.Text, out var dt)
            && double.TryParse(Delta_x_TB.Text, out var dx)
            && double.TryParse(CFL_TB.Text, out var cfl))
        {
            State = StateEnum.Working;
            curTime = 0;
            stepCnt = 0;
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
        var dt = (gas as FluentGas)!.Delta_t;
        try
        {
            FluxCalculator fluxMethod = Enum.Parse<FluentGas.FluxMethodEnum>(
                (FluxMethod_CB.SelectedItem as ComboBoxItem)!.Content.ToString()!) switch
            {
                FluentGas.FluxMethodEnum.Roe => FluxCalculators.Roe,
                FluentGas.FluxMethodEnum.LaxFriedrichs => FluxCalculators.LaxFriedrichs,
                FluentGas.FluxMethodEnum.HLL => FluxCalculators.HLL,
                FluentGas.FluxMethodEnum.HLLC => FluxCalculators.HLLC,
                _ => throw new UnreachableException(),
            };
            var mainMethod = Enum.Parse<FluentGas.MainMethodEnum>(
                (MainMethod_CB.SelectedItem as ComboBoxItem)!.Content.ToString()!);
            switch (mainMethod)
            {
                case FluentGas.MainMethodEnum.Godunov:
                    await Task.Run(() =>
                    {
                        (gas as FluentGas)!.Godunov(fluxMethod);
                    });
                    break;
                case FluentGas.MainMethodEnum.DG: break;
                default: throw new UnreachableException();
            }
        }
        catch (Exception)
        {
            MessageBox.Show("请检查下拉菜单");
            return;
        }

        curTime += dt;
        stepCnt += 1;
        Delta_t_TB.Text = $"{(gas as FluentGas)!.Delta_t}";
        Draw();
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
        curTime = 0;
        stepCnt = 0;
        PartitionPositionSlider.Value = (gas as PartitionedGas)!.PartitionIndex;
        LDensity_TB.Text = $"{(gas as PartitionedGas)!.LGas.Density}";
        RDensity_TB.Text = $"{(gas as PartitionedGas)!.RGas.Density}";
        LPressure_TB.Text = $"{(gas as PartitionedGas)!.LGas.Pressure}";
        RPressure_TB.Text = $"{(gas as PartitionedGas)!.RGas.Pressure}";
        Draw();
    }

    private async void NextManyButton_Click(object sender, RoutedEventArgs e)
    {
        const int STEP_CNT = 10000;
        State = StateEnum.Busy;
        for (int i = 1; i <= STEP_CNT; i++)
        {
            await Next();
        }
        State = StateEnum.Working;
    }

    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(string.Join("\t", gas.Velocitys.Select(v => $"{v}")));
    }

}
