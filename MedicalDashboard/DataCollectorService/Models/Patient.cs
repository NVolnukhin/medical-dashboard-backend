namespace DataCollectorService.Models
{
    public class Patient
    {
        public Guid Id { get; set;  } = Guid.NewGuid();
        public int Age { get; set; }
        public Dictionary<string, DateTime> MetricLastGenerations = new Dictionary<string, DateTime>();
        public string? Sex { get; set; }
        public string? Name { get; set; }
        public int? Ward {  get; set; }

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
        public double? Height { get; set; }

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
            var now = DateTime.UtcNow;
            MetricLastGenerations = new Dictionary<string, DateTime>()
            {
                ["Pulse"] = now,
                ["Saturation"] = now,
                ["Weight"] = now,
                ["BMI"] = now,
                ["Hemoglobin"] = now,
                ["Cholesterol"] = now,
                ["SysPressure"] = now,
                ["DiasPressure"] = now,
                ["Temperature"] = now,
                ["Respiration"] = now
            };
        }
    }
}