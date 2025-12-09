using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RemovePartition;

//tex:用 $\rho$ 、 $u$ 、 $p$ 作为原始变量
public struct Physics
{
    public static double gamma { get; set; } = 1.3f;
    public double Density { get; set; }
    public double Pressure { get; set; }
    public double Velocity { get; set; }
    //tex:$c=\sqrt{\gamma\frac p\rho}$
    public readonly double SoundSpeed => Math.Sqrt(gamma * Pressure / Density);
    public double U_rho
    {
        //tex: $U_\rho=\rho$
        readonly get => Density;
        //tex: $\rho=U_\rho$
        set => Density = Math.Max(0, value);
    }
    public double U_p
    {
        //tex: $U_p=u\rho$
        readonly get => Density * Velocity;
        //tex: $u=\frac {U_p}\rho$
        set => Velocity = Density < 1e-6 ? 0 : value / Density;
    }
    public double U_E
    {
        //tex: $U_E=\rho E=\frac{p}{\gamma-1}+\frac{1}{2}\rho u^2$
        readonly get => Pressure / (gamma - 1) + Velocity.Square() / 2;
        //tex: $p=\left(\gamma-1\right)\left(U_E-\frac 12\rho u^2\right)$
        set => Pressure = ((gamma - 1) * (value - Density * Velocity.Square() / 2)).N0PX();
    }

    //tex: $F_\rho=\rho u$
    public readonly double F_rho => Density * Velocity;
    //tex: $F_p=\rho u^2+p$
    public readonly double F_p => Density * Velocity.Square() + Pressure;
    //tex: $F_E=u\left(\rho E+p\right)$ where $\rho E=U_E$
    public readonly double F_E => Velocity * (U_E + Pressure);
}



