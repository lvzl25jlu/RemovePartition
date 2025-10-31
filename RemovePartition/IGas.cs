using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;

public interface IGas
{
    public const int DEFAULT_POINTS_COUNT = 10000;
    public int PointsCount { get; set; }
    public double[] Densitys { get; }
    public double[] Pressures { get; }
    public double[] Velocitys => [.. Enumerable.Repeat(0, PointsCount)];
}

