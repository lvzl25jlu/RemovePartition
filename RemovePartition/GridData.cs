using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace RemovePartition;

public interface IGridData<TData>
    where TData : IGridData<TData>
{
    FluidVaribles FluVar { get; set; }
    abstract static TData operator +(TData lsh, TData rsh);
    abstract static TData operator *(TData lsh, double rsh);
}

/// <summary>
/// 适用于有限体积的网格数据
/// </summary>
public struct FvmData : IGridData<FvmData>
{
    //tex:FVM网格数据仅包含守恒变量的平均值 $\bar{\mathbf{U}}$：
    public Vec3<double> U { get; set; }
    public FluidVaribles FluVar
    {
        get => new() { U = U };
        set => U = value.U;
    }
    public static FvmData operator +(FvmData lhs, FvmData rhs) => new()
    {
        U = lhs.U + rhs.U
    };
    public static FvmData operator *(FvmData lhs, double rhs) => new()
    {
        U = lhs.U * rhs
    };
}

/// <summary>
/// 适用于间断伽辽金法的网格数据
/// </summary>
[InlineArray(Order + 1)]
public struct DgData : IGridData<DgData>, IEnumerable<Vec3<double>>
{
    // DG方法的阶数，m能取到的最大值
    public const int Order = 2;
    //tex:DG方法网格包含模态系数 $U^{(m)}$：
    private Vec3<double> _data;
    public FluidVaribles FluVar
    {
        get => new() { U = this[0] }; set
        {
            this[0] = value.U;
            for(int i = 1; i <= Order; i++)
            {
                this[i] = (0, 0, 0);
            }
        }
    }

    public readonly IEnumerator<Vec3<double>> GetEnumerator()
    {
        var that = this;
        return Enumerable.Range(0, Order + 1).Select(i => that[i]).GetEnumerator();
    }

    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    

    public static DgData operator +(DgData lhs, DgData rhs)
    {
        var res = new DgData();
        for(int i = 0; i <= Order; i++)
        {
            res[i] = lhs[i] + rhs[i];
        }
        return res;
    }
    public static DgData operator *(DgData lhs, double rhs)
    {
        var res = new DgData();
        for(int i = 0; i <= Order; i++)
        {
            res[i] = lhs[i] * rhs;
        }
        return res;
    }
}

public static class GridDataArrayExt
{
    extension<TData>(TData[])
        where TData : struct, IGridData<TData>
    {
        public static TData[] operator +(TData[] lhs, TData[] rhs)
        {
            if(lhs.Length != rhs.Length)
            {
                throw new ArgumentException("数组长度不匹配，无法相加");
            }
            var length = (lhs.Length + rhs.Length) / 2;
            return [.. Enumerable.Range(0, length)
                .Select(i => lhs[i] + rhs[i])];
        }

        public static TData[] operator *(TData[] lhs, double rhs)
            => [.. lhs.Select(item => item * rhs)];
    }
}