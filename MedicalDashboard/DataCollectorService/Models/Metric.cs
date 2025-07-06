namespace DataCollectorService.Models
{
    public class Metric
    {
        public double Value { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.MinValue;
    }
}
