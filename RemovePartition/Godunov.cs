using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RemovePartition;

partial class FluentGas
{
    public void Godunov(FluxCalculator fluxCal)
    {
        //tex:fluxs[i] 表示 $F_{i-\frac{1}{2}}$
        FluxVaribles[] fluxs = new FluxVaribles[PointsCount + 1];
        fluxs[0] = fluxCal(Ps[0] with
        {
            Velocity = -Ps[0].Velocity
        }, Ps[0]);
        for (int i = 1; i < points.Length; i++)
        {
            fluxs[i] = fluxCal(Ps[i - 1], Ps[i]);
        }
        fluxs[^1] = fluxCal(Ps[^1], Ps[^1] with
        {
            Velocity = -Ps[^1].Velocity
        });

        //tex: 辅助变量$\frac{\Delta t}{\Delta x}$
        double aux = Delta_t / Delta_x;
        FluidVaribles[] next = [.. Ps];
        //  一阶欧拉
        for (int i = 0; i < PointsCount; i++)
        {
            //tex: $$\Delta U = \frac{F_{i+1}-F_{i}}{\Delta x}\Delta t$$
            var dU = new FluidVaribles
            {
                ConservedVariables = (fluxs[i + 1] - fluxs[i]) * aux
            };
            //tex: $$U_{i}^{\left(n+1\right)}=U_{i}^{\left(n\right)} - \Delta U$$
            next[i].U -= dU;
        }
        points = next;
    }
}
