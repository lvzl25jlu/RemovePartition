using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;

internal class Godunov : IGas
{
    public int PointsCount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public double[] Densitys => throw new NotImplementedException();

    public double[] Pressures => throw new NotImplementedException();
}
