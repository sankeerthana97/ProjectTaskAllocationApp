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

        private async Task AddTaskHistoryAsync(ProjectTask task, ProjectTaskStatus newStatus, string userId, string comments = null!)
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

            // Add task history
            await AddTaskHistoryAsync(task, ProjectTaskStatus.Done, userId, "Task accepted and marked as done");
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
            }

            // Add task history
            await AddTaskHistoryAsync(task, ProjectTaskStatus.InProgress, userId, $"Task rejected: {reason}");
        }

        public async Task UpdateProjectStatusAsync(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return;

            var tasks = await _context.Tasks
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();

            var completedTasks = tasks.Count(t => t.Status == ProjectTaskStatus.Done);
            var totalTasks = tasks.Count;

            if (totalTasks > 0)
            {
                var completionPercentage = (double)completedTasks / totalTasks * 100;
                project.CompletionPercentage = (int)completionPercentage;

                if (completionPercentage >= 100)
                {
                    project.Status = ProjectStatus.Completed;
                }
            }

            await _context.SaveChangesAsync();
        }

        public bool IsValidStatusTransition(ProjectTask task, ProjectTaskStatus newStatus)
        {
            switch (task.Status)
            {
                case ProjectTaskStatus.New:
                    return newStatus == ProjectTaskStatus.InProgress;
                case ProjectTaskStatus.InProgress:
                    return newStatus == ProjectTaskStatus.Review || newStatus == ProjectTaskStatus.New;
                case ProjectTaskStatus.Review:
                    return newStatus == ProjectTaskStatus.Done || newStatus == ProjectTaskStatus.InProgress;
                case ProjectTaskStatus.Done:
                    return false;
                default:
                    return false;
            }
        }

        public async Task HandleStatusChangeEmails(ProjectTask task, ProjectTaskStatus oldStatus, ProjectTaskStatus newStatus)
        {
            var project = await _context.Projects.FindAsync(task.ProjectId);
            var assignedTo = await _context.Users.FindAsync(task.AssignedToId);
            var employee = await _context.Users.FindAsync(task.EmployeeId);

            if (project == null || assignedTo == null || employee == null) return;

            if (oldStatus == ProjectTaskStatus.New && newStatus == ProjectTaskStatus.InProgress)
            {
                await _emailService.SendTaskAssignedEmailAsync(assignedTo, task, project);
            }
            else if (oldStatus == ProjectTaskStatus.Review && newStatus == ProjectTaskStatus.Done)
            {
                await _emailService.SendTaskAcceptedEmailAsync(employee, task, project);
            }
            else if (oldStatus == ProjectTaskStatus.Review && newStatus == ProjectTaskStatus.InProgress)
            {
                await _emailService.SendTaskRejectedEmailAsync(employee, task, project);
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

            // Add task history
            await AddTaskHistoryAsync(task, ProjectTaskStatus.InProgress, userId, $"Task rejected: {reason}");
        }
    }
}
