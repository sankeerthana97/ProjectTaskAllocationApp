using Microsoft.EntityFrameworkCore;
using ProjectTaskAllocationApp.Data;
using ProjectTaskAllocationApp.Models;
using System.Threading.Tasks;

namespace ProjectTaskAllocationApp.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPerformanceService _performanceService;
        private readonly IWorkloadService _workloadService;

        public TaskService(ApplicationDbContext context, IEmailService emailService, IPerformanceService performanceService, IWorkloadService workloadService)
        {
            _context = context;
            _emailService = emailService;
            _performanceService = performanceService;
            _workloadService = workloadService;
        }

        private async Task AddTaskHistoryAsync(ProjectTask task, ProjectTaskStatus newStatus, string userId, string comments = null)
        {
            var history = new TaskHistory
            {
                TaskId = task.Id,
                Status = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = userId,
                Comments = comments
            };

            task.History.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task StartTaskAsync(ProjectTask task, string userId)
        {
            if (!task.CanStart())
                throw new InvalidOperationException("Task cannot be started");

            task.Status = ProjectTaskStatus.InProgress;
            task.StartedAt = DateTime.UtcNow;
            await AddTaskHistoryAsync(task, ProjectTaskStatus.InProgress, userId);
            await _context.SaveChangesAsync();
        }

        public async Task CompleteTaskAsync(ProjectTask task, string userId)
        {
            if (!task.CanMarkComplete())
                throw new InvalidOperationException("Task cannot be marked complete");

            task.Status = ProjectTaskStatus.Review;
            task.CompletedAt = DateTime.UtcNow;
            await AddTaskHistoryAsync(task, ProjectTaskStatus.Review, userId);
            await _context.SaveChangesAsync();
            
            // Send review notification to manager
            var manager = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == "Manager" && u.Projects.Any(p => p.Id == task.ProjectId));
        
            if (manager != null)
            {
                await _emailService.SendTaskReviewEmailAsync(manager, task);
            }

            // Update project status if needed
            await UpdateProjectStatusAsync(task.ProjectId);
        }

        public async Task AcceptTaskAsync(ProjectTask task, string userId)
        {
            if (!task.CanReview())
                throw new InvalidOperationException("Task cannot be accepted");

            task.Status = ProjectTaskStatus.Done;
            task.ReviewedAt = DateTime.UtcNow;
            
            // Update employee performance
            var employee = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == task.EmployeeId);

            if (employee != null)
            {
                _performanceService.UpdatePerformance(employee, true);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RejectTaskAsync(ProjectTask task, string userId, string reason)
        {
            if (!task.CanReject())
                throw new InvalidOperationException("Task cannot be rejected");

            task.Status = ProjectTaskStatus.InProgress;
            task.ReviewComments = reason;
            
            // Update employee performance
            var employee = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == task.EmployeeId);

            if (employee != null)
            {
                _performanceService.UpdatePerformance(employee, false);
                await _context.SaveChangesAsync();

                // Send rejection email to employee
                await _emailService.SendEmailAsync(employee.Email,
                    "Task Rejected",
                    $"<h3>Task Rejected</h3>\n                    <p>Your task has been rejected: {task.Name}</p>\n                    <p>Project: {task.Project.Name}</p>\n                    <p>Reason: {reason}</p>\n                    <p>Please make corrections and resubmit.</p>");
            }
        }
    }
}
