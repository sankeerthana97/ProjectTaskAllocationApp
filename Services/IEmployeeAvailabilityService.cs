using System.Threading.Tasks;
using ProjectTaskAllocationApp.Models;

namespace ProjectTaskAllocationApp.Services
{
    public interface IEmployeeAvailabilityService
    {
        Task<bool> IsEmployeeAvailableAsync(ApplicationUser employee);
        Task<bool> CanAssignTaskAsync(ApplicationUser employee);
        Task<string> GetAvailabilityStatusAsync(ApplicationUser employee);
        Task<string> GetAvailabilityColorAsync(ApplicationUser employee);
    }
}
