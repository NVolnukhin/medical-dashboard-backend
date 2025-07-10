using DataCollectorService.Models;

namespace DataCollectorService.Observerer
{
    public interface ISubject
    {
        void Attach(IObserver observer);
        void Detach(IObserver observer);
        Task Notify(List<Patient> patients);
    }
}
