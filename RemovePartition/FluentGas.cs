using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using static RemovePartition.RshCalculators;
using static RemovePartition.FluentGas;

namespace RemovePartition;

/// <summary>
/// 非泛型版本用来提供一些默认值
/// </summary>
public abstract class FluentGas : IGas
{
    public const double DEFAULT_CFL = 0.3;
    public const double DEFAULT_DX = 0.01;
    public const double DEFAULT_DT = 0.001;

    public abstract int PointsCount { get; init; }
    public abstract double[] Densitys { get; }
    public abstract double[] Pressures { get; }
    public abstract double[] Velocitys { get; }

    public double DeltaX { get; set; } = DEFAULT_DX;
    public double DeltaT { get; set; } = DEFAULT_DT;
    public double CFL { get; set; } = DEFAULT_CFL;
    public abstract double UpdateDeltaT();

    public enum MainMethodEnum
    {
        Godunov,
        DG
    }

    public enum FluxMethodEnum
    {
        Roe,
        LF,
        HLL,
        HLLC
    }

    public enum TimeMethodEnum
    {
        Eular,
        RK3
    }

    public abstract Task Advance();
}

/// <summary>
/// 流动气体类
/// </summary>
/// <typeparam name="TData">
/// Type of Data
/// 网格中存储的数据类型
/// </typeparam>
class FluentGas<TData> : FluentGas
    where TData : struct, IGridData<TData>
{
    public FluentGas()
    {
        PointsCount = IGas.DEFAULT_POINTS_COUNT;
    }
    public FluentGas(IGas that)
    {
        PointsCount = that.PointsCount;
        //手动缓存优化
        var (densitys, pressures, velocitys) = (that.Densitys, that.Pressures, that.Velocitys);
        for(int i = 0; i < PointsCount; i++)
        {
            fluField.Grids[i].FluVar = FluidVaribles.FromPrimitVars(densitys[i], velocitys[i], pressures[i]);
        }
    }

    public override double[] Densitys => [.. fluField.Select(g => g.FluVar.Density)];
    public override double[] Pressures => [.. fluField.Select(g => g.FluVar.Pressure)];
    public override double[] Velocitys => [.. fluField.Select(g => g.FluVar.Velocity)];

    public override double UpdateDeltaT()
    {
        //tex:$\Delta t=\text{CFL}\times\frac{\Delta x}{ \left|u\right|+c}$
        return DeltaT = CFL * DeltaX / (fluField.Select(g => g.FluVar.SoundSpeed + Math.Abs(g.FluVar.Velocity)).Max());
    }

    /// <summary>
    /// 流场
    /// </summary>
    FluidField<TData> fluField = new();

    public override int PointsCount
    {
        get => fluField.Length; init => fluField = new()
        {
            Grids = [.. Enumerable.Range(0, value).Select(i => new Grid<TData>
            {
                X=(i + 0.5) * DeltaX,
                W=DeltaX,
                FluVar = new()
            })]
        };
    }

    /// <summary>
    /// 主方法
    /// 必须先设置 FluxMethod
    /// </summary>
    public RhsCalculator<TData> RhsCalculator
    {
        get; init;
    } = ff => throw new NotImplementedException();

    /// <summary>
    /// 计算通量使用的方法
    /// 必须在 MainMethod 之前设置
    /// </summary>
    public FluxCalculator FluxCalculator
    {
        get; init;
    } = (l, r) => throw new NotImplementedException();

    /// <summary>
    /// 时间推进方法
    /// </summary>
    public TimeAdvancer<TData> TimeAdvancer
    {
        get; init;
    } = (ff, dt, rhs) => throw new NotImplementedException();
    /// <summary>
    /// 推进流场到下一时刻
    /// </summary>
    /// <returns>仅可等待</returns>
    public override async Task Advance()
        => await Task.Run(() => fluField = TimeAdvancer(fluField, DeltaT, RhsCalculator));

}