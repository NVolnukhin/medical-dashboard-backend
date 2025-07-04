using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public interface IGeneratorService
    {
        double GenerateHeartRate(double? previous);
        double GenerateSaturation(double? previous);
        (double systolic, double diastolic) GeneratePressure();
        double GenerateWeight(double? previous, double baseWeight);
        double GenerateBMI(double? previous, double baseWeight, double height);
        double GenerateTemperature(double? previous);
        double GenerateRespiration(double? previous);
        double GenerateHemoglobin(double? previous);
        double GenerateCholesterol(double? previous);
    }
}
