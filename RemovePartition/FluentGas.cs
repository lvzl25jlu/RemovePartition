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
        get => Ps.Length; set
        {
            points = [.. Enumerable.Repeat(new FluidVaribles(), value)];
        }
    }

    public double Delta_x { get; set; } = 0.01;
    public double Delta_t { get; set; } = 0.001;
    public double CFL { get; set; }

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
        HLLC
    }

    public enum TimeAdvanceMethodEnum
    {
        ForwardEular,
        RK3
    }

}