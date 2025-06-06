using Microsoft.EntityFrameworkCore;
using ProjectTaskAllocationApp.Data;
using ProjectTaskAllocationApp.Models;
using System.Threading.Tasks;

namespace ProjectTaskAllocationApp.Services
{
    public class WorkloadService
    {
        private readonly ApplicationDbContext _context;
        private const int MAX_WORKLOAD = 100;
        private const int TASKS_PER_WORKLOAD_UNIT = 10;

        public WorkloadService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CalculateWorkload(ApplicationUser user)
        {
            var activeTasks = await _context.ProjectTasks
                .Where(t => t.EmployeeId == user.Id &&
                           (t.Status == ProjectTaskStatus.ToDo ||
                            t.Status == ProjectTaskStatus.InProgress ||
                            t.Status == ProjectTaskStatus.Review))
                .CountAsync();

            return (activeTasks * 100) / TASKS_PER_WORKLOAD_UNIT;
        }

        public bool IsOverloaded(ApplicationUser user)
        {
            return user.Workload >= MAX_WORKLOAD;
        }

        public async Task UpdateWorkload(ApplicationUser user)
        {
            user.Workload = await CalculateWorkload(user);
        }
    }
}
