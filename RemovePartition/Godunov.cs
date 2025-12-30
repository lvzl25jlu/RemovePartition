using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RemovePartition;

partial class FluentGas
{
    public void Godunov(FluxCalculator fluxCal, TimeAdvancer timeAdv)
    {
        points = timeAdv(DeltaT, Ps, flus =>
        {
            //tex:fluxs[i] 表示 $F_{i-\frac{1}{2}}$
            FluxVaribles[] fluxs = new FluxVaribles[PointsCount + 1];
            fluxs[0] = fluxCal(Ps[0] with
            {
                Velocity = -Ps[0].Velocity
            }, Ps[0]);
            for(int i = 1; i < points.Length; i++)
            {
                fluxs[i] = fluxCal(Ps[i - 1], Ps[i]);
            }
            fluxs[^1] = fluxCal(Ps[^1], Ps[^1] with
            {
                Velocity = -Ps[^1].Velocity
            });
            //tex:$\mathbf{R}_j=-\frac{1}{\Delta x}\left(\mathbf{F}_{j+\frac 12}-\mathbf{F}_{j-\frac 12}\right)$
            return [.. flus.Select((_, i) =>
                (fluxs[i]-fluxs[i + 1] )/DeltaX
            )];
        });
    }
}
