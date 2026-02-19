namespace DataCollectorService.Services
{
    public interface IGeneratorService
    {
        double GeneratePulse(double? previous);
        double GenerateSaturation(double? previous);
        double GenerateSystolicPressure();
        double GenerateDiastolicPressure();
        double GenerateWeight(double? previous, double baseWeight);
        double GenerateBMI(double? previous, double baseWeight, double? height);
        double GenerateTemperature(double? previous);
        double GenerateRespiration(double? previous);
        double GenerateHemoglobin(double? previous);
        double GenerateCholesterol(double? previous);
    }
}
