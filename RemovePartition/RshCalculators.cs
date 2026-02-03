using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace RemovePartition;

using FluxVaribles = Vec3<double>;

delegate TData[] RhsCalculator<TData>(FluidField<TData> ff)
    where TData : struct, IGridData<TData>;

static partial class RshCalculators
{
    /// <summary>
    /// Godunov格式计算的RHS
    /// </summary>
    /// <param name="ff">流场</param>
    /// <param name="fluxCal">通量计算器</param>
    /// <returns>网格变量对时间的导数</returns>
    public static FvmData[] Godunov(this FluxCalculator fluxCal, FluidField<FvmData> ff)
    {
        //tex: fluxs[j] 表示 $\hat{\mathbf{F}}_{j-\frac 12}$
        var fluxs = new FluxVaribles[ff.Length + 1];
        fluxs[0] = fluxCal(ff[0].FluVar with
        {
            Velocity = -ff[0].FluVar.Velocity
        }, ff[0]);
        for(int j = 1; j < ff.Length; j++)
        {
            fluxs[j] = fluxCal(ff[j - 1], ff[j]);
        }
        fluxs[^1] = fluxCal(ff[^1], ff[^1].FluVar with
        {
            Velocity = -ff[^1].FluVar.Velocity
        });
        //tex: $\mathbf{R}_j= \frac{1}{\Delta x_j}\left(\hat{\mathbf{F}}_{j-\frac 12} - \hat{\mathbf{F}}_{j+\frac 12}\right)$
        return [..ff.Select((grid, j) => new FvmData
        {
            U = (fluxs[j] - fluxs[j + 1]) / grid.W
        })];
    }
    /// <summary>
    /// 间断伽辽金格式计算的RHS
    /// </summary>
    /// <param name="fluxCal"></param>
    /// <param name="ff"></param>
    /// <returns>模态系数对时间的导数</returns>
    public static DgData[] DG(this FluxCalculator fluxCal, FluidField<DgData> ff)
    {
        //tex:
        //$$
        //  \mathbf{V}_j ^{(m)} = \frac{ 2m + 1}{\Delta x_j}\int_{ -1}^1
        //      \mathbf{ F}\left(\mathbf{ U}\left(\xi\right)\right)
        //      \frac{\mathrm d \phi ^{ (m)}\left(\xi\right)}{\mathrm d \xi}
        //  \mathrm d\xi
        //$$
        var V = ff.Select(grid => new DgData()).ToArray();
        for(int j = 0; j < ff.Length; j++)
        {
            var grid = ff[j];
            for(int m = 0; m <= DgData.Order; m++)
            {
                V[j][m] = (2 * m + 1) / grid.W * GuessIntegrate.Integrate2(xi =>
                    FluidVaribles.FromConservativeVars(grid.Uhxi(xi)).F *
                    LegendrePolynomials.Dphi[m](xi));
            }
        }

        //tex: fluxs[j] 表示 $\hat{\mathbf{F}}_{j-\frac 12}$ <br>
        //对DG方法而言，通量需要用边界点处的流体量来计算
        //$$
        //  \hat{\mathbf{F}}_{j+\frac12} = \hat{\mathbf{F}}\left(\hat {\mathbf{ U} }_{j+\frac12}^-,\hat {\mathbf{U}}_{j+\frac12}^+\right) 
        //$$
        //其中
        //$$
        //\begin{align*}
        //    \hat {\mathbf{U}}_{j+\frac12}^- &= \sum_m \mathbf{ U}_j^{(m)}\phi^{(m)}\left(+1\right)\\
        //    \hat {\mathbf{U}}_{j+\frac12}^+ &= \sum_m \mathbf{ U}_{j+1}^{(m)}\phi^{(m)}\left(-1\right)
        //\end{align*}
        //$$
        var fluxs = new FluxVaribles[ff.Length + 1];
        var UL = ff[0].FluVarAt(ff[0].Range.L);
        fluxs[0] = fluxCal(UL with
        {
            Velocity = -UL.Velocity
        }, UL);
        for(int j = 1; j < ff.Length; j++)
        {
            fluxs[j] = fluxCal(ff[j - 1].FluVarAt(ff[j - 1].Range.R),
                ff[j].FluVarAt(ff[j].Range.L));
        }
        var UR = ff[^1].FluVarAt(ff[^1].Range.R);
        fluxs[^1] = fluxCal(UR, UR with
        {
            Velocity = -UR.Velocity
        });

        //tex:
        //$$
        //  \mathbf{S}_j^{(m)}=-\frac{2m+1}{\Delta x_j}\left(
        //      \hat{\mathbf{F} }_{j+\frac12}\phi^{(m)}\left(1\right)
        //      - \hat{\mathbf{F} }_{j-\frac12}\phi^{(m)}\left(-1\right)
        //  \right)
        //$$
        var S = ff.Select(grid => new DgData()).ToArray();
        for(int j = 0; j < ff.Length; j++)
        {
            var grid = ff[j];
            for(int m = 0; m <= DgData.Order; m++)
            {
                S[j][m] = -(2 * m + 1) / grid.W * (fluxs[j + 1] * LegendrePolynomials.Phi[m](1)
                    - fluxs[j] * LegendrePolynomials.Phi[m](-1));
            }
        }

        //tex:
        //$$
        //  \frac{\mathrm{ d} } {\mathrm{ d} t}\mathbf{ U} _j ^{ (m)}
        //  =\mathbf{ R} _j ^{ (m)}
        //  =\mathbf{ V} _j ^{ (m)} + \mathbf{ S} _j ^{ (m)}
        //$$
        return V + S;
    }
}
