using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;

partial class FluentGas
{
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
        var next = new FluidVaribles[PointsCount];
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
            (FluidVaribles.SpecHeatRatio * Ps[0].Pressure * (Ps[1].Velocity - Ps[0].Velocity) / Delta_x
                + Ps[0].Velocity * (Ps[1].Pressure - Ps[0].Pressure) / Delta_x)
        );
        for(int j = 0 + 1; j < Ps.Length - 1; j++)
        {
            part_t[j] = (
                (Ps[j].Density * (Ps[j + 1].Velocity - Ps[j - 1].Velocity) / Delta_x / 2
                    + Ps[j].Velocity * (Ps[j + 1].Density - Ps[j - 1].Density) / Delta_x / 2),
                (Ps[j].Velocity * (Ps[j + 1].Velocity - Ps[j - 1].Velocity) / Delta_x / 2
                    + (1 / Ps[j].Density) * (Ps[j + 1].Pressure - Ps[j - 1].Pressure) / Delta_x / 2),
                (FluidVaribles.SpecHeatRatio * Ps[j].Pressure * (Ps[j + 1].Velocity - Ps[j - 1].Velocity) / Delta_x / 2
                    + Ps[j].Velocity * (Ps[j + 1].Pressure - Ps[j - 1].Pressure) / Delta_x / 2)
            );
        }
        part_t[^1] = (
            (Ps[^1].Density * (Ps[^1].Velocity - Ps[^2].Velocity) / Delta_x
                + Ps[^1].Velocity * (Ps[^1].Density - Ps[^2].Density) / Delta_x),
            (Ps[^1].Velocity * (Ps[^1].Velocity - Ps[^2].Velocity) / Delta_x
                + (1 / Ps[^1].Density) * (Ps[^1].Pressure - Ps[^2].Pressure) / Delta_x),
            (FluidVaribles.SpecHeatRatio * Ps[^1].Pressure * (Ps[^1].Velocity - Ps[^2].Velocity) / Delta_x
                + Ps[^1].Velocity * (Ps[^1].Pressure - Ps[^2].Pressure) / Delta_x)
        );
        var next = new FluidVaribles[PointsCount];
        for(int j = 0; j < PointsCount; j++)
        {
            next[j].Density = Math.Max(0, Ps[j].Density - part_t[j].rho * Delta_t);
            next[j].Velocity = Ps[j].Velocity - part_t[j].u * Delta_t;
            next[j].Pressure = Math.Max(0, Ps[j].Pressure - part_t[j].p * Delta_t);
        }
        points = next;
    }
}

