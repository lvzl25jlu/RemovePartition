using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace RemovePartition;

/// <summary>
/// 任意一点的流体变量
/// 你不应该假设它保存的是守恒量还是原始量
/// </summary>
public struct FluidVaribles
{
    static double gamma = 1.4;
    public static double SpecHeatRatio { get => gamma; set => gamma = value; }
    static double R = 287.0;
    public static double GasConst { get => R; set => R = value; }
    public double Density
    {
        //tex: $\rho=U_\rho$
        readonly get => U_rho;
        //tex: $U_\rho=\rho$
        set => U_rho = value;
    }
    public double Pressure
    {
        //tex: $p=\left(\gamma-1\right)\left(U_E-\frac 12\rho u^2\right)$
        readonly get => (gamma - 1) * (U_E - Density * Velocity.HSq);
        //tex: $U_E=\rho E=\frac{p}{\gamma-1}+\frac{1}{2}\rho u^2$
        set => U_E = (value / (gamma - 1)) + Density * Velocity.HSq;
    }
    public double Velocity
    {
        //tex: $u=\frac{U_p}\rho$
        readonly get => U_rho == 0 ? 0 : U_p / U_rho;
        //tex: $U_p=\rho u$
        set => U_p = Density * value;
    }
    //tex: $h=E+\frac p\rho=\frac{\gamma}{\gamma-1}\frac p\rho+\frac 12 u^2$
    public readonly double Enthalpy => SpecHeatRatio / (SpecHeatRatio - 1) * Pressure / Density + Velocity.HSq;
    //tex: $c=\sqrt{\gamma\frac p\rho}$
    public readonly double SoundSpeed => Math.Sqrt(SpecHeatRatio * Pressure / Density);
    public double U_rho { get; set; }
    public double U_p { get; set; }
    public double U_E { get; set; }
    public Vec3<double> U
    {
        readonly get => (U_rho, U_p, U_E);
        set => (U_rho, U_p, U_E) = value;
    }
    //tex: $F_\rho=\rho u=U_p$
    public readonly double F_rho => U_p;
    //tex: $F_p=\rho u^2 + p=U_p u + p$
    public readonly double F_p => U_p * Velocity + Pressure;
    //tex: $F_E=u\left(E + p\right)=u\left(U_E + p\right)$
    public readonly double F_E => Velocity * (U_E + Pressure);
    public readonly Vec3<double> F => (F_rho, F_p, F_E);
    public static FluidVaribles FromConservativeVars(double u_rho, double u_p, double u_e)
        => new()
        {
            U_rho = u_rho,
            U_p = u_p,
            U_E = u_e
        };
    public static FluidVaribles FromConservativeVars(Vec3<double> u)
        => new()
        {
            U = u
        };
    public static FluidVaribles FromPrimitVars(double density, double velocity, double pressure)
        => new()
        {
            Density = density,
            Velocity = velocity,
            Pressure = pressure
        };
}