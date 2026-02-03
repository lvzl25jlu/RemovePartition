using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace RemovePartition;

class FluidField<TData> : IEnumerable<Grid<TData>>
    where TData : struct, IGridData<TData>
{
    /// <summary>
    /// 网格数
    /// </summary>
    public int Length => Grids.Length;
    /// <summary>
    /// 流场中的网格
    /// </summary>
    public Grid<TData>[] Grids { get; init; } = [];
    public Grid<TData> this[int index]
    {
        get => Grids[index];
        set => Grids[index] = value;
    }

    IEnumerator<Grid<TData>> IEnumerable<Grid<TData>>.GetEnumerator()
        => ((IEnumerable<Grid<TData>>)Grids).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => Grids.GetEnumerator();

    /// <summary>
    /// 根据 datas 数组创建一个新的流场实例
    /// </summary>
    /// <param name="datas">新流场的数据</param>
    /// <returns>新的流场</returns>
    /// <exception cref="ArgumentException">
    /// 如果datas长度和流场网格数不符抛出
    /// </exception>
    public FluidField<TData> WithData(TData[] datas)
    {
        if(datas.Length != Length)
        {
            throw new ArgumentException("数据长度与网格数量不符");
        }
        return new()
        {
            Grids = [.. Grids.Select((grid, index) => grid with
            {
                Data = datas[index]
            })]
        };
    }
    /// <summary>
    /// 获取流场中所有网格的数据
    /// </summary>
    public TData[] Datas => [.. Grids.Select(grid => grid.Data)];
}
