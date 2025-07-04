using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Metric
    {
        public double Value { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.MinValue;
    }
}
