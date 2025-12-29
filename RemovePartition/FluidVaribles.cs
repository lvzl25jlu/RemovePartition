using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;

public struct FluidVaribles:IVec3<double>
{
    //tex: $\gamma$
    public static double SpecHeatRatio { get => gamma; set => gamma = value; }
    static double gamma = 1.3f;
    //tex: $R$
    public static double GasConst { get => R; set => R = value; }
    static double R = 287.0f;
    //tex:用 $\rho$ 、 $u$ 、 $p$ 作为原始变量
    double rho;
    double u;
    double p;
    public double Density
    {
        readonly get => rho;
        set
        {
            //ArgumentOutOfRangeException.ThrowIfNegative(value);
            rho = value;
        }
    }
    public double Pressure
    {
        readonly get => p;
        set
        {
            //ArgumentOutOfRangeException.ThrowIfNegative(value);
            p = value;
        }
    }
    public double Velocity
    {
        readonly get => u;
        set => u = value;
    }
    //tex:$h=E+\frac p\rho=\frac{\gamma}{\gamma-1}\frac p\rho+\frac 12 u^2$
    public readonly double Enthalpy => gamma / (gamma - 1) * p / rho + u * u / 2;
    //tex:$c=\sqrt{\gamma\frac p\rho}$
    public readonly double SoundSpeed => Math.Sqrt(gamma * p / rho);
    public double U_rho
    {
        //tex: $U_\rho=\rho$
        readonly get => rho;
        //tex: $\rho=U_\rho$
        set => Density = value;
    }
    public double U_p
    {
        //tex: $U_p=u\rho$
        readonly get => rho * u;
        //tex: $u=\frac {U_p}\rho$
        set => Velocity = rho < 1e-6 ? 0 : value / rho;
    }
    public double U_E
    {
        //tex: $U_E=\rho E=\frac{p}{\gamma-1}+\frac{1}{2}\rho u^2$
        readonly get => p / (gamma - 1) + rho * u * u / 2;
        //tex: $p=\left(\gamma-1\right)\left(U_E-\frac 12\rho u^2\right)$
        set => Pressure = (gamma - 1) * (value - rho * u * u / 2);
    }

    //tex: $F_\rho=\rho u=U_p$
    public readonly double F_rho => rho * u;
    //tex: $F_p=\rho u^2+p$
    public readonly double F_p => rho * u * u + p;
    //tex: $F_E=u\left(\rho E+p\right)$ where $\rho E=U_E$
    public readonly double F_E => u * (U_E + p);

    //tex:$U=\begin{pmatrix}U_\rho\\U_p\\U_E\end{pmatrix}$
    public Vec3<double> U
    {
        readonly get => (U_rho, U_p, U_E);
        set => (U_rho, U_p, U_E) = ((double, double, double))value;
    }
    //  就是U，但是名字更长
    public Vec3<double> ConservedVariables
    {
        readonly get => (U_rho, U_p, U_E);
        set => (U_rho, U_p, U_E) = ((double, double,double))value;
    }
    //tex:$F=\begin{pmatrix}F_\rho\\F_p\\F_E\end{pmatrix}$
    public readonly Vec3<double> F => (F_rho, F_p, F_E);
    

    public double this[int index]
    {
        readonly get => index switch
        {
            1 => U_rho,
            2 => U_p,
            3 => U_E,
            _ => throw new IndexOutOfRangeException(),
        };
        set => _ = index switch
        {
            1 => U_rho = value,
            2 => U_p = value,
            3 => U_E = value,
            _ => throw new IndexOutOfRangeException(),
        };
    }
}