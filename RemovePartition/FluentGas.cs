using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RemovePartition;


partial class FluentGas : IGas
{
    public const double DEFAULT_CFL = 0.3;
    public const double DEFAULT_DX = 0.01;
    public const double DEFAULT_DT = 0.001;

    public FluentGas()
    {
        PointsCount = IGas.DEFAULT_POINTS_COUNT;
    }

    public FluentGas(IGas that)
    {
        PointsCount = that.PointsCount;
        //手动缓存优化
        var (densitys, pressures, velocitys) = (that.Densitys, that.Pressures, that.Velocitys);
        for (int i = 0; i < PointsCount; i++)
        {
            Ps[i].Density = densitys[i];
            Ps[i].Pressure = pressures[i];
            Ps[i].Velocity = velocitys[i];
        }
    }

    FluidVaribles[] points = [.. Enumerable.Repeat(new FluidVaribles(), IGas.DEFAULT_POINTS_COUNT)];
    // 起个短点的名字
    FluidVaribles[] Ps => points;

    public int PointsCount
    {
        get => Ps.Length; init
        {
            points = [.. Enumerable.Repeat(new FluidVaribles(), value)];
        }
    }

    public double DeltaX { get; set; } = DEFAULT_DX;
    public double DeltaT { get; set; } = DEFAULT_DT;
    public double CFL { get; set; } = DEFAULT_CFL;

    public void UpdateDelta_t()
    {
        //tex:$\Delta t=\text{CFL}\times\frac{\Delta x}{ \left|u\right|+c}$
        DeltaT = CFL * DeltaX / (Ps.Select(p => p.SoundSpeed + Math.Abs(p.Velocity)).Max());
    }

    public double[] Densitys => [.. Ps.Select(p => p.Density)];
    public double[] Pressures => [.. Ps.Select(p => p.Pressure)];
    public double[] Velocitys => [.. Ps.Select(p => p.Velocity)];

    public enum MainMethodEnum
    {
        Godunov,
        DG
    }

    public enum FluxMethodEnum
    {
        Roe,
        LaxFriedrichs,
        HLL,
        HLLC
    }

    public enum TimeAdvanceMethodEnum
    {
        ForwardEular,
        RK3
    }

}