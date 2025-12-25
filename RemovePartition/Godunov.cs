using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;

partial class FluentGas
{
    public void Godunov(FluxCalculator fluxCal)
    {
        FluxVaribles[] fluxs = [.. Ps[1..].Select((_, i) =>
            fluxCal(Ps[i], Ps[i+1])
        )];

        FluidVaribles[] next = [.. Ps];

        //  一阶欧拉
        for(int i = 1; i < fluxs.Length; i++)
        {
            //tex: $$\Delta U = \frac{F_{i+1}-F_{i}}{\Delta x}\Delta t$$
            var dU = new FluidVaribles
            {
                ConservedVariables = (fluxs[i] - fluxs[i - 1])
                    / Delta_x * Delta_t
            };
            //tex: $$U_{i}^{\left(n+1\right)}=U_{i}^{\left(n\right)} - \Delta U$$
            next[i] = Ps[i] - dU;
        }
    }
}
