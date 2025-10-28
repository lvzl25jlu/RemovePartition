using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;

//tex:用 $\rho$ 、 $u$ 、 $p$ 作为原始变量
public struct Primitive
{
    public static double gamma { get; set; } = 1.3f;
    public double Density { get; set; }
    public double Pressure { get; set; }
    public double Velocity { get; set; }
    public double U_rho
    {
        //tex: $U_\rho=\rho$
        readonly get => Density;
        //tex: $\rho=U_\rho$
        set => Density = value < 0 ? 0 : value;
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
    public readonly double F_rho => Density*Velocity;
    //tex: $F_p=\rho u^2+p$
    public readonly double F_p => Density * Velocity.Square() + Pressure;
    //tex: $F_E=u\left(\rho E+p\right)$ where $\rho E=U_E$
    public readonly double F_E => Velocity * (U_E + Pressure);
}



public class FluentGas : IGas
{
    const int DEFAULT_POINTS_COUNT = 100;
    int pointsCount = DEFAULT_POINTS_COUNT;
    public int PointsCount
    {
        get => pointsCount; set
        {
            primitives = [.. Enumerable.Repeat(new Primitive(), value)];
            pointsCount = value;
        }
    }
    public double Delta_x { get; set; } = 0.01;
    public double Delta_t { get; set; } = 0.001;
    public FluentGas(IGas that)
    {
        PointsCount = that.PointsCount;
        //手动缓存优化
        var (densitys, pressures, velocitys) = (that.Densitys, that.Pressures, that.Velocitys);
        for(int i = 0; i < pointsCount; i++)
        {
            Pr[i].Density = densitys[i];
            Pr[i].Pressure = pressures[i];
            Pr[i].Velocity = velocitys[i];
        }
    }

    public double[] Densitys => [.. Pr.Select(p => p.Density)];
    public double[] Pressures => [.. Pr.Select(p => p.Pressure)];
    public double[] Velocity => [.. Pr.Select(p => p.Velocity)];

    //tex:
    //  根据欧拉方程
    // $$
    //  \frac{\partial U}{\partial t}+\frac{\partial F}{\partial x}=0
    // $$
    //  在一维时
    // $$
    //  U=\begin{pmatrix}\rho\\\rho u\\\rho E\end{pmatrix},
    //  F=\begin{pmatrix}\rho\\\rho u^2+p\\u\left(\rho E+p\right)\end{pmatrix}                                         
    // $$
    //  其中 $E=e+\frac 12u^2$ ，$e=\frac{RT}{\gamma-1}$

    Primitive[] primitives = [.. Enumerable.Repeat(new Primitive(), DEFAULT_POINTS_COUNT)];
    Primitive[] Pr => primitives; // 起个短点的名字

    //tex: 一阶欧拉向前差分
    // $$
    //     \frac{U^{\left(n+1\right)}-U^{\left(n\right)}}{\Delta t}
    //          +\frac {\partial F}{\partial x}=0
    // $$
    // 空间选择中心差分
    // $$
    //  \frac {\partial F}{\partial x}=\frac{F_{j+1}-F_{j-1}}{\Delta x}
    // $$
    // 对左端点（$j=0$）采用向前差分 <br>
    // 对右端点（$j=\text{-1}$）采用向后差分
    public void ForwardEular()
    {
        var part_x_F = new (double rho, double p, double E)[PointsCount];
        part_x_F[0] = (
            (-3 * Pr[0].F_rho + 4 * Pr[1].F_rho - Pr[2].F_rho) / (2 * Delta_x),
            (-3 * Pr[0].F_p + 4 * Pr[1].F_p - Pr[2].F_p) / (2 * Delta_x),
            (-3 * Pr[0].F_E + 4 * Pr[1].F_E - Pr[2].F_E) / (2 * Delta_x)
        );
        for(int j = 0 + 1; j < pointsCount - 1; j++)
        {
            part_x_F[j] = (
                (Pr[j + 1].F_rho - Pr[j - 1].F_rho) / Delta_x / 2,
                (Pr[j + 1].F_p - Pr[j - 1].F_p) / Delta_x / 2,
                (Pr[j + 1].F_E - Pr[j - 1].F_E) / Delta_x / 2
            );
        }
        part_x_F[^1] = (
            (3 * Pr[^1].F_rho - 4 * Pr[^2].F_rho + Pr[^3].F_rho) / (2 * Delta_x),
            (3 * Pr[^1].F_p - 4 * Pr[^2].F_p + Pr[^3].F_p) / (2 * Delta_x),
            (3 * Pr[^1].F_E - 4 * Pr[^2].F_E + Pr[^3].F_E) / (2 * Delta_x)
        );
        //tex: $$U^{\left(n+1\right)}=U^{\left(n\right)}
        //      -\frac {\partial F}{\partial x}\Delta t$$
        var next = new Primitive[PointsCount];
        for(int j = 0; j < pointsCount; j++)
        {
            next[j].U_rho = Pr[j].U_rho - part_x_F[j].rho * Delta_t;
            next[j].U_p = Pr[j].U_p - part_x_F[j].p * Delta_t;
            next[j].U_E = Pr[j].U_E - part_x_F[j].E * Delta_t;
        }
        primitives = next;
    }

