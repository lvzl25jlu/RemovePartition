using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Numerics;

namespace RemovePartition;

using FluxVaribles = Vec3<double>;
delegate FluxVaribles FluxCalculator(FluidVaribles le, FluidVaribles ri);

static partial class FluxCalculators
{
    /// <summary>
    /// 计算 Lax-Friedrichs 格式的数值通量
    /// </summary>
    /// <param name="le"></param>
    /// <param name="ri"></param>
    /// <returns> LF 数值通量</returns>
    /// <remarks>实现参考：https://zhuanlan.zhihu.com/p/584123678</remarks>
    public static FluxVaribles LaxFriedrichs(FluidVaribles le, FluidVaribles ri)
    {
        //tex:$\alpha=\max\left\lbrace\left|u\right|+c\right\rbrace$
        var alpha = Math.Max(
            Math.Abs(le.Velocity) + le.SoundSpeed,
            Math.Abs(ri.Velocity) + ri.SoundSpeed
        );
        //tex:
        //$$
        //   \mathbf{F}_{i+\frac 1 2} = \frac 1 2 \left(\mathbf{F}_L + \mathbf{F}_R
        //      -\alpha\left(\mathbf{U}_R-\mathbf{U}_L\right)\right)
        //$$
        return 0.5 * (le.F + ri.F - alpha * (ri.U - le.U));
    }

