namespace DataCollectorService.Models
{
    public class PatientState
    {
        public Dictionary<string, DateTime> MetricLastGenerations { get; set; } = new();
    }
}
