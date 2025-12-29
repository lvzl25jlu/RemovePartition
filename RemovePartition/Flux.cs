using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;

delegate FluxVaribles FluxCalculator(FluidVaribles le, FluidVaribles ri);

struct FluxVaribles : IVec3<double>
{
    public double this[int index]
    {
        readonly get => index switch
        {
            1 => F_rho,
            2 => F_p,
            3 => F_E,
            _ => throw new IndexOutOfRangeException(),
        };
        set => _ = index switch
        {
            1 => F_rho = value,
            2 => F_p = value,
            3 => F_E = value,
            _ => throw new IndexOutOfRangeException(),
        };
    }

    public double F_rho { get; set; }
    public double F_p { get; set; }
    public double F_E { get; set; }

    public static implicit operator Vec3<double>(FluxVaribles that) =>
        (that.F_rho, that.F_p, that.F_E);
    public static implicit operator FluxVaribles(Vec3<double> that) => new()
    {
        F_rho = that[1],
        F_p = that[2],
        F_E = that[3],
    };
}

