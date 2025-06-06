using ProjectTaskAllocationApp.Models;

namespace ProjectTaskAllocationApp.Services
{
    public interface IPerformanceService
    {
        void UpdatePerformance(ApplicationUser user, bool taskAccepted);
        bool IsEmployeeAvailable(ApplicationUser user);
        bool ShouldRecoverPerformance(ApplicationUser user);
        void RecoverPerformance(ApplicationUser user);
    }
}