using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ProjectTaskAllocationApp.Data;
using ProjectTaskAllocationApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using ProjectTaskAllocationApp.Services;
using System.Linq;

namespace ProjectTaskAllocationApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IEmployeeAvailabilityService _availabilityService;
        private readonly ITaskService _taskService;

        public TaskController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IEmployeeAvailabilityService availabilityService,
            ITaskService taskService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _availabilityService = availabilityService;
            _taskService = taskService;
        }

        [HttpGet]
        [Authorize(Roles = "TeamLead,Employee")]
        public async Task<IActionResult> GetTasks()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (User.IsInRole("TeamLead"))
            {
                return Ok(await _context.Tasks
                    .Include(t => t.Project)
                    .Include(t => t.AssignedTo)
                    .Where(t => t.Project.TeamLeadId == userId)
                    .ToListAsync());
            }
            else if (User.IsInRole("Employee"))
            {
                return Ok(await _context.Tasks
                    .Include(t => t.Project)
                    .Include(t => t.AssignedTo)
                    .Where(t => t.AssignedToId == userId)
                    .ToListAsync());
            }

            return Unauthorized();
        }

        [HttpGet("to-review")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetTasksToReview()
        {
            return Ok(await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Where(t => t.Status == TaskStatus.Review)
                .ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "TeamLead")]
        public async Task<IActionResult> CreateTask([FromBody] Task task)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var employee = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == task.AssignedToId);
            if (employee == null)
            {
                return BadRequest("Assigned employee not found");
            }

            if (!_availabilityService.CanAssignTask(employee))
            {
                return BadRequest($"Employee {employee.Name} is overloaded (workload 100%)");
            }

            task.Status = TaskStatus.ToDo;
            task.CreatedAt = DateTime.UtcNow;
            
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Send email notification to employee
            await _emailService.SendTaskAssignedEmail(employee, task);

            return Ok(task);
        }

        [HttpPut("{taskId}/status")]
        [Authorize(Roles = "Employee,Manager")]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, [FromBody] TaskStatusUpdate model)
        {
            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (User.IsInRole("Employee") && task.AssignedToId != userId)
            {
                return Unauthorized();
            }

            if (User.IsInRole("Manager") && task.Project.ManagerId != userId)
            {
                return Unauthorized();
            }

            if (!_taskService.IsValidStatusTransition(task.Status, model.NewStatus))
            {
                return BadRequest($"Invalid status transition from {task.Status} to {model.NewStatus}");
            }

            task.Status = model.NewStatus;
            task.UpdatedAt = DateTime.UtcNow;

            // Create task history entry
            var history = new TaskHistory
            {
                TaskId = task.Id,
                PreviousStatus = task.Status,
                NewStatus = model.NewStatus,
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow,
                Comments = model.Comments
            };

            _context.TaskHistories.Add(history);
            await _context.SaveChangesAsync();

            // Send appropriate email based on status change
            await _taskService.HandleStatusChangeEmails(task, model.NewStatus);

            return Ok(task);
        }

        [HttpGet("{projectId}/assigned-employees")]
        [Authorize(Roles = "TeamLead")]
        public async Task<IActionResult> GetAssignedEmployees(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Employees)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (project.TeamLeadId != userId)
            {
                return Unauthorized();
            }

            return Ok(project.Employees.Select(e => new
            {
                e.Id,
                e.Name,
                e.Performance,
                e.Workload
            }));
        }
    }

    public class TaskStatusUpdate
    {
        public TaskStatus NewStatus { get; set; }
        public string Comments { get; set; }
    }
}
