using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;

struct FluxVaribles
{
    public double F_rho { get; set; }
    public double F_p { get; set; }
    public double F_E { get; set; }

    public static implicit operator Vec3<double>(FluxVaribles that) =>
        (that.F_rho, that.F_p, that.F_E);
    public static implicit operator FluxVaribles(Vec3<double> that) =>
        new()
        {
            F_rho = vec[0],
            F_p = vec[1],
            F_E = vec[2],
        };
}


internal interface IFluxSolver
{
    public FluxVaribles CalculateFlux(FluidVaribles lFV, FluidVaribles rFV);
}
