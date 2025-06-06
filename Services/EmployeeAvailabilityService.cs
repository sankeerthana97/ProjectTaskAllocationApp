using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectTaskAllocationApp.Models;

namespace ProjectTaskAllocationApp.Services
{
    public class EmployeeAvailabilityService : IEmployeeAvailabilityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWorkloadService _workloadService;

        public EmployeeAvailabilityService(ApplicationDbContext context, IWorkloadService workloadService)
        {
            _context = context;
            _workloadService = workloadService;
        }

        public async Task<bool> IsEmployeeAvailableAsync(ApplicationUser employee)
        {
            return employee.Performance >= 40 && employee.IsAvailable;
        }

        public async Task<bool> CanAssignTaskAsync(ApplicationUser employee)
        {
            // Check if employee is available
            if (!await IsEmployeeAvailableAsync(employee))
                return false;

            // Check workload
            var activeTasks = await _context.Tasks
                .CountAsync(t => t.EmployeeId == employee.Id && 
                                (t.Status == ProjectTaskStatus.ToDo || 
                                 t.Status == ProjectTaskStatus.InProgress || 
                                 t.Status == ProjectTaskStatus.Review));

            // Check if adding one more task would overload the employee
            return activeTasks < 10;
        }

        public async Task<string> GetAvailabilityStatusAsync(ApplicationUser employee)
        {
            if (!employee.IsAvailable)
                return "Not Available";

            if (employee.Performance < 40)
                return "Low Performance";

            var activeTasks = await _context.Tasks
                .CountAsync(t => t.EmployeeId == employee.Id && 
                                (t.Status == ProjectTaskStatus.ToDo || 
                                 t.Status == ProjectTaskStatus.InProgress || 
                                 t.Status == ProjectTaskStatus.Review));

            if (activeTasks >= 10)
                return "Overloaded";

            return "Available";
        }

        public async Task<string> GetAvailabilityColorAsync(ApplicationUser employee)
        {
            var status = await GetAvailabilityStatusAsync(employee);

            return status switch
            {
                "Not Available" => "#FF0000",    // Red
                "Low Performance" => "#FFA500",  // Orange
                "Overloaded" => "#FFFF00",       // Yellow
                _ => "#00FF00"                   // Green
            };
        }
    }
}
