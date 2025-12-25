using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;
static class TheRoeFluxCalculator
{
    /// <summary>
    /// 计算Roe格式的数值通量
    /// </summary>
    /// <param name="lFV">左侧单元的物理量</param>
    /// <param name="rFV">右侧单元的物理量</param>
    /// <returns>Roe数值通量</returns>
    /// <remarks>
    /// 实现参考：https://github.com/rustamNSU/GodunovsMethod/blob/master/src/RiemannSolvers.cpp
    /// </remarks>
    public static FluxVaribles RoeFluxCalculator(FluidVaribles lFV, FluidVaribles rFV)
    {
        //tex: 比热容比$\gamma$
        double gamma = FluidVaribles.SpecHeatRatio;

        //tex: $\sqrt{\rho_L}$ 
        double sqrtRhoL = Math.Sqrt(lFV.Density);
        //tex: $\sqrt{\rho_R}$
        double sqrtRhoR = Math.Sqrt(rFV.Density);

        //tex:速度的Roe平均 $\bar{u}=\frac{\sqrt{\rho_L}u_L+\sqrt{\rho_R}*u_R}{\sqrt{\rho_L}+\sqrt{\rho_R}}$
        double aveVelocity = (sqrtRhoL * lFV.Velocity + sqrtRhoR * rFV.Velocity)
            / (sqrtRhoL + sqrtRhoR);
        //tex:焓的Roe平均 $\bar{h} = \frac{\sqrt{\rho_L} h_L + \sqrt{\rho_R} h_R}{\sqrt{\rho_L} + \sqrt{\rho_R}}$
        double aveEnthalpy = (sqrtRhoL * lFV.Enthalpy + sqrtRhoR * rFV.Enthalpy)
            / (sqrtRhoL + sqrtRhoR);
        //tex:平均声速 $\bar{c}=\sqrt{\left(\gamma-1\right)\left(\bar{h}-\frac{\bar{u}^2}2\right)}$
        double aveSoundSpeed = Math.Sqrt(
            (gamma - 1) *
            (aveEnthalpy - aveVelocity.Square() / 2)
        );
        //tex:$\bar{\rho}=\sqrt{\rho_L\rho_R}$
        double aveDensity = sqrtRhoL * sqrtRhoR;

        //tex: $\Delta \rho = \rho_R - \rho_L$
        double deltaDensity = rFV.Density - lFV.Density;
        //tex: $\Delta p = p_R - p_L$
        double deltaPressure = rFV.Pressure - lFV.Pressure;
        //tex: $\Delta u = u_R - u_L$
        double deltaVelocity = rFV.Velocity - lFV.Velocity;
        //tex: 辅助（auxiliary）变量$\frac 1{\bar{c}^2}$
        double aux = 1 / aveSoundSpeed.Square();

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
            (1.0, aveVelocity, 0.5 * aveVelocity.Square()),
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
        Vec3<double> sumU = (Vec3<double>)lFV.U + (Vec3<double>)rFV.U;
        //tex:先算$\bar{\mathbf{A}}\left(\mathbf{U}_L + \mathbf{U}_R\right)$
        Vec3<double> flux = (
            sumU[2],
            (gamma - 3) * aveVelocity.Square() / 2 * sumU[1]
                + (3 - gamma) * aveVelocity * sumU[2]
                + (gamma - 1) * sumU[3],
            ((gamma - 1) * Math.Pow(aveVelocity, 3) / 2 - aveVelocity * aveEnthalpy) * sumU[1]
                + (aveEnthalpy - (gamma - 1) * aveVelocity.Square()) * sumU[2]
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