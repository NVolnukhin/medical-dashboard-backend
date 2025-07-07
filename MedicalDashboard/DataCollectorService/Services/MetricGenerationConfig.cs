namespace DataCollectorService.Services
{
    public class MetricGenerationConfig
    {
        public int PulseIntervalSeconds { get; set; } = 30;
        public int SaturationIntervalSeconds { get; set; } = 30;
        public int BmiIntervalSeconds { get; set; } = 30;
        public int TemperatureIntervalSeconds { get; set; } = 60;
        public int RespirationIntervalSeconds { get; set; } = 60;
        public int PressureIntervalSeconds { get; set; } = 60;
        public int HemoglobinIntervalSeconds { get; set; } = 60;
        public int WeightIntervalSeconds { get; set; } = 300;
        public int CholesterolIntervalSeconds { get; set; } = 300;
    }
}
