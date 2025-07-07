namespace DataCollectorService.Models
{
    public class Patient
    {
        public Guid Id { get; set;  } = Guid.NewGuid();
        public int Age { get; set; }
        public Dictionary<string, int> MetricIntervals = new Dictionary<string, int>();
        public string? Sex { get; set; }
        public string? Name { get; init; }

        private double _baseWeight;
        public double BaseWeight
        {
            get => _baseWeight;
            set
            {
                _baseWeight = value;
                if (Weight.Value == 0)
                {
                    Weight.Value = value;
                }
            }
        }
        public double Height { get; set; }

        public Metric Pulse { get; set; } = new();
        public Metric Saturation { get; set; } = new();
        public Metric Temperature { get; set; } = new();
        public Metric RespirationRate { get; set; } = new();
        public Metric SystolicPressure { get; set; } = new();
        public Metric DiastolicPressure { get; set; } = new();
        public Metric Hemoglobin { get; set; } = new();
        public Metric Weight { get; set; } = new();
        public Metric BMI { get; set; } = new();
        public Metric Cholesterol { get; set; } = new();

        public void InitializeIntervals()
        {
            MetricIntervals = new Dictionary<string, int>()
            {
                ["Pulse"] = 0, // Либо _random.Next(0, _config.HeartRateIntervalSeconds),
                                   // чтобы метрики генерировались не одновременно и везде так же
                ["Saturation"] = 0,
                ["Weight"] = 0,
                ["BMI"] = 0,
                ["Hemoglobin"] = 0,
                ["Cholesterol"] = 0,
                ["SysPressure"] = 0,
                ["DiasPressure"] = 0,
                ["Temperature"] = 0,
                ["Respiration"] = 0
            };
        }
    }
}