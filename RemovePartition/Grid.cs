using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace RemovePartition;

struct Grid<TData>(double x = 0, double w = 2)
    where TData : struct, IGridData<TData>
{
    /// <summary>
    /// 网格中心坐标
    /// </summary>
    public double X { get; set; } = x;
    /// <summary>
    /// 网格宽度（缩写）
    /// </summary>
    public double W { get; set; } = w;
    /// <summary>
    /// 网格范围
    /// </summary>
    public (double L, double R) Range
    {
        readonly get => (X - W / 2, X + W / 2);
        set
        {
            X = (value.L + value.R) / 2;
            W = value.R - value.L;
        }
    }
    /// <summary>
    /// 网格中的流体信息
    /// </summary>
    public TData Data { get; set; }
    /// <summary>
    /// 从网格中直接提取流体信息
    /// </summary>
    /// <param name="that"></param>
    public FluidVaribles FluVar
    {
        readonly get => Data.FluVar;
        set => Data = Data with { FluVar = value };
    }
    /// <summary>
    /// 从网格中直接提取流体信息
    /// </summary>
    /// <param name="that"></param>
    public static implicit operator FluidVaribles(Grid<TData> @this) => @this.Data.FluVar;
}

/// <summary>
/// 对DG方法而言，构造下列扩展方法方便使用
/// </summary>
static class GridDGExtensions
{
    extension(Grid<DgData> that)
    {
        /// <summary>
        /// 坐标变换后的基函数
        /// </summary>
        public Func<double, double>[] Phi => [.. LegendrePolynomials.Phi.Select<Func<double, double>,
            //tex:$\phi^{(m)}\left(\xi\left(x\right)\right)=\phi^{(m)}\left(\frac{x - x_{j}}{w_{j}/2}\right)$
            Func<double, double>>(fi =>x => fi((x - that.X) / that.W / 2))];

        /// <summary>
        /// Uh函数，即守恒量离散后的表达式
        /// </summary>
        /// <remarks>自变量是区间坐标</remarks>
        public Func<double, Vec3<double>> Uhx => Enumerable.Range(0, DgData.Order+1)
            //tex:$\mathbf{U}_h\left(x\right)=\sum_{m}\mathbf{U}^{(m)}\phi^{(m)}\left(\xi\left(x\right)\right)$
            .Select<int, Func<double, Vec3<double>>>(m => x => that.Data[m] * that.Phi[m](x))
            .Aggregate((a, b) => a + b);

        /// <summary>
        /// Uh函数，即守恒量离散后的表达式
        /// </summary>
        /// <remarks>自变量是将区间变换到[-1,+1]上的坐标</remarks>
        public Func<double, Vec3<double>> Uhxi => Enumerable.Range(0, DgData.Order + 1)
            //tex:$\mathbf{U}_h\left(\xi\right)=\sum_{m}\mathbf{U}^{(m)}\phi^{(m)}\left(\xi\right)$
            .Select<int, Func<double, Vec3<double>>>(m => xi => that.Data[m] * LegendrePolynomials.Phi[m](xi))
            .Aggregate((a, b) => a + b);

        /// <summary>
        /// 计算网格中任意一点的流体变量
        /// </summary>
        /// <param name="x">自行保证在 Range 的范围内</param>
        /// <returns>x 处流体的信息</returns>
        public FluidVaribles FluVarAt(double x) => new()
        {
            U = that.Data.Select((um, m) => um * that.Phi[m](x)).Sum()
        };
    }
}
