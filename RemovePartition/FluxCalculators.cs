using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RemovePartition;

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
            ( <= 0, _, <= 0) => ri.F,
            _ => throw new UnreachableException(),
        };
    }
}
