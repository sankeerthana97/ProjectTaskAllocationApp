using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ProjectTaskAllocationApp.Data;
using ProjectTaskAllocationApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using ProjectTaskAllocationApp.Services;

namespace ProjectTaskAllocationApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmployeeAvailabilityService _availabilityService;
        private readonly IPerformanceService _performanceService;

        public EmployeeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmployeeAvailabilityService availabilityService,
            IPerformanceService performanceService)
        {
            _context = context;
            _userManager = userManager;
            _availabilityService = availabilityService;
            _performanceService = performanceService;
        }

        [HttpGet("me")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var employee = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (employee == null)
                return NotFound();

            return Ok(new
            {
                employee.Name,
                employee.Skills,
                employee.Experience,
                employee.Bio,
                Performance = employee.Performance,
                Workload = employee.Workload,
                IsAvailable = _availabilityService.IsEmployeeAvailable(employee)
            });
        }

        [HttpGet]
        [Authorize(Roles = "Manager,TeamLead")]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _context.Users
                .Where(u => u.Role == "Employee")
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Skills,
                    e.Experience,
                    e.Performance,
                    e.Workload,
                    IsAvailable = _availabilityService.IsEmployeeAvailable(e)
                })
                .ToListAsync();

            return Ok(employees);
        }

        [HttpGet("available")]
        [Authorize(Roles = "Manager,TeamLead")]
        public async Task<IActionResult> GetAvailableEmployees(
            [FromQuery] string skills,
            [FromQuery] int? experience,
            [FromQuery] int? performance,
            [FromQuery] int? workload)
        {
            var query = _context.Users
                .Where(u => u.Role == "Employee")
                .AsQueryable();

            if (!string.IsNullOrEmpty(skills))
            {
                var skillList = skills.Split(',').Select(s => s.Trim()).ToList();
                query = query.Where(e => e.Skills.Any(s => skillList.Contains(s)));
            }

            if (experience.HasValue)
            {
                query = query.Where(e => e.Experience >= experience.Value);
            }

            if (performance.HasValue)
            {
                query = query.Where(e => e.Performance >= performance.Value);
            }

            if (workload.HasValue)
            {
                query = query.Where(e => e.Workload <= workload.Value);
            }

            var employees = await query
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Skills,
                    e.Experience,
                    e.Performance,
                    e.Workload,
                    IsAvailable = _availabilityService.IsEmployeeAvailable(e)
                })
                .ToListAsync();

            return Ok(employees);
        }

        [HttpPut("me")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> UpdateProfile([FromBody] EmployeeProfileUpdate model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var employee = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (employee == null)
                return NotFound();

            employee.Name = model.Name;
            employee.Skills = model.Skills;
            employee.Experience = model.Experience;
            employee.Bio = model.Bio;

            await _userManager.UpdateAsync(employee);

            return Ok(new
            {
                employee.Name,
                employee.Skills,
                employee.Experience,
                employee.Bio,
                Performance = employee.Performance,
                Workload = employee.Workload,
                IsAvailable = _availabilityService.IsEmployeeAvailable(employee)
            });
        }
    }

    public class EmployeeProfileUpdate
    {
        public string Name { get; set; }
        public string[] Skills { get; set; }
        public int Experience { get; set; }
        public string Bio { get; set; }
    }
}
