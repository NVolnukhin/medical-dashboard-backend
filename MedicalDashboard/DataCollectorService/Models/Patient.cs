namespace Models
{
    public class Patient
    {
        public int Id;
        public int Age { get; set; }
        public int UpdateCount { get; set; }
        public string? Sex { get; set; }
        public string? Name { get; init; }

        public double BaseWeight { get; set; }
        public double Height { get; set; }
        public MedicalIndicator CurrentMetrics { get; set; } = new();
    }
}