using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;

public interface IGas
{
    const int DEFAULT_POINTS_COUNT = 100;
    int PointsCount { get; init; }
    double[] Densitys { get; }
    double[] Pressures { get; }
    double[] Velocitys { get; }
}
