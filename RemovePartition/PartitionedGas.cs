namespace RemovePartition;

public struct InitialGas(double density, double pressure)
{
    public const double DEFAULT_DENSITY = 1.3;
    public const double DEFAULT_PRESSURE = 100;
    public double Density { get; set; } = density;
    public double Pressure { get; set; } = pressure;
    public void operator *=(double ratio)
    {
        Density *= ratio;
        Pressure *= ratio;
    }
}

public class PartitionedGas : IGas
{
    public int PointsCount { get; init; } = IGas.DEFAULT_POINTS_COUNT;

    public const double DEFAULT_L_DENSITY = 2.0;
    public const double DEFAULT_L_PRESSURE = 2.0;
    public const double DEFAULT_R_DENSITY = 1.0;
    public const double DEFAULT_R_PRESSURE = 1.0;

    public InitialGas LGas = new(DEFAULT_L_DENSITY, DEFAULT_L_PRESSURE);
    public InitialGas RGas = new(DEFAULT_R_DENSITY, DEFAULT_R_PRESSURE);

    public bool IsIdealGas { get; set; } = true;

    /// <summary>
    /// 初始状态下隔板的位置，介于 value-1 和 value 之间
    /// </summary>
    public int PartitionIndex
    {
        get; set
        {
            if (value <= 0 || value >= PointsCount)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (IsIdealGas)
            {
                //tex: 
                //$$
                //\begin{align*}
                //  \rho_1L_1=\rho_2L_2\Rightarrow\rho_2=\frac{L_1}{L_2}\rho_1\\
                //  P\propto\rho\Rightarrow P_2=\frac{L_1}{L_2}P_1
                //\end{align*}
                //$$
                LGas *= (field - 0.0) / (value - 0.0);
                RGas *= (PointsCount - field) * 1.0 / (PointsCount - value);
            }
            field = value;
        }
    } = IGas.DEFAULT_POINTS_COUNT / 2;

    public double[] Densitys =>
    [
        .. Enumerable.Repeat(LGas.Density, PartitionIndex-0),
        .. Enumerable.Repeat(RGas.Density, PointsCount - PartitionIndex)
    ];
    public double[] Pressures =>
    [
        .. Enumerable.Repeat(LGas.Pressure, PartitionIndex-0),
        .. Enumerable.Repeat(RGas.Pressure, PointsCount - PartitionIndex)
    ];
}

