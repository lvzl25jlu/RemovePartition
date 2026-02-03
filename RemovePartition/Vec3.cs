using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RemovePartition;

public struct Vec3<T>(T v1, T v2, T v3)
{
    public T Value1 = v1;
    public T Value2 = v2;
    public T Value3 = v3;
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

    public static implicit operator Vec3<T>((T, T, T) that) => new()
    {
        Value1 = that.Item1,
        Value2 = that.Item2,
        Value3 = that.Item3
    };

    public static implicit operator (T, T, T)(Vec3<T> that)
        => (that.Value1, that.Value2, that.Value3);
    public readonly void Deconstruct(out T v1, out T v2, out T v3)
    {
        v1 = Value1;
        v2 = Value2;
        v3 = Value3;
    }
}

public static class Vec3Operators
{
    extension<T>(Vec3<T>)
        where T :
        IAdditionOperators<T, T, T>
    {
        public static Vec3<T> operator +(Vec3<T> lhs, Vec3<T> rhs) => new()
        {
            Value1 = lhs.Value1 + rhs.Value1,
            Value2 = lhs.Value2 + rhs.Value2,
            Value3 = lhs.Value3 + rhs.Value3
        };
    }
    extension<T>(Vec3<T>)
        where T :
        ISubtractionOperators<T, T, T>
    {
        public static Vec3<T> operator -(Vec3<T> lhs, Vec3<T> rhs) => new()
        {
            Value1 = lhs.Value1 - rhs.Value1,
            Value2 = lhs.Value2 - rhs.Value2,
            Value3 = lhs.Value3 - rhs.Value3
        };
    }
    extension<T>(Vec3<T>)
        where T :
        IMultiplyOperators<T, T, T>,
        IDivisionOperators<T, T, T>
    {
        public static Vec3<T> operator *(T lhs, Vec3<T> rhs) => new()
        {
            Value1 = lhs * rhs.Value1,
            Value2 = lhs * rhs.Value2,
            Value3 = lhs * rhs.Value3
        };
        public static Vec3<T> operator *(Vec3<T> lhs, T rhs) => new()
        {
            Value1 = lhs.Value1 * rhs,
            Value2 = lhs.Value2 * rhs,
            Value3 = lhs.Value3 * rhs
        };
        public static Vec3<T> operator /(Vec3<T> lhs, T rhs) => new()
        {
            Value1 = lhs.Value1 / rhs,
            Value2 = lhs.Value2 / rhs,
            Value3 = lhs.Value3 / rhs
        };
    }
    extension<T>(IEnumerable<Vec3<T>> that)
        where T : IAdditionOperators<T, T, T>
    {
        public Vec3<T> Sum()
        {
            using var enumerator = that.GetEnumerator();
            if(!enumerator.MoveNext())
                throw new ArgumentException("Sequence contains no elements", nameof(that));
            var res = enumerator.Current;
            while(enumerator.MoveNext())
            {
                res += enumerator.Current;
            }
            return res;
        }
    }
}

