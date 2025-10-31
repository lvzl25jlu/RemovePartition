namespace RemovePartition;

public struct IdealGas()
{
    public double Pressure { get; set; } = 10;
    public double Density { get; set; } = 10;
}

public class PartitionedGas : IGas
{
    public int PointsCount { get; set; } = IGas.DEFAULT_POINTS_COUNT;

    int partitionIndex =IGas.DEFAULT_POINTS_COUNT / 2;

    IdealGas leftGas = new();
    IdealGas rightGas = new();
    /// <summary>
    /// 初始状态下隔板的位置
    /// 隔板放在编号为 PartitionIndex 的点的左侧
    /// </summary>
    public int PartitionIndex
    {
        get => partitionIndex; set
        {
            if (value <= 0 || value >= PointsCount)
                throw new ArgumentOutOfRangeException(nameof(value));
            //tex: 根据气体等温变化规律
            // $$
            // \frac {p_1}{\rho_1}=\frac {p_2}{\rho_2}
            // $$
            // 如果体积变为原本的 $\eta$ 倍，
            // 则 $\rho_2=\frac{\rho_1}\eta$ ，那么
            // $$
            // p_2=\frac{\rho_2}{\rho_1}p_1=\frac {p_1}\eta
            // $$
            // 因此移动隔板后同时给两项除以体积的变化倍数即可

            // 转成浮点数，下同
            var eta = (value - 0) * 1.0 / (partitionIndex - 0);
            leftGas.Density /= eta;
            leftGas.Pressure /= eta;

            var mu = (PointsCount - value) * 1.0 / (PointsCount - partitionIndex);
            rightGas.Density /= mu;
            rightGas.Pressure /= mu;

            partitionIndex = value;
        }
    }

    public double[] Densitys =>
    [
        .. Enumerable.Repeat(leftGas.Density, PartitionIndex),
        .. Enumerable.Repeat(rightGas.Density, PointsCount - PartitionIndex)
    ];
    public double[] Pressures =>
    [
        .. Enumerable.Repeat(leftGas.Pressure, PartitionIndex),
        .. Enumerable.Repeat(rightGas.Pressure, PointsCount - PartitionIndex)
    ];

}

