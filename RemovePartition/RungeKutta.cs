namespace RemovePartition;

delegate FluidField<TData> TimeAdvancer<TData>(FluidField<TData> fluField, double dt, RhsCalculator<TData> rhs)
    where TData : struct, IGridData<TData>;

static class RungeKutta
{
    /// <summary>
    /// 显式欧拉法，亦即一阶龙格库塔
    /// </summary>
    /// <param name="ff"></param>
    /// <param name="dt">Δt</param>
    /// <param name="rhs">右手项计算函数</param>
    /// <returns>下一时刻流场</returns>
    public static FluidField<TData> Eular<TData>(FluidField<TData> ff, double dt, RhsCalculator<TData> rhs)
        where TData : struct, IGridData<TData>
    {
        //tex:显式欧拉法的公式为：
        //$$
        //\mathbf{U}_j^{(n+1)}=\mathbf{U}_j^{(n)}+\Delta t\mathbf{R}\left(\mathbf{U}^{(n)}\right)_j
        //$$
        var Un = ff.Select(g => g.Data).ToArray();
        var R = rhs(ff);
        var Unew = Un + R * dt;
        return ff.WithData(Unew);
    }

    /// <summary>
    /// 三阶龙格库塔法
    /// </summary>
    /// <param name="ff">当前时刻流场</param>
    /// <param name="dt">Δt</param>
    /// <param name="rhs">右手项计算函数</param>
    /// <returns>下一时刻流场</returns>
    public static FluidField<TData> RK3<TData>(FluidField<TData> ff, double dt, RhsCalculator<TData> rhs)
        where TData : struct, IGridData<TData>
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
        var Un = ff.Datas;
        var RUn = rhs(ff);
        var U1 = Un + RUn * dt;
        var RU1 = rhs(ff.WithData(U1));
        var U2 = Un * 0.75 + U1 * 0.25 + RU1 * (dt * 0.25);
        var RU2 = rhs(ff.WithData(U2));
        var Unew = Un * (1.0 / 3.0) + U2 * (2.0 / 3.0) + RU2 * (dt * (2.0 / 3.0));
        return ff.WithData(Unew);
    }
}

