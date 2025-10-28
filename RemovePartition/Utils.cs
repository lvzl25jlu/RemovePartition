using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		if(interval <= 1)
		{
			canvas.Children.Add(new TextBlock { Text = "画布太窄" });
			return;
		}
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

		canvas.Children.Clear();
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

/// <summary>
/// 这是一个工具类，用以实现类数组属性。<br/>
/// 对<c>obj.ArrayLike[i]</c>这样的代码，
/// <c>ArrayLike</c>若为<br/>
/// <list type="bullet">
///     <item>
///         <term><c>T[]</c></term>
///         <description>则每次访问时都会重新构造一遍数组</description>
///     </item>
///     <item>
///         <term><c>Indexer&lt;T&gt;</c></term>
///         <description>则只会在访问时触发对应的索引器</description>
///     </item>
/// </list>
/// </summary>
public class Indexer<T> : IEnumerable<T>
{
	public Func<int, T> Getter { init; private get; } =
		_ => throw new NotImplementedException();
	public Action<int, T> Setter { init; private get; } =
		(_, _) => throw new NotImplementedException();
	public T this[int index]
	{
		get => Getter(index);
		set => Setter(index, value);
	}

	public Func<IEnumerator<T>> Enumer { init; private get; } =
		() => throw new NotImplementedException();

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
		=> Enumer();

	IEnumerator IEnumerable.GetEnumerator()
		=> Enumer();
}