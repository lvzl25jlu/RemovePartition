using System;
using System.Collections.Generic;
using System.Text;

namespace RemovePartition;

delegate FluidVaribles[] TimeAdvancer(double dt, FluidVaribles[] flus,
        Func<FluidVaribles[], Vec3<double>[]> residual);

static class RungeKutta
{
    /// <summary>
    /// 显式欧拉法，亦即一阶龙格库塔
    /// </summary>
    /// <param name="dt">Δt</param>
    /// <param name="flus">当前时刻流场</param>
    /// <param name="residual">残差计算函数</param>
    /// <returns>下一时刻流场</returns>
    public static FluidVaribles[] Eular(double dt, FluidVaribles[] flus,
        Func<FluidVaribles[], Vec3<double>[]> residual)
    {
        var R = residual(flus);
        return [..flus.Select((flu,i) => new FluidVaribles
        {
            U = flu.U + R[i] * dt
        })];
    }

    /// <summary>
    /// 三阶龙格库塔法
    /// </summary>
    /// <param name="dt">Δt</param>
    /// <param name="flus">当前时刻流场</param>
    /// <param name="residual">残差计算函数</param>
    /// <returns>下一时刻流场</returns>
    public static FluidVaribles[] RK3(double dt, FluidVaribles[] flus,
        Func<FluidVaribles[], Vec3<double>[]> residual)
    {
        //tex:对方程 $\frac{\partial\mathbf{U}}{\partial t}=\mathbf{R}\left(\mathbf{U}\right)$
        //三阶龙格库塔的公式为：
        //$$
        //\begin{aligned}
        //\mathbf{U}^{(1)}&=\mathbf{U}^{n}+\Delta t\mathbf{R}\left(\mathbf{U}^{n}\right)\\
        //\mathbf{U}^{(2)}&=\frac{3}{4}\mathbf{U}^{n}+\frac{1}{4}\mathbf{U}^{(1)}+\frac{1}{4}\Delta t\mathbf{R}\left(\mathbf{U}^{(1)}\right)\\
        //\mathbf{U}^{n+1}&=\frac{1}{3}\mathbf{U}^{n}+\frac{2}{3}\mathbf{U}^{(2)}+\frac{2}{3}\Delta t\mathbf{R}\left(\mathbf{U}^{(2)}\right)
        //\end{aligned}
        //$$
        var RU = residual(flus);
        var U1 = flus.Select((fi, i) => new FluidVaribles
        {
            U = fi.U + RU[i] * dt
        }).ToArray();
        var RU1 = residual(U1);
        var U2 = flus.Select((fi, i) => new FluidVaribles
        {
            U = fi.U * 0.75 + U1[i].U * 0.25 + RU1[i] * (dt * 0.25)
        }).ToArray();
        var RU2 = residual(U2);
        return [.. flus.Select((fi, i) => new FluidVaribles
        {
            U = fi.U * (1.0 / 3.0) + U2[i].U * (2.0 / 3.0) + RU2[i] * (dt * (2.0 / 3.0))
        })];
    }
}

