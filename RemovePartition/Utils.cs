using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    extension(double x)
    {
        //tex: $x^2$
        public double Square => x * x;
        //tex: $\frac 12x^2$
        public double HSq => x * x / 2;
        //tex: $x^y$
        public double Pow(double y) => Math.Pow(x, y);
        //tex:
        // $$
        //   \begin{cases}
        //      0&x\le 0\\
        //      x &x>0\\
        //    \end{cases}
        // $$
        public double N0PX() => x <= 0 ? 0 : x;
    }

    extension(Canvas that)
    {
        public void Draw(double[] datas) =>
            Draw(that, datas, (0, 100), Brushes.Black);
        public void Draw(double[] datas, Brush brush) =>
            Draw(that, datas, (0, 100), brush);
        public void Draw(double[] datas, (double min, double max) range) =>
            Draw(that, datas, range, Brushes.Black);
        public void Draw(double[] datas, (double min, double max) range, Brush brush)
        {
            var interval = that.ActualWidth / (datas.Length - 1);
            var h = range.max - range.min;
            var H = that.ActualHeight;
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

            that.Children.Add(path);
        }
    }
}

public static class LegendrePolynomials
{   /// <summary>
    /// 勒让德多项式
    /// </summary>
    //tex:勒让德多项式：
    //$$
    //  \begin{array}{c|c|c|c}
    //  l & 0 & 1 & 2 & \cdots\\\hline
    //  \phi^{(l)}\left(\xi\right) & 1 & \xi & \frac{1}{ 2}\left(3\xi ^ 2 - 1\right) & \cdots
    //  \end{array}
    //$$
    public static readonly ReadOnlyCollection<Func<double, double>> Phi = [
        x => 1,
        x => x,
        x => 0.5 * (3 * x * x - 1)
    ];
    /// <summary>
    /// 勒让德多项式的导数
    /// </summary>
    public static readonly ReadOnlyCollection<Func<double, double>> Dphi = [
        x => 0,
        x => 1,
        x => 3 * x
    ];
}

public static class GuessIntegrate
{
    //tex: 高斯两点型积分
    //$$
    //\begin{array}{c|c|c}
    //  \xi_i  & -\frac 1{\sqrt3} & \frac 1{\sqrt3} 
    //  \\\hline
    //  \omega_i & 1 & 1
    //\end{array}
    //$$
    public static readonly ReadOnlyCollection<(double X, double W)> GuessPoint2 = [
        (-Math.Sqrt(1.0 / 3.0), 1.0),
        (+Math.Sqrt(1.0 / 3.0), 1.0)
    ];
    /// <summary>
    /// 两点型高斯积分
    /// </summary>
    /// <param name="f">被积函数</param>
    /// <param name="l">积分下限</param>
    /// <param name="r">积分上限</param>
    /// <returns>积分结果</returns>
    public static double Integrate2(this Func<double, double> f, double l = -1, double r = +1)
    {
        var h = (r - l) / 2;
        var m = (r + l) / 2;
        //tex: $\int_{l}^{r}f\left(x\right)\mathrm dx\approx \sum_i\omega_if\left(m+h\xi_i\right)$
        return h*GuessPoint2
            .Select(g => f(m + g.X * h) * g.W)
            .Aggregate((a, b) => a + b);
    }

    /// <summary>
    /// 两点型高斯积分
    /// </summary>
    /// <param name="f">被积函数</param>
    /// <param name="l">积分下限</param>
    /// <param name="r">积分上限</param>
    /// <returns>积分结果</returns>
    public static Vec3<double> Integrate2(this Func<double, Vec3<double>> f, double l = -1, double r = +1)
    {
        var h = (r - l) / 2;
        var m = (r + l) / 2;
        //tex: $\int_{l}^{r}f\left(x\right)\mathrm dx\approx h\sum_i\omega_if\left(m+h\xi_i\right)$
        return h*GuessPoint2
            .Select(g => f(m + g.X * h) * g.W)
            .Aggregate((a, b) => a + b);
    }
}



