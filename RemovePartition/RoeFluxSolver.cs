using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemovePartition;
internal class RoeFluxSolver : IFluxSolver
{
    // https://github.com/rustamNSU/GodunovsMethod/blob/master/src/RiemannSolvers.cpp
    public FluxVaribles CalculateFlux(FluidVaribles lFV, FluidVaribles rFV)
    {
        double sqrtRhoL = Math.Sqrt(lFV.Density);
        double sqrtRhoR = Math.Sqrt(rFV.Density);

        // Roe average states
        double aveVelocity = (sqrtRhoL * lFV.Velocity + sqrtRhoR * rFV.Velocity)
            / (sqrtRhoL + sqrtRhoR);
        double aveEnthalpy = (sqrtRhoL * lFV.Enthalpy + sqrtRhoR * rFV.U_E)
            / (sqrtRhoL + sqrtRhoR);
        double aveSoundSpeed = Math.Sqrt(
            (FluidVaribles.SpecHeatRatio - 1) *
            (aveEnthalpy - 0.5 * aveVelocity.Square())
        );
        double aveDensity = sqrtRhoL * sqrtRhoR;

        double deltaDensity = rFV.Density - lFV.Density;
        double deltaPressure = rFV.Pressure - lFV.Pressure;
        double deltaVelocity = rFV.Velocity - lFV.Velocity;
        double auxCoeff = 1 / aveSoundSpeed.Square();

        double alpha1 = 0.5 * auxCoeff * (deltaPressure - aveSoundSpeed * aveDensity * deltaVelocity);
        double alpha2 = deltaDensity - deltaPressure * auxCoeff;
        double alpha3 = 0.5 * auxCoeff * (deltaPressure + aveSoundSpeed * aveDensity * deltaVelocity);
        Vec3<double> alpha = (alpha1, alpha2, alpha3);


        double eigenValue1 = aveVelocity - aveSoundSpeed;
        double eigenValue2 = aveVelocity;
        double eigenValue3 = aveVelocity + aveSoundSpeed;
        Vec3<double> eigen = (eigenValue1, eigenValue2, eigenValue3);

        // double xBoundary = 0;

        Vec3<double> r1 = (
            1.0,
            aveVelocity - aveSoundSpeed,
            aveEnthalpy - aveVelocity * aveSoundSpeed
        );
        Vec3<double> r2 = (
            1.0,
            aveVelocity,
            0.5 * aveVelocity.Square()
        );
        Vec3<double> r3 = (
            1.0,
            aveVelocity + aveSoundSpeed,
            aveEnthalpy + aveVelocity * aveSoundSpeed
        );
        Vec3<Vec3<double>> r = (r1, r2, r3);

        Vec3<double> consercation = (Vec3<double>)lFV.U + (Vec3<double>)rFV.U;

        double gamma = FluidVaribles.SpecHeatRatio;

        Vec3<double> temp = (
            consercation[2],
            (gamma - 3) * aveVelocity.Square() / 2 * consercation[1]
                + (3 - gamma) * aveVelocity * consercation[2]
                + (gamma - 1) * consercation[3],
            ((gamma - 1) * Math.Pow(aveVelocity, 3) / 2 - aveVelocity * aveEnthalpy) * consercation[1]
                + (aveEnthalpy - (gamma - 1) * aveVelocity.Square()) * consercation[2]
                + gamma * aveVelocity * consercation[3]
        );

        //flux = 0.5 * temp - 0.5 * (
        //          Math.Abs(eigenValue1) * alpha1 * r1
        //          + Math.Abs(eigenValue2) * alpha2 * r2
        //          + Math.Abs(eigenValue3) * alpha3 * r3
        //    );

        for(int i = 1; i <= 3; i++)
        {
            temp -= Math.Abs(eigen[i]) * alpha[i] * r[i];
        }

        FluxVaribles flux = 0.5 * temp;

        return flux;
    }
}
