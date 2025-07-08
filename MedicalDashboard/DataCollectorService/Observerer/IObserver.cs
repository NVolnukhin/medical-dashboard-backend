using DataCollectorService.Models;

namespace DataCollectorService.Observerer
{
    public interface IObserver
    {
        Task Update(Patient patient);
    }
}
