using Microsoft.Extensions.Configuration;
using ProjectTaskAllocationApp.Models;
using System.Threading.Tasks;

namespace ProjectTaskAllocationApp.Services
{
    public class PerformanceService
    {
        private readonly IConfiguration _configuration;
        private const int MAX_PERFORMANCE = 100;
        private const int MIN_PERFORMANCE = 40;
        private const int PERFORMANCE_RECOVERY_THRESHOLD = 2;
        private const int PERFORMANCE_DECREASE_AMOUNT = 5;
        private const int PERFORMANCE_RECOVERY_AMOUNT = 5;

        public PerformanceService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void UpdatePerformance(ApplicationUser user, bool taskAccepted)
        {
            if (taskAccepted)
            {
                user.Performance = Math.Min(MAX_PERFORMANCE, 
                    user.Performance + PERFORMANCE_RECOVERY_AMOUNT);
            }
            else
            {
                user.Performance = Math.Max(MIN_PERFORMANCE, 
                    user.Performance - PERFORMANCE_DECREASE_AMOUNT);
            }
        }

        public bool IsEmployeeAvailable(ApplicationUser user)
        {
            return user.Performance >= MIN_PERFORMANCE;
        }

        public bool ShouldRecoverPerformance(ApplicationUser user)
        {
            return user.Performance < MAX_PERFORMANCE &&
                   user.Performance >= MIN_PERFORMANCE;
        }

        public void RecoverPerformance(ApplicationUser user)
        {
            user.Performance = Math.Min(MAX_PERFORMANCE, 
                user.Performance + PERFORMANCE_RECOVERY_AMOUNT);
        }
    }
}
