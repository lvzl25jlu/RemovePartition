using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RemovePartition;

interface IVec3<T> where T :
    IAdditionOperators<T, T, T>,
    ISubtractionOperators<T, T, T>,
    IMultiplyOperators<T, T, T>,
    IDivisionOperators<T, T, T>
{
    T this[int index] { get; set; }
}

static class IVec3DoubleExt
{
    extension<V>(V)
        where V : struct, IVec3<double>
    {
        public static V operator *(V lsh, double rsh) => new()
        {
            [1] = lsh[1] * rsh,
            [2] = lsh[2] * rsh,
            [3] = lsh[3] * rsh,
        };
        public static V operator *(double lsh, V rsh) => new()
        {
            [1] = lsh * rsh[1],
            [2] = lsh * rsh[2],
            [3] = lsh * rsh[3],
        };
        public static V operator /(V lsh, double rsh) => new()
        {
            [1] = lsh[1] / rsh,
            [2] = lsh[2] / rsh,
            [3] = lsh[3] / rsh,
        };
    }
    extension<U, V>(U)
        where U : struct, IVec3<double>
        where V : struct, IVec3<double>
    {
        public static Vec3<double> operator +(U lsh, V rsh) => new()
        {
            [1] = lsh[1] + rsh[1],
            [2] = lsh[2] + rsh[2],
            [3] = lsh[3] + rsh[3],
        };
        public static Vec3<double> operator -(U lsh, V rsh) => new()
        {
            [1] = lsh[1] - rsh[1],
            [2] = lsh[2] - rsh[2],
            [3] = lsh[3] - rsh[3],
        };
    }
}

public struct Vec3<T> :
    IVec3<T>,
    IAdditionOperators<Vec3<T>, Vec3<T>, Vec3<T>>,
    ISubtractionOperators<Vec3<T>, Vec3<T>, Vec3<T>>,
    IMultiplyOperators<Vec3<T>, Vec3<T>, Vec3<T>>,
    IDivisionOperators<Vec3<T>, Vec3<T>, Vec3<T>>
    where T :
    IAdditionOperators<T, T, T>,
    ISubtractionOperators<T, T, T>,
    IMultiplyOperators<T, T, T>,
    IDivisionOperators<T, T, T>
{
    public T Value1;
    public T Value2;
    public T Value3;
    public T this[int index]
    {
        readonly get => index switch
        {
            1 => Value1,
            2 => Value2,
            3 => Value3,
            _ => throw new IndexOutOfRangeException(),
        };
        set => _ = index switch
        {
            1 => Value1 = value,
            2 => Value2 = value,
            3 => Value3 = value,
            _ => throw new IndexOutOfRangeException(),
        };
        
    }

    public static implicit operator Vec3<T>((T, T, T) tuple) => new()
    {
        Value1 = tuple.Item1,
        Value2 = tuple.Item2,
        Value3 = tuple.Item3
    };

    public static implicit operator (T, T, T)(Vec3<T> vector) =>
        (vector.Value1, vector.Value2, vector.Value3);

    public static Vec3<T> operator +(Vec3<T> lsh, Vec3<T> rsh) => new()
    {
        Value1 = lsh.Value1 + rsh.Value1,
        Value2 = lsh.Value2 + rsh.Value2,
        Value3 = lsh.Value3 + rsh.Value3
    };

    public static Vec3<T> operator -(Vec3<T> lsh, Vec3<T> rsh) => new()
    {
        Value1 = lsh.Value1 - rsh.Value1,
        Value2 = lsh.Value2 - rsh.Value2,
        Value3 = lsh.Value3 - rsh.Value3
    };
    public static Vec3<T> operator *(Vec3<T> lsh, Vec3<T> rsh) => new()
    {
        Value1 = lsh.Value1 * rsh.Value1,
        Value2 = lsh.Value2 * rsh.Value2,
        Value3 = lsh.Value3 * rsh.Value3
    };
    public static Vec3<T> operator /(Vec3<T> lsh, Vec3<T> rsh) => new()
    {
        Value1 = lsh.Value1 / rsh.Value1,
        Value2 = lsh.Value2 / rsh.Value2,
        Value3 = lsh.Value3 / rsh.Value3
    };

    public static Vec3<T> operator *(T lsh, Vec3<T> rsh) => new()
    {
        Value1 = lsh * rsh.Value1,
        Value2 = lsh * rsh.Value2,
        Value3 = lsh * rsh.Value3
    };

    public static Vec3<T> operator *(Vec3<T> lsh, T rsh) => new()
    {
        Value1 = lsh.Value1 * rsh,
        Value2 = lsh.Value2 * rsh,
        Value3 = lsh.Value3 * rsh
    };

    public static Vec3<T> operator /(Vec3<T> lsh, T rsh) => new()
    {
        Value1 = lsh.Value1 / rsh,
        Value2 = lsh.Value2 / rsh,
        Value3 = lsh.Value3 / rsh
    };

}