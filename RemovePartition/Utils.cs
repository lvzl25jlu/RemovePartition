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
    extension(double x)
    {
        // 平方
        public double Square => x * x;
        // 指数
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
            //if(interval <= 1)
            //{
            //	canvas.Children.Add(new TextBlock { Text = "画布太窄" });
            //	return;
            //}
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


