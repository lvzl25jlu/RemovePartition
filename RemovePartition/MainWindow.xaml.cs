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
    StateEnum stateEnum;
    StateEnum State
    {
        get => stateEnum; set
        {
            stateEnum = value;
            switch(value)
            {
                case StateEnum.Idle:
                    // 空闲时可以调整输入
                    PartitionPositionSlider.IsEnabled = true;
                    Delta_tTextBox.IsEnabled = true;
                    Delta_xTextBox.IsEnabled = true;
                    // 空闲时（初始状态）才能移除隔板
                    RemoveButton.IsEnabled = true;
                    // 还没开始推进
                    NextStepButton.IsEnabled = false;
                    NextThousandButton.IsEnabled = false;
                    // 可以重置
                    ResetButton.IsEnabled = true;
                    break;
                case StateEnum.Working:
                    // 工作时不许调整输入
                    PartitionPositionSlider.IsEnabled = false;
                    Delta_tTextBox.IsEnabled = false;
                    Delta_xTextBox.IsEnabled = false;
                    // 无法移走已经被移走的隔板
                    RemoveButton.IsEnabled = false;
                    // 可以推进
                    NextStepButton.IsEnabled = true;
                    NextThousandButton.IsEnabled = true;
                    // 可以重置
                    ResetButton.IsEnabled = true;
                    break;
                case StateEnum.Busy:
                    // 耐心等待计算完成，什么都不要干
                    PartitionPositionSlider.IsEnabled = false;
                    Delta_tTextBox.IsEnabled = false;
                    Delta_xTextBox.IsEnabled = false;
                    RemoveButton.IsEnabled = false;
                    NextStepButton.IsEnabled = false;
                    NextThousandButton.IsEnabled = false;
                    ResetButton.IsEnabled = false;
                    break;
            }
        }
    }

    IGas gas = new PartitionedGas();

    public MainWindow()
    {
        InitializeComponent();

        State = StateEnum.Idle;

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
    }

    private void Draw()
    {
        foreach(var canvas in new Canvas[3] { DensityCanvas, PressureCanvas, VelocityCanvas })
        {
            canvas.Children.Clear();
        }
        var denMax =1.1* gas.Densitys.Max();
        DensityCanvas.Draw(gas.Densitys,(0,denMax));
        DensityMaxTextBlock.Text = $"{denMax}";
        var preMax=1.1*gas.Pressures.Max();
        PressureCanvas.Draw(gas.Pressures,(0,preMax));
        PressureMaxTextBlock.Text = $"{preMax}";
        var (vMin,vMax)=(gas.Densitys.Min(),gas.Densitys.Max());
        vMin *= vMin > 0 ? 0.9 : 1.1;
        vMax *= vMax > 0 ? 1.1 : 0.9;
        VelocityCanvas.Draw(gas.Velocitys, (vMin, vMax));
        VelocityMinTextBlock.Text=$"{vMin}";
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
            && double.TryParse(Delta_xTextBox.Text, out var dx))
        {
            State = StateEnum.Working;
            gas = new FluentGas(gas)
            {
                Delta_t = dt,
                Delta_x = dx,
            };
            Draw();
        }
        else
        {
            MessageBox.Show("请检查输入框");
        }
    }

    private void NextStepButton_Click(object sender, RoutedEventArgs e)
    {
        (gas as FluentGas)!.ForwardEular();
        Draw();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        State = StateEnum.Idle;
        gas = new PartitionedGas();
        PartitionPositionSlider.Value = 50;
    }

    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(string.Join("\t", (gas as FluentGas)!.Velocity.Select(v => $"{v}")));
    }

    private void NextThousandButton_Click(object sender, RoutedEventArgs e)
    {
        var dt = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };
        var cnt = 0;
        State = StateEnum.Busy;
        dt.Tick += new EventHandler((_, _) =>
        {
            (gas as FluentGas)!.ForwardEular();
            Draw();
            if(++cnt > 1000)
            {
                dt.Stop();
                State = StateEnum.Working;
            }
        });
        dt.Start();

    }
}
