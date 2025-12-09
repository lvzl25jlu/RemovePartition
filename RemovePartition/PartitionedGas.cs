namespace RemovePartition;

public struct InitialGas()
{
    public const double DEFAULT_DENSITY = 1.3;
    public const double DEFAULT_PRESSURE = 100;
    public double Density { get; set; } = DEFAULT_DENSITY;
    public double Pressure { get; set; } = DEFAULT_PRESSURE;

}

public class PartitionedGas : IGas
{
    public int PointsCount { get; set; } = IGas.DEFAULT_POINTS_COUNT;

    int partitionIndex = IGas.DEFAULT_POINTS_COUNT / 2;

    public InitialGas LGas = new();
    public InitialGas RGas = new();

    /// <summary>
    /// 初始状态下隔板的位置，介于 value-1 和 value 之间
    /// </summary>
    public int PartitionIndex
    {
        get => partitionIndex; set
        {
            if(value <= 0 || value >= PointsCount)
                throw new ArgumentOutOfRangeException(nameof(value));
            partitionIndex = value;
        }
    }

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

