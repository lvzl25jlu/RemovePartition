using System.Diagnostics;
using System.Drawing.Drawing2D;
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
        Init,
        Removed,
        Busy
    }
    StateEnum State
    {
        get; set
        {
            field = value;
            switch(value)
            {
                case StateEnum.Init:
                    // 初始时可以调整输入
                    PartitionPosition_Sld.IsEnabled = true;
                    LDensity_TB.IsEnabled = true;
                    RDensity_TB.IsEnabled = true;
                    LPressure_TB.IsEnabled = true;
                    RPressure_TB.IsEnabled = true;
                    DeltaT_TB.IsEnabled = true;
                    DeltaX_TB.IsEnabled = true;
                    IdealGas_CB.IsEnabled = true;
                    DynamicInterval_CB.IsEnabled = true;
                    // 初始状态才能移除隔板
                    Remove_Btn.IsEnabled = true;
                    // 还没开始推进
                    Advace_Btn.IsEnabled = false;
                    // 可以重置
                    Reset_Btn.IsEnabled = true;
                    // 可以添加曲线
                    AddCurve_Btn.IsEnabled = true;
                    break;
                case StateEnum.Removed:
                    // 已经移除隔板了，调整输入没意义
                    PartitionPosition_Sld.IsEnabled = false;
                    LDensity_TB.IsEnabled = false;
                    RDensity_TB.IsEnabled = false;
                    LPressure_TB.IsEnabled = false;
                    RPressure_TB.IsEnabled = false;
                    DeltaT_TB.IsEnabled = false;
                    DeltaX_TB.IsEnabled = false;
                    // 此时理想气体没有意义
                    IdealGas_CB.IsEnabled = false;
                    // 可以选择是否使用动态步长
                    DynamicInterval_CB.IsEnabled = true;
                    // 无法移走已经被移走的隔板
                    Remove_Btn.IsEnabled = false;
                    // 可以推进
                    Advace_Btn.IsEnabled = true;
                    // 可以重置
                    Reset_Btn.IsEnabled = true;
                    // 不可以添加曲线
                    AddCurve_Btn.IsEnabled = false;
                    break;
                case StateEnum.Busy:
                    // 耐心等待计算完成，什么都不要干
                    PartitionPosition_Sld.IsEnabled = false;
                    LDensity_TB.IsEnabled = false;
                    RDensity_TB.IsEnabled = false;
                    LPressure_TB.IsEnabled = false;
                    RPressure_TB.IsEnabled = false;
                    DeltaT_TB.IsEnabled = false;
                    DeltaX_TB.IsEnabled = false;
                    IdealGas_CB.IsEnabled = false;
                    DynamicInterval_CB.IsEnabled = false;
                    Remove_Btn.IsEnabled = false;
                    Advace_Btn.IsEnabled = false;
                    Reset_Btn.IsEnabled = false;
                    AddCurve_Btn.IsEnabled = false;
                    break;
            }
        }
    }

    PartitionedGas pGas = new() { PointsCount = IGas.DEFAULT_POINTS_COUNT };
    List<FluentGas> fGases = [];


    public MainWindow()
    {
        InitializeComponent();

        // 设定一些初始值
        LDensity_TB.Text = $"{PartitionedGas.DEFAULT_L_DENSITY}";
        LPressure_TB.Text = $"{PartitionedGas.DEFAULT_L_PRESSURE}";
        RDensity_TB.Text = $"{PartitionedGas.DEFAULT_R_DENSITY}";
        RPressure_TB.Text = $"{PartitionedGas.DEFAULT_R_PRESSURE}";
        DeltaT_TB.Text = $"{FluentGas.DEFAULT_DT}";
        DeltaX_TB.Text = $"{FluentGas.DEFAULT_DX}";
        CFL_TB.Text = $"{FluentGas.DEFAULT_CFL}";
        CurTime_TB.Text = "0";
        StepCnt_TB.Text = "0";
        IdealGas_CB.IsChecked = true;
        DynamicInterval_CB.IsChecked = IsDynamicInterval;

        // 等所有控件加载完毕后再绑定事件
        // 否则加载时会触发 ValueChanged 事件
        // 此时画布还是 null

        RoutedEventHandler? onLoaded = null;
        onLoaded = new RoutedEventHandler((s, e) =>
        {
            PartitionPosition_Sld.ValueChanged += Slider_ValueChanged;
            Draw();
            Loaded -= onLoaded!;
        });
        Loaded += onLoaded;

        LDensity_TB.TextChanged += (_, _) =>
        {
            if(double.TryParse(LDensity_TB.Text, out var rhoL))
            {
                pGas.LGas.Density = rhoL;
                Draw();
            }
        };
        RDensity_TB.TextChanged += (_, _) =>
        {
            if(double.TryParse(RDensity_TB.Text, out var rhoR))
            {
                pGas.RGas.Density = rhoR;
                Draw();
            }
        };
        LPressure_TB.TextChanged += (_, _) =>
        {
            if(double.TryParse(LPressure_TB.Text, out var pL))
            {
                pGas.LGas.Pressure = pL;
                Draw();
            }
        };
        RPressure_TB.TextChanged += (_, _) =>
        {
            if(double.TryParse(RPressure_TB.Text, out var pR))
            {
                pGas.RGas.Pressure = pR;
                Draw();
            }
        };

        // 状态初始化
        State = StateEnum.Init;
        pGas.LGas = new InitialGas
        {
            Density = double.Parse(LDensity_TB.Text),
            Pressure = double.Parse(LPressure_TB.Text),
        };
        pGas.RGas = new InitialGas
        {
            Density = double.Parse(RDensity_TB.Text),
            Pressure = double.Parse(RPressure_TB.Text),
        };

        Draw();

        CurvePanel.Children.Add(new CurveInfo
        {
            ColorBrush = new SolidColorBrush(Colors.Red),
            MainMethod = FluentGas.MainMethodEnum.Godunov,
            FluxMethod = FluentGas.FluxMethodEnum.Roe,
            TimeMethod = FluentGas.TimeMethodEnum.RK3,
            Margin = new Thickness(5, 0, 5, 0),
        });
        CurvePanel.Children.Add(new CurveInfo
        {
            ColorBrush = new SolidColorBrush(Colors.Blue),
            MainMethod = FluentGas.MainMethodEnum.DG,
            FluxMethod = FluentGas.FluxMethodEnum.Roe,
            TimeMethod = FluentGas.TimeMethodEnum.RK3,
            Margin = new Thickness(5, 0, 5, 0),
        });
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

        foreach(var canvas in new Canvas[3] { Density_Canvas, Pressure_Canvas, Velocity_Canvas })
        {
            canvas.Children.Clear();
        }

        var allGases = fGases.Append<IGas>(pGas);

        var rhoMax = allGases
                    .Select(g => g.Densitys.Max())
                    .Max() * 1.1;
        if(Math.Abs(rhoMax - DensiCanvasRange.Max) / rhoMax > 0.1)
        {
            DensiCanvasRange = (0, rhoMax);
        }
        Density_Canvas.Draw(pGas.Densitys, DensiCanvasRange);
        for(int i = 0; i < fGases.Count; i++)
        {
            Density_Canvas.Draw(fGases[i].Densitys, DensiCanvasRange,
                (CurvePanel.Children[i] as CurveInfo)!.ColorBrush);
        }

        var pMax = allGases
                    .Select(g => g.Pressures.Max())
                    .Max() * 1.1;
        if(Math.Abs(pMax - PressCanvasRangeRange.Max) / pMax > 0.1)
        {
            PressCanvasRangeRange = (0, pMax);
        }
        Pressure_Canvas.Draw(pGas.Pressures, PressCanvasRangeRange);
        for(int i = 0; i < fGases.Count; i++)
        {
            Pressure_Canvas.Draw(fGases[i].Pressures, PressCanvasRangeRange,
                (CurvePanel.Children[i] as CurveInfo)!.ColorBrush);
        }

        var (uMin, uMax) = (allGases
                            .Select(g => g.Velocitys.Min())
                            .Min(), allGases
                            .Select(g => g.Velocitys.Max())
                            .Max());
        uMax *= uMax >= 0 ? 1.1 : 0.9;
        uMin *= uMin <= 0 ? 1.1 : 0.9;
        if(uMax- uMin < 1e-5)
        {
            uMax += 0.5;
            uMin -= 0.5;
        }

        VelocCanvasRange = (uMin, uMax);
        Velocity_Canvas.Draw(pGas.Velocitys, VelocCanvasRange);
        for(int i = 0; i < fGases.Count; i++)
        {
            Velocity_Canvas.Draw(fGases[i].Velocitys, VelocCanvasRange,
                (CurvePanel.Children[i] as CurveInfo)!.ColorBrush);
        }
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        pGas.PartitionIndex = (int)e.NewValue;

        // 这段代码会触发 TextChanged 事件
        // 但是不影响结果
        LDensity_TB.Text = $"{pGas.LGas.Density}";
        RDensity_TB.Text = $"{pGas.RGas.Density}";
        LPressure_TB.Text = $"{pGas.LGas.Pressure}";
        RPressure_TB.Text = $"{pGas.RGas.Pressure}";

        Draw();
    }

    double curTime = 0;
    int stepCnt = 0;
    double dt = 0;
    double dx = 0;
    double cfl = 0;

    private void Remove_Btn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            dt = double.Parse(DeltaT_TB.Text);
            dx = double.Parse(DeltaX_TB.Text);
            cfl = double.Parse(CFL_TB.Text);
            foreach(var info in CurvePanel.Children.Cast<CurveInfo>())
            {
                // 通量方法是泛型无关的
                FluxCalculator fluxMethod = info.FluxMethod switch
                {
                    FluentGas.FluxMethodEnum.LF => FluxCalculators.LaxFriedrichs,
                    FluentGas.FluxMethodEnum.Roe => FluxCalculators.Roe,
                    FluentGas.FluxMethodEnum.HLL => FluxCalculators.HLL,
                    FluentGas.FluxMethodEnum.HLLC => FluxCalculators.HLLC,
                    _ => throw new NotImplementedException($"不支持的通量方法：{info.FluxMethod}"),
                };

                fGases.Add(info.MainMethod switch
                {
                    // Godunov 方法使用有限体积数据
                    FluentGas.MainMethodEnum.Godunov => new FluentGas<FvmData>(pGas)
                    {
                        FluxCalculator = fluxMethod,
                        RhsCalculator = fluxMethod.Godunov,                 
                        TimeAdvancer = info.TimeMethod switch
                        {
                            FluentGas.TimeMethodEnum.Eular => RungeKutta.Eular,
                            FluentGas.TimeMethodEnum.RK3 => RungeKutta.RK3,
                            _ => throw new NotImplementedException($"不支持的时间推进方法：{info.TimeMethod}"),
                        },
                        DeltaT = dt,
                        DeltaX = dx,
                        CFL = cfl,
                    },
                    // DG 方法使用DG数据
                    FluentGas.MainMethodEnum.DG => new FluentGas<DgData>(pGas)
                    {
                        FluxCalculator = fluxMethod,
                        RhsCalculator = fluxMethod.DG,
                        TimeAdvancer = info.TimeMethod switch
                        {
                            FluentGas.TimeMethodEnum.Eular => RungeKutta.Eular,
                            FluentGas.TimeMethodEnum.RK3 => RungeKutta.RK3,
                            _ => throw new NotImplementedException($"不支持的时间推进方法：{info.TimeMethod}"),
                        },
                        DeltaT = dt,
                        DeltaX = dx,
                        CFL = cfl,
                    },
                    _ => throw new NotImplementedException($"不支持的主要方法：{info.MainMethod}"),
                });
            }
            Draw();
            State = StateEnum.Removed;
            curTime = 0;
            stepCnt = 0;
        }
        catch(Exception ex)
        {
            MessageBox.Show($"{ex.Message}","出错了");
        }
    }

    private void Reset_Btn_Click(object sender, RoutedEventArgs e)
    {
        State = StateEnum.Init;
        curTime = 0;
        stepCnt = 0;
        fGases = [];
        CurvePanel.Children.Clear();
        pGas = new();
        PartitionPosition_Sld.Value = pGas.PartitionIndex;
        LDensity_TB.Text = $"{pGas.LGas.Density}";
        RDensity_TB.Text = $"{pGas.RGas.Density}";
        LPressure_TB.Text = $"{pGas.LGas.Pressure}";
        RPressure_TB.Text = $"{pGas.RGas.Pressure}";
        Draw();
    }

    private async void Advance_Btn_Click(object sender, RoutedEventArgs e)
    {
        if(!int.TryParse(StepNum_TB.Text, out var step))
        {
            MessageBox.Show($"请输入一个整数");
            return;
        }
        State = StateEnum.Busy;
        try
        {
            for(int i = 1; i <= step; i++)
            {
                foreach(var fGas in fGases)
                {
                    await fGas.Advance();
                }
                curTime += dt;
                stepCnt += 1;
                if(IsDynamicInterval)
                {
                    dt = fGases
                        .Select(g => g.UpdateDeltaT())
                        .Min();
                    foreach(var fGas in fGases)
                    {
                        fGas.DeltaT = dt;
                    }
                }
                DeltaT_TB.Text = $"{dt}";
                Draw();
            }
            State = StateEnum.Removed;
        }
        catch(Exception ex)
        {
            MessageBox.Show(ex.Message, "出错了");
        }
    }

    bool IsDynamicInterval { get; set; } = false;
    private void DynamicInterval_CB_Click(object sender, RoutedEventArgs e)
        => IsDynamicInterval = DynamicInterval_CB.IsChecked ?? false;

    private void IdealGas_CB_Click(object sender, RoutedEventArgs e)
        => pGas.IsIdealGas = IdealGas_CB.IsChecked ?? false;

    private void AddCurve_Btn_Click(object sender, RoutedEventArgs e)
    {
        var window = new CurveWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if(window.ShowDialog() == true)
        {
            var color = window.SelectedColor ?? Colors.Black;
            var info = new CurveInfo
            {
                ColorBrush = new SolidColorBrush(color),
                MainMethod = window.SelectedRhsMethod,
                FluxMethod = window.SelectedFluxMethod,
                TimeMethod = window.SelectedTimeMethod,
                Margin = new Thickness(5, 0, 5, 0),
            };
            CurvePanel.Children.Add(info);
        }
    }
}
