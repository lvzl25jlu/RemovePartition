using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RemovePartition;
public static class Utils
{
    // 平方
    public static double Square(this double x) => x * x;
    // 指数
    public static double Pow(this double x, double y) => Math.Pow(x, y);
    //tex:
    // $$
    //   \begin{cases}
    //      0&x\le 0\\
    //      x &x>0\\
    //    \end{cases}
    // $$
    public static double N0PX(this double x) => x <= 0 ? 0 : x;

    public static void Draw(this Canvas canvas, double[] datas) =>
        Draw(canvas, datas, (0, 100), Brushes.Black);
    public static void Draw(this Canvas canvas, double[] datas, Brush brush) =>
        Draw(canvas, datas, (0, 100), brush);
    public static void Draw(this Canvas canvas, double[] datas, (double min, double max) range) =>
        Draw(canvas, datas, range, Brushes.Black);
    public static void Draw(this Canvas canvas, double[] datas, (double min, double max) range, Brush brush)
    {
        var interval = canvas.ActualWidth / (datas.Length - 1);
        //if(interval <= 1)
        //{
        //	canvas.Children.Add(new TextBlock { Text = "画布太窄" });
        //	return;
        //}
        var h = range.max - range.min;
        var H = canvas.ActualHeight;
        var pathFigure = new PathFigure()
        {
            StartPoint = new Point(0, H * (range.max - datas[0]) / h),
        };
        // 0点在上面的 StartPoint 里面
        for(int i = 1; i < datas.Length; i++)
        {
            var point = new Point
            {
                X = i * interval,
                Y = H * (range.max - datas[i]) / h
            };
            pathFigure.Segments.Add(new LineSegment(point, true));
        }

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        var path = new Path
        {
            Stroke = brush,
            StrokeThickness = 1,
            Data = pathGeometry
        };

        canvas.Children.Add(path);
    }
}

public static class Differencer
{
    public static double ForwardDifference<T>(this T[] arr, Func<T, double> map, int idx) =>
        (map(arr[idx + 1]) - map(arr[idx]));
    public static double BackwardDifference<T>(this T[] arr, Func<T, double> map, int idx) =>
        (map(arr[idx]) - map(arr[idx - 1]));
    public static double CenterDifference<T>(this T[] arr, Func<T, double> map, int idx) =>
        (map(arr[idx + 1]) - map(arr[idx - 1]));
}

public struct Vec3<T> :
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
        set
        {
            switch(index)
            {
                case 1:
                    Value1 = value;
                    break;
                case 2:
                    Value2 = value;
                    break;
                case 3:
                    Value3 = value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }

    public static implicit operator Vec3<T>((T, T, T) tuple) =>new()
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

    public static Vec3<T> operator *(Vec3<T> lsh,T  rsh) => new()
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