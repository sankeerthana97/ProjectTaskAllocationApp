using ProjectTaskAllocationApp.Models;
using System.Threading.Tasks;

namespace ProjectTaskAllocationApp.Services
{
    public interface IWorkloadService
    {
        Task<int> CalculateWorkload(ApplicationUser user);
        bool IsOverloaded(ApplicationUser user);
        Task UpdateWorkload(ApplicationUser user);
    }
}