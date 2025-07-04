using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Microsoft.Extensions.Logging;
using Services;
using System.Text;
using System.Threading.Tasks;

namespace Processors
{
    public interface IMetricProcessor
    {
        void Generate(Patient patient);
        void Log(Patient patient, ILogger logger);
    }
}