    /// <summary>
    /// 计算 HLL 格式的数值通量
    /// </summary>
    /// <param name="le"></param>
    /// <param name="ri"></param>
    /// <returns> HLL 数值通量</returns>
    /// <remarks>实现参考：https://zhuanlan.zhihu.com/p/584123678</remarks>
    public static FluxVaribles HLL(FluidVaribles le, FluidVaribles ri)
    {
        //tex: $S_L=\min\left\lbrace u_L - c_L, u_R - c_R\right\rbrace$
        var sL = Math.Min(
            le.Velocity - le.SoundSpeed,
            ri.Velocity - ri.SoundSpeed
        );
        //tex: $S_R=\max\left\lbrace u_L + c_L, u_R + c_R\right\rbrace$
        var sR = Math.Max(
            le.Velocity + le.SoundSpeed,
            ri.Velocity + ri.SoundSpeed
        );
        //tex:
        //$$
        //   \mathbf{H} = \frac{S_R \mathbf{F}_L - S_L \mathbf{F}_R
        //      + S_L S_R \left(\mathbf{U}_R-\mathbf{U}_L\right)}{S_R - S_L}
        //$$
        var h = (sR * le.F - sL * ri.F + sL * sR * (ri.U - le.U)) / (sR - sL);
        //tex:
        //$$
        //  \mathbf{F}_{i+\frac 1 2} =\begin{cases}
        //      \mathbf{F}_L, & 0 \le S_L \\
        //      \mathbf{H}, & S_L < 0 < S_R \\
        //      \mathbf{F}_R, & S_R \le 0
        //  \end{cases}
        //$$
        return (sL, sR) switch
        {
            ( >= 0, _) => le.F,
            ( < 0, > 0) => h,
            ( <= 0, _) => ri.F,
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// 计算 HLLC 格式的数值通量
    /// </summary>
    /// <param name="le"></param>
    /// <param name="ri"></param>
    /// <returns> HLLC 数值通量</returns>
    /// <remarks>实现参考：https://zhuanlan.zhihu.com/p/584123678</remarks>
    /// 
    public static FluxVaribles HLLC(FluidVaribles le, FluidVaribles ri)
    {
        //tex: $S_L=\min\left\lbrace u_L - c_L, u_R - c_R\right\rbrace$
        var sL = Math.Min(
            le.Velocity - le.SoundSpeed,
            ri.Velocity - ri.SoundSpeed
        );
        //tex: $S_R=\max\left\lbrace u_L + c_L, u_R + c_R\right\rbrace$
        var sR = Math.Max(
            le.Velocity + le.SoundSpeed,
            ri.Velocity + ri.SoundSpeed
        );
        //tex: $S_\ast=\frac{p_R - p_L + \rho_L u_L (S_L - u_L) - \rho_R u_R (S_R - u_R)}{\rho_L (S_L - u_L) - \rho_R (S_R - u_R)}$
        var sAst = (ri.Pressure - le.Pressure +
            le.Density * le.Velocity * (sL - le.Velocity) -
            ri.Density * ri.Velocity * (sR - ri.Velocity))
            / (le.Density * (sL - le.Velocity) - ri.Density * (sR - ri.Velocity));
        //tex:$\mathbf{D}_\ast=\begin{pmatrix}0&1&S_\ast\end{pmatrix}^T$
        var dAst = new Vec3<double>(0, 1, sAst);
        //tex:
        //$$ \mathbf{F}_{\ast,K}=
        //      \frac{S_\ast\left(S_{K} \mathbf{U}_{K} - \mathbf{F}_{K}\right)
        //          +S_{K} \left(p_{K} + \rho_{K} (S_{K} - u_{K})
        //          (S_\ast - u_{K})\right) \mathbf{D}_\ast}
        //      {S_{K}-S_\ast}
        //$$ 其中 $K=L,R$
        var fAstL = (sAst * (sL * le.U - le.F)
            + sL * (le.Pressure + le.Density * (sL - le.Velocity) * (sAst - le.Velocity)) * dAst)
            / (sL - sAst);
        var fAstR = (sAst * (sR * ri.U - ri.F)
            + sR * (ri.Pressure + ri.Density * (sR - ri.Velocity) * (sAst - ri.Velocity)) * dAst)
            / (sR - sAst);
        //tex:
        //$$
        //  \mathbf{F}_{i+\frac 1 2} =\begin{cases}
        //      \mathbf{F}_L & 0 \le S_L \\
        //      \mathbf{F}_{\ast,L} & S_L < 0 \le S_\ast \\
        //      \mathbf{F}_{\ast,R}  & S_\ast < 0 \le S_R \\
        //      \mathbf{F}_R & S_R \le 0
        //  \end{cases}
        //$$
        return (sL, sAst, sR) switch
        {
            ( >= 0, _, _) => le.F,
            ( < 0, >= 0, _) => fAstL,
            (_, < 0, >= 0) => fAstR,
            (_, _, <= 0) => ri.F,
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// 计算Roe格式的数值通量
    /// </summary>
    /// <param name="le">左侧单元的物理量</param>
    /// <param name="ri">右侧单元的物理量</param>
    /// <returns> Roe 数值通量</returns>
    /// <remarks>
    /// 实现参考：https://github.com/rustamNSU/GodunovsMethod/blob/master/src/RiemannSolvers.cpp
    /// </remarks>
    public static FluxVaribles Roe(FluidVaribles le, FluidVaribles ri)
    {
        //tex: 比热容比$\gamma$
        double gamma = FluidVaribles.SpecHeatRatio;

        //tex: $\sqrt{\rho_L}$ 
        double sqrtRhoL = Math.Sqrt(le.Density);
        //tex: $\sqrt{\rho_R}$
        double sqrtRhoR = Math.Sqrt(ri.Density);

        //tex:速度的Roe平均 $\bar{u}=\frac{\sqrt{\rho_L}u_L+\sqrt{\rho_R}*u_R}{\sqrt{\rho_L}+\sqrt{\rho_R}}$
        double aveVelocity = (sqrtRhoL * le.Velocity + sqrtRhoR * ri.Velocity)
            / (sqrtRhoL + sqrtRhoR);
        //tex:焓的Roe平均 $\bar{h} = \frac{\sqrt{\rho_L} h_L + \sqrt{\rho_R} h_R}{\sqrt{\rho_L} + \sqrt{\rho_R}}$
        double aveEnthalpy = (sqrtRhoL * le.Enthalpy + sqrtRhoR * ri.Enthalpy)
            / (sqrtRhoL + sqrtRhoR);
        //tex:平均声速 $\bar{c}=\sqrt{\left(\gamma-1\right)\left(\bar{h}-\frac{\bar{u}^2}2\right)}$
        double aveSoundSpeed = Math.Sqrt(
            (gamma - 1) *
            (aveEnthalpy - aveVelocity.Square / 2)
        );
        //tex:$\bar{\rho}=\sqrt{\rho_L\rho_R}$
        double aveDensity = sqrtRhoL * sqrtRhoR;

        //tex: $\Delta \rho = \rho_R - \rho_L$
        double deltaDensity = ri.Density - le.Density;
        //tex: $\Delta p = p_R - p_L$
        double deltaPressure = ri.Pressure - le.Pressure;
        //tex: $\Delta u = u_R - u_L$
        double deltaVelocity = ri.Velocity - le.Velocity;
        //tex: 辅助（auxiliary）变量$\frac 1{\bar{c}^2}$
        double aux = 1 / aveSoundSpeed.Square;

        //tex:波强系数
        //$
        //  \mathbf\alpha=\begin{pmatrix}
        //      \frac{1}{2} \frac 1{\bar{c}^2}\left(\Delta p - \bar{c} \bar{\rho} \Delta u\right)\\
        //      \left(\rho_R - \rho_L\right) -  \frac 1{\bar{c}^2}\Delta p\\
        //      \frac{1}{2} \frac 1{\bar{c}^2} \left(\Delta p + \bar{c} \bar{\rho} \Delta u\right)\\
        //  \end{pmatrix}
        //$
        Vec3<double> alpha = (
            0.5 * aux * (deltaPressure - aveSoundSpeed * aveDensity * deltaVelocity),
            deltaDensity - deltaPressure * aux,
            0.5 * aux * (deltaPressure + aveSoundSpeed * aveDensity * deltaVelocity)
        );

        //tex: 特征值
        //$
        //  \mathbf\lambda=\begin{pmatrix}
        //      \bar{u} - \bar{c}\\
        //      \bar{u} \\
        //      \bar{u} + \bar{c}\\
        //  \end{pmatrix}
        //$
        Vec3<double> eigen = (
            aveVelocity - aveSoundSpeed,
            aveVelocity,
            aveVelocity + aveSoundSpeed
        );

        //tex: 右特征向量矩阵
        //$
        //  \mathbf{r} = \begin{pmatrix}
        //       \begin{pmatrix} 1 & \bar{u} - \bar{c} & \bar{h} - \bar{u} \bar{c} \end{pmatrix}^T\\
        //       \begin{pmatrix} 1 & \bar{u} & \frac{\bar{u}^2}{2} \end{pmatrix}^T\\
        //       \begin{pmatrix} 1 & \bar{u} + \bar{c} & \bar{h} + \bar{u} \bar{c} \end{pmatrix}^T\\
        //   \end{pmatrix}^T
        //$
        Vec3<Vec3<double>> r = (
            (1.0, aveVelocity - aveSoundSpeed, aveEnthalpy - aveVelocity * aveSoundSpeed),
            (1.0, aveVelocity, 0.5 * aveVelocity.Square),
            (1.0, aveVelocity + aveSoundSpeed, aveEnthalpy + aveVelocity * aveSoundSpeed)
        );

        //tex:Roe格式通量
        //$$
        //  \mathbf{F}_{\text{Roe}} = \frac 12 \left( \mathbf{F}\left(\mathbf{U}_L\right) + \mathbf{F}\left(\mathbf{U}_R\right) - \sum_{k=1}^{3} \left|\lambda_k\right| \alpha_k \mathbf{r}_k \right)
        //$$
        //  其中 
        //$$
        //  \mathbf{F}\left(\mathbf{U}_L\right) + \mathbf{F}\left(\mathbf{U}_R\right)
        //  = \bar{\mathbf{A}} \mathbf{U}_L + \bar{\mathbf{A}} \mathbf{U}_R 
        //  = \bar{\mathbf{A}} \left(\mathbf{U}_L + \mathbf{U}_R \right)
        //$$
        //  其中
        //$$
        //  \bar{\mathbf{A}}=\begin{pmatrix}
        //      0& 1 & 0 \\
        //      \frac{\gamma-3}2 \bar{u}^2 & (3-\gamma)\bar{u} & \gamma-1 \\
        //      \left(\frac{\gamma-1}2 \bar{u}^3 - \bar{u}\bar{h}\right) & \left(\bar{h} - (\gamma-1)\bar{u}^2\right) & \gamma \bar{u}
        //  \end{pmatrix}
        //$$

        //tex:$\mathbf{U}_L + \mathbf{U}_R$
        var sumU = le.U + ri.U;
        //tex:先算$\bar{\mathbf{A}}\left(\mathbf{U}_L + \mathbf{U}_R\right)$
        FluxVaribles flux = (
            sumU[2],
            (gamma - 3) * aveVelocity.Square / 2 * sumU[1]
                + (3 - gamma) * aveVelocity * sumU[2]
                + (gamma - 1) * sumU[3],
            ((gamma - 1) * Math.Pow(aveVelocity, 3) / 2 - aveVelocity * aveEnthalpy) * sumU[1]
                + (aveEnthalpy - (gamma - 1) * aveVelocity.Square) * sumU[2]
                + gamma * aveVelocity * sumU[3]
        );
        //tex: 减去特征分解项$\sum\limits_{k=1}^{3} \left|\lambda_k\right| \alpha_k \mathbf{r}_k$
        for(int i = 1; i <= 3; i++)
        {
            flux -= Math.Abs(eigen[i]) * alpha[i] * r[i];
        }
        //tex: 最后乘以$\frac 12$
        return 0.5 * flux;
    }
}