    /// <summary>
    /// Lax-Wendroff格式求解器
    /// 基于泰勒展开和原PDE推导的二阶精度格式
    /// 公式: U^(n+1) = U^n - Δt * ∂F/∂x + (Δt²/2) * ∂/∂x(A * ∂F/∂x)
    /// 其中 A = ∂F/∂U 是通量雅可比矩阵
    /// </summary>
    public void LaxWendroff()
    {
        //tex: 创建临时数组存储中间计算结果
        var fluxDerivative = new (double rho, double p, double E)[PointsCount];
        var secondOrderTerm = new (double rho, double p, double E)[PointsCount];

        //tex: === 第一步：计算一阶导数项 ∂F/∂x ===
        //tex: 使用中心差分计算通量F的空间导数

        //tex: 左边界：二阶前向差分
        fluxDerivative[0] = (
            (-3 * Pr[0].F_rho + 4 * Pr[1].F_rho - Pr[2].F_rho) / (2 * Delta_x),
            (-3 * Pr[0].F_p + 4 * Pr[1].F_p - Pr[2].F_p) / (2 * Delta_x),
            (-3 * Pr[0].F_E + 4 * Pr[1].F_E - Pr[2].F_E) / (2 * Delta_x)
        );

        //tex: 内部点：二阶中心差分
        for(int j = 1; j < PointsCount - 1; j++)
        {
            fluxDerivative[j] = (
                (Pr[j + 1].F_rho - Pr[j - 1].F_rho) / (2 * Delta_x),
                (Pr[j + 1].F_p - Pr[j - 1].F_p) / (2 * Delta_x),
                (Pr[j + 1].F_E - Pr[j - 1].F_E) / (2 * Delta_x)
            );
        }

        //tex: 右边界：二阶后向差分
        fluxDerivative[^1] = (
            (3 * Pr[^1].F_rho - 4 * Pr[^2].F_rho + Pr[^3].F_rho) / (2 * Delta_x),
            (3 * Pr[^1].F_p - 4 * Pr[^2].F_p + Pr[^3].F_p) / (2 * Delta_x),
            (3 * Pr[^1].F_E - 4 * Pr[^2].F_E + Pr[^3].F_E) / (2 * Delta_x)
        );

        //tex: === 第二步：计算二阶导数项 ∂/∂x(A * ∂F/∂x) ===
        //tex: 这里简化处理：用中心差分近似二阶导数

        //tex: 计算 A * ∂F/∂x 的近似值
        //tex: 对于欧拉方程，A ≈ u ± c (特征速度)，这里用当地声速近似
        var A_times_fluxDeriv = new (double rho, double p, double E)[PointsCount];
        for(int j = 0; j < PointsCount; j++)
        {
            double soundSpeed = Math.Sqrt(Primitive.gamma * Pr[j].Pressure / Pr[j].Density);
            double waveSpeed = Math.Abs(Pr[j].Velocity) + soundSpeed;

            //tex: 近似计算 A * ∂F/∂x
            A_times_fluxDeriv[j] = (
                waveSpeed * fluxDerivative[j].rho,
                waveSpeed * fluxDerivative[j].p,
                waveSpeed * fluxDerivative[j].E
            );
        }

        //tex: 计算 ∂/∂x(A * ∂F/∂x) 使用中心差分
        //tex: 左边界
        secondOrderTerm[0] = (
            (-3 * A_times_fluxDeriv[0].rho + 4 * A_times_fluxDeriv[1].rho - A_times_fluxDeriv[2].rho) / (2 * Delta_x),
            (-3 * A_times_fluxDeriv[0].p + 4 * A_times_fluxDeriv[1].p - A_times_fluxDeriv[2].p) / (2 * Delta_x),
            (-3 * A_times_fluxDeriv[0].E + 4 * A_times_fluxDeriv[1].E - A_times_fluxDeriv[2].E) / (2 * Delta_x)
        );

        //tex: 内部点
        for(int j = 1; j < PointsCount - 1; j++)
        {
            secondOrderTerm[j] = (
                (A_times_fluxDeriv[j + 1].rho - A_times_fluxDeriv[j - 1].rho) / (2 * Delta_x),
                (A_times_fluxDeriv[j + 1].p - A_times_fluxDeriv[j - 1].p) / (2 * Delta_x),
                (A_times_fluxDeriv[j + 1].E - A_times_fluxDeriv[j - 1].E) / (2 * Delta_x)
            );
        }

        //tex: 右边界
        secondOrderTerm[^1] = (
            (3 * A_times_fluxDeriv[^1].rho - 4 * A_times_fluxDeriv[^2].rho + A_times_fluxDeriv[^3].rho) / (2 * Delta_x),
            (3 * A_times_fluxDeriv[^1].p - 4 * A_times_fluxDeriv[^2].p + A_times_fluxDeriv[^3].p) / (2 * Delta_x),
            (3 * A_times_fluxDeriv[^1].E - 4 * A_times_fluxDeriv[^2].E + A_times_fluxDeriv[^3].E) / (2 * Delta_x)
        );

        //tex: === 第三步：组合Lax-Wendroff更新公式 ===
        //tex: U^(n+1) = U^n - Δt * ∂F/∂x + (Δt²/2) * ∂/∂x(A * ∂F/∂x)
        var next = new Primitive[PointsCount];
        for(int j = 0; j < PointsCount; j++)
        {
            //tex: 一阶项：-Δt * ∂F/∂x
            double rho_first_order = -Delta_t * fluxDerivative[j].rho;
            double p_first_order = -Delta_t * fluxDerivative[j].p;
            double E_first_order = -Delta_t * fluxDerivative[j].E;

            //tex: 二阶项：(Δt²/2) * ∂/∂x(A * ∂F/∂x)
            double rho_second_order = (Delta_t * Delta_t * 0.5f) * secondOrderTerm[j].rho;
            double p_second_order = (Delta_t * Delta_t * 0.5f) * secondOrderTerm[j].p;
            double E_second_order = (Delta_t * Delta_t * 0.5f) * secondOrderTerm[j].E;

            //tex: 组合更新
            next[j].U_rho = Pr[j].U_rho + rho_first_order + rho_second_order;
            next[j].U_p = Pr[j].U_p + p_first_order + p_second_order;
            next[j].U_E = Pr[j].U_E + E_first_order + E_second_order;

            //tex: 物理约束检查
            if(next[j].Density <= 0)
                next[j].Density = 1e-6f;
            if(next[j].Pressure <= 0)
                next[j].Pressure = 1e-6f;
            if(double.IsNaN(next[j].Velocity))
                next[j].Velocity = 0f;
        }

        primitives = next;
    }

