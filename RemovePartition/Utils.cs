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

