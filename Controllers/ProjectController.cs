using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ProjectTaskAllocationApp.Data;
using ProjectTaskAllocationApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using ProjectTaskAllocationApp.Services;
using System.Linq;
using ProjectTaskAllocationApp.Services.Performance;
using ProjectTaskAllocationApp.Services.Workload;

namespace ProjectTaskAllocationApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        private readonly IPerformanceService _performanceService;
        private readonly IWorkloadService _workloadService;

        public ProjectController(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IPerformanceService performanceService,
            IWorkloadService workloadService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _performanceService = performanceService;
            _workloadService = workloadService;
        }

        [HttpGet]
        [Authorize(Roles = "Manager,TeamLead")]
        public async Task<IActionResult> GetProjects()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (User.IsInRole("Manager"))
            {
                return Ok(await _context.Projects.ToListAsync());
            }
            else if (User.IsInRole("TeamLead"))
            {
                return Ok(await _context.Projects
                    .Where(p => p.TeamLeadId == userId || p.ManagerId == userId)
                    .ToListAsync());
            }

            return Unauthorized();
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateProject(Project project)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Validate employee assignments
                var employees = await _userManager.Users
                    .Where(u => project.EmployeeIds.Contains(u.Id))
                    .ToListAsync();

                if (!employees.Any())
                {
                    return BadRequest("At least one employee must be selected for the project");
                }

                foreach (var employee in employees)
                {
                    if (!_performanceService.IsEmployeeAvailable(employee))
                    {
                        return BadRequest($"Employee {employee.Name} is not available (performance below 40%)");
                    }

                    if (_workloadService.IsOverloaded(employee))
                    {
                        return BadRequest($"Employee {employee.Name} is overloaded (workload 100%)");
                    }
                }

                // Create project
                project.Status = ProjectStatus.Active;
                project.CreatedAt = DateTime.UtcNow;
                
                // Add employee relationships
                foreach (var employee in employees)
                {
                    project.Employees.Add(employee);
                }

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // Send emails
                var teamLead = await _userManager.FindByIdAsync(project.TeamLeadId);
                await _emailService.SendProjectAssignmentToTeamLeadAsync(teamLead, project, employees);

                // Notify employees about project assignment
                foreach (var employee in employees)
                {
                    await _emailService.SendEmailAsync(employee.Email,
                        "New Project Assignment",
                        $"<h3>New Project Assignment</h3>
                        <p>You have been assigned to work on project: {project.Name}</p>
                        <p>Project Description: {project.Description}</p>
                        <p>Start Date: {project.StartDate:yyyy-MM-dd}</p>
                        <p>End Date: {project.EndDate:yyyy-MM-dd}</p>
                        <p>Please log into the system to view your tasks.</p>");
                }

                return Ok(project);
            }
            catch (Exception ex)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Manager,TeamLead,Employee")]
        public async Task<IActionResult> GetProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            return Ok(project);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,TeamLead")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] Project project)
        {
            if (id != project.Id)
                return BadRequest();

            var existingProject = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProject == null)
                return NotFound();

            existingProject.Name = project.Name;
            existingProject.Description = project.Description;
            existingProject.StartDate = project.StartDate;
            existingProject.EndDate = project.EndDate;
            existingProject.Status = project.Status;
            existingProject.TeamLeadId = project.TeamLeadId;

            await _context.SaveChangesAsync();
            return Ok(existingProject);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public IActionResult DeleteProject(int id)
        {
            var project = _context.Projects.FirstOrDefault(p => p.Id == id);
            if (project == null)
                return NotFound();

            _context.Projects.Remove(project);
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost("{projectId}/tasks")]
        [Authorize(Roles = "Manager,TeamLead")]
        public async Task<IActionResult> CreateTask(int projectId, [FromBody] ProjectTaskAllocationApp.Models.ProjectTask task)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                return NotFound();

            task.ProjectId = projectId;
            task.Status = ProjectTaskStatus.ToDo;
            
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Update project statistics
            project.TotalTasks++;
            project.PendingTasks++;
            await _context.SaveChangesAsync();

            // Notify employee about new task
            if (!string.IsNullOrEmpty(task.EmployeeId))
            {
                var employee = await _userManager.FindByIdAsync(task.EmployeeId);
                if (employee != null)
                {
                    await _emailService.SendEmailAsync(employee.Email,
                        "New Task Assigned",
                        $"<h3>New Task Assigned</h3>
                        <p>You have been assigned a new task: {task.Name}</p>
                        <p>Project: {project.Name}</p>
                        <p>Description: {task.Description}</p>
                        <p>Deadline: {task.Deadline?.ToString("yyyy-MM-dd")}</p>
                        <p>Status: {task.Status}</p>");
                }
            }

            return Ok(task);
        }

        [HttpPut("tasks/{taskId}/assign")]
        [Authorize(Roles = "Manager,TeamLead")]
        public async Task<IActionResult> AssignTask(int taskId, [FromBody] TaskAssignmentModel model)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            // Check if employee exists
            var employee = await _userManager.FindByIdAsync(model.EmployeeId);
            if (employee == null)
                return BadRequest("Employee not found");

            // Check employee workload
            if (_workloadService.IsOverloaded(employee))
                return BadRequest("Employee is overloaded");

            // Check employee performance
            if (!_performanceService.IsEmployeeAvailable(employee))
                return BadRequest("Employee performance is below required threshold");

            task.EmployeeId = model.EmployeeId;
            await _context.SaveChangesAsync();

            // Update project statistics
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == task.ProjectId);
            if (project != null)
            {
                project.PendingTasks++;
                await _context.SaveChangesAsync();
            }

            // Notify employee
            await _emailService.SendEmailAsync(employee.Email,
                "Task Assigned",
                $"<h3>Task Assigned</h3>
                <p>You have been assigned a new task: {task.Name}</p>
                <p>Project: {task.Project.Name}</p>
                <p>Description: {task.Description}</p>
                <p>Deadline: {task.Deadline?.ToString("yyyy-MM-dd")}</p>
                <p>Status: {task.Status}</p>");

            return Ok(task);
        }

        [HttpGet("projects/{projectId}/tasks")]
        [Authorize(Roles = "Manager,TeamLead")]
        public async Task<IActionResult> GetProjectTasks(int projectId)
        {
            var tasks = await _context.Tasks
                .Where(t => t.ProjectId == projectId)
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .OrderBy(t => t.Priority)
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpDelete("tasks/{taskId}")]
        [Authorize(Roles = "Manager,TeamLead")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            // Update project statistics
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == task.ProjectId);
            if (project != null)
            {
                project.TotalTasks--;
                switch (task.Status)
                {
                    case ProjectTaskStatus.Done:
                        project.CompletedTasks--;
                        break;
                    case ProjectTaskStatus.InProgress:
                        project.InProgressTasks--;
                        break;
                    case ProjectTaskStatus.ToDo:
                        project.PendingTasks--;
                        break;
                }
                await _context.SaveChangesAsync();
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok();
        }



        [HttpPost("tasks/{taskId}/start")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> StartTask(int taskId)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            if (task.Status != ProjectTaskStatus.ToDo)
                return BadRequest("Task cannot be started - it's not in To-Do status");

            await _taskService.StartTaskAsync(task, User.Identity.Name);
            return Ok(task);
        }

        [HttpPost("tasks/{taskId}/complete")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CompleteTask(int taskId)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                return NotFound();

            if (task.Status != ProjectTaskStatus.InProgress)
                return BadRequest("Task cannot be marked complete - it's not in In-Progress status");

            await _taskService.CompleteTaskAsync(task, User.Identity.Name);
            return Ok(task);
        }

        [HttpGet("projects/{projectId}/tasks")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetProjectTasks(int projectId)
        {
            var tasks = await _context.ProjectTasks
                .Where(t => t.ProjectId == projectId)
                .Include(t => t.Employee)
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpPut("tasks/{taskId}")]
        [Authorize(Roles = "Manager,TeamLead,Employee")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] ProjectTaskAllocationApp.Models.ProjectTask task)
        {
            var existingTask = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (existingTask == null)
                return NotFound();

            var oldStatus = existingTask.Status;
            
            existingTask.Name = task.Name;
            existingTask.Description = task.Description;
            existingTask.Status = task.Status;
            existingTask.EmployeeId = task.EmployeeId;
            existingTask.Deadline = task.Deadline;

            await _context.SaveChangesAsync();

            // Notify about status change
            if (oldStatus != task.Status && !string.IsNullOrEmpty(existingTask.EmployeeId))
            {
                var employee = await _userManager.FindByIdAsync(existingTask.EmployeeId);
                if (employee != null)
                {
                    await _emailService.SendEmailAsync(employee.Email,
                        "Task Status Updated",
                        $"<h3>Task Status Updated</h3>
                        <p>Your task status has been updated to: {task.Status}</p>
                        <p>Task: {task.Name}</p>
                        <p>Previous Status: {oldStatus}</p>
                        <p>New Status: {task.Status}</p>");
                }
            }

            return Ok(existingTask);
        }
    }
}
