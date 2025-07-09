using DataCollectorService.Models;

namespace DataCollectorService.Observerer
{
    public interface IObserver
    {
        Task Update(List<Patient> patients);
    }
}