public class FluentGas : IGas
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

    Physics[] points = [.. Enumerable.Repeat(new Physics(), IGas.DEFAULT_POINTS_COUNT)];
    // 起个短点的名字
    Physics[] Ps => points;

    public int PointsCount
    {
        get => Ps.Length; set
        {
            points = [.. Enumerable.Repeat(new Physics(), value)];
        }
    }

    public double Delta_x { get; set; } = 0.01;
    public double Delta_t { get; set; } = 0.001;
    public double CFL { get; set; }

    public double[] Densitys => [.. Ps.Select(p => p.Density)];
    public double[] Pressures => [.. Ps.Select(p => p.Pressure)];
    public double[] Velocitys => [.. Ps.Select(p => p.Velocity)];

    // 一阶欧拉向前差分
    public void ForwardEular()
    {
        //tex: 
        // $$
        // \frac {\partial F}{\partial x} = \begin{cases}
        //      \frac{F_{1}-F_{0}}{\Delta x} & j=0\\
        //      \frac{F_{j+1}-F_{j-1}}{2\Delta x} & 0 \lt j \lt N-1\\
        //      \frac{F_{N-1}-F_{N-2}}{\Delta x} & j=N-1
        //  \end{cases}
        // $$
        var part_x_F = new (double rho, double p, double E)[PointsCount];
        part_x_F[0] = (
            (Ps[1].F_rho - Ps[0].F_rho) / Delta_x,
            (Ps[1].F_p - Ps[0].F_p) / Delta_x,
            (Ps[1].F_E - Ps[0].F_E) / Delta_x
        );
        for(int j = 0 + 1; j < PointsCount - 1; j++)
        {
            part_x_F[j] = (
                (Ps[j + 1].F_rho - Ps[j - 1].F_rho) / Delta_x / 2,
                (Ps[j + 1].F_p - Ps[j - 1].F_p) / Delta_x / 2,
                (Ps[j + 1].F_E - Ps[j - 1].F_E) / Delta_x / 2
            );
        }
        part_x_F[^1] = (
            (Ps[^1].F_rho - Ps[^2].F_rho) / Delta_x,
            (Ps[^1].F_p - Ps[^2].F_p) / Delta_x,
            (Ps[^1].F_E - Ps[^2].F_E) / Delta_x
        );
        //tex: $$U^{\left(n+1\right)}=U^{\left(n\right)}
        //      -\frac {\partial F}{\partial x}\Delta t$$
        var next = new Physics[PointsCount];
        for(int j = 0; j < PointsCount; j++)
        {
            next[j].U_rho = Ps[j].U_rho - part_x_F[j].rho * Delta_t;
            next[j].U_p = Ps[j].U_p - part_x_F[j].p * Delta_t;
            next[j].U_E = Ps[j].U_E - part_x_F[j].E * Delta_t;
        }
        points = next;
        //tex:$\Delta t=\text{CFL}\times\frac{\Delta x}{ \left|u\right|+c}$

        //Delta_t = CFL * Delta_x / (Ps.Select(p => p.SoundSpeed + Math.Abs(p.Velocity)).Max());
    }

    // 一阶欧拉向前差分改进版
    public void ForwardEularEx()
    {
        //tex: 通过手动展开计算式：
        //$$
        //  \begin{align*}
        //      \frac{\partial \rho}{\partial t}=-\left(\rho\frac{\partial u}{\partial x}+u\frac{\partial \rho}{\partial x}\right)\\
        //      \frac{\partial u}{\partial t}=-\left(u\frac{\partial u}{\partial x}+\frac{1}{\rho}\frac{\partial p}{\partial x}\right)\\
        //      \frac{\partial p}{\partial t}=-\left(\gamma p\frac{\partial u}{\partial x}+u\frac{\partial p}{\partial x}\right)\\
        //  \end{align*}
        //$$
        var part_t = new (double rho, double u, double p)[PointsCount];
        part_t[0] = (
            (Ps[0].Density * (Ps[1].Velocity - Ps[0].Velocity) / Delta_x
                + Ps[0].Velocity * (Ps[1].Density - Ps[0].Density) / Delta_x),
            (Ps[0].Velocity * (Ps[1].Velocity - Ps[0].Velocity) / Delta_x
                + (1 / Ps[0].Density) * (Ps[1].Pressure - Ps[0].Pressure) / Delta_x),
            (Physics.gamma * Ps[0].Pressure * (Ps[1].Velocity - Ps[0].Velocity) / Delta_x
                + Ps[0].Velocity * (Ps[1].Pressure - Ps[0].Pressure) / Delta_x)
        );
        for(int j = 0 + 1; j < Ps.Length - 1; j++)
        {
            part_t[j] = (
                (Ps[j].Density * (Ps[j + 1].Velocity - Ps[j - 1].Velocity) / Delta_x / 2
                    + Ps[j].Velocity * (Ps[j + 1].Density - Ps[j - 1].Density) / Delta_x / 2),
                (Ps[j].Velocity * (Ps[j + 1].Velocity - Ps[j - 1].Velocity) / Delta_x / 2
                    + (1 / Ps[j].Density) * (Ps[j + 1].Pressure - Ps[j - 1].Pressure) / Delta_x / 2),
                (Physics.gamma * Ps[j].Pressure * (Ps[j + 1].Velocity - Ps[j - 1].Velocity) / Delta_x / 2
                    + Ps[j].Velocity * (Ps[j + 1].Pressure - Ps[j - 1].Pressure) / Delta_x / 2)
            );
        }
        part_t[^1] = (
            (Ps[^1].Density * (Ps[^1].Velocity - Ps[^2].Velocity) / Delta_x
                + Ps[^1].Velocity * (Ps[^1].Density - Ps[^2].Density) / Delta_x),
            (Ps[^1].Velocity * (Ps[^1].Velocity - Ps[^2].Velocity) / Delta_x
                + (1 / Ps[^1].Density) * (Ps[^1].Pressure - Ps[^2].Pressure) / Delta_x),
            (Physics.gamma * Ps[^1].Pressure * (Ps[^1].Velocity - Ps[^2].Velocity) / Delta_x
                + Ps[^1].Velocity * (Ps[^1].Pressure - Ps[^2].Pressure) / Delta_x)
        );
        var next = new Physics[PointsCount];
        for(int j = 0; j < PointsCount; j++)
        {
            next[j].Density = Math.Max(0, Ps[j].Density - part_t[j].rho * Delta_t);
            next[j].Velocity = Ps[j].Velocity - part_t[j].u * Delta_t;
            next[j].Pressure = Math.Max(0, Ps[j].Pressure - part_t[j].p * Delta_t);
        }
        points = next;
    }
}