    /// <summary>
    /// 简化的Lax-Wendroff格式（两步法实现）
    /// 这种方法更接近原始论文的实现，数值上更稳定
    /// </summary>
    public void LaxWendroffTwoStep()
    {
        //tex: 第一步：计算半时间步的中间值
        var halfTimeStep = new Primitive[PointsCount];

        //tex: 使用Lax-Friedrichs格式计算半时间步
        for(int j = 1; j < PointsCount - 1; j++)
        {
            //tex: 平均值
            double avg_rho = 0.5f * (Pr[j - 1].U_rho + Pr[j + 1].U_rho);
            double avg_p = 0.5f * (Pr[j - 1].U_p + Pr[j + 1].U_p);
            double avg_E = 0.5f * (Pr[j - 1].U_E + Pr[j + 1].U_E);

            //tex: 通量差
            double flux_rho = (Pr[j + 1].F_rho - Pr[j - 1].F_rho) / (2 * Delta_x);
            double flux_p = (Pr[j + 1].F_p - Pr[j - 1].F_p) / (2 * Delta_x);
            double flux_E = (Pr[j + 1].F_E - Pr[j - 1].F_E) / (2 * Delta_x);

            //tex: 半时间步更新
            halfTimeStep[j].U_rho = avg_rho - (Delta_t * 0.5f) * flux_rho;
            halfTimeStep[j].U_p = avg_p - (Delta_t * 0.5f) * flux_p;
            halfTimeStep[j].U_E = avg_E - (Delta_t * 0.5f) * flux_E;
        }

        //tex: 边界条件（零梯度）
        halfTimeStep[0] = halfTimeStep[1];
        halfTimeStep[^1] = halfTimeStep[^2];

        //tex: 第二步：使用中间值计算全时间步
        var next = new Primitive[PointsCount];

        for(int j = 1; j < PointsCount - 1; j++)
        {
            //tex: 使用中间值计算通量导数
            double flux_rho_deriv = (halfTimeStep[j + 1].F_rho - halfTimeStep[j - 1].F_rho) / (2 * Delta_x);
            double flux_p_deriv = (halfTimeStep[j + 1].F_p - halfTimeStep[j - 1].F_p) / (2 * Delta_x);
            double flux_E_deriv = (halfTimeStep[j + 1].F_E - halfTimeStep[j - 1].F_E) / (2 * Delta_x);

            //tex: 全时间步更新
            next[j].U_rho = Pr[j].U_rho - Delta_t * flux_rho_deriv;
            next[j].U_p = Pr[j].U_p - Delta_t * flux_p_deriv;
            next[j].U_E = Pr[j].U_E - Delta_t * flux_E_deriv;
        }

        //tex: 边界条件
        next[0] = next[1];
        next[^1] = next[^2];

        primitives = next;
    }
}