using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ProjectTaskAllocationApp.Data;
using ProjectTaskAllocationApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace ProjectTaskAllocationApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimelineController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TimelineController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("projects/{projectId}")]
        [Authorize(Roles = "Manager,TeamLead,Employee")]
        public async Task<IActionResult> GetProjectTimeline(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Unauthorized();

            var project = await _context.Projects
                .Include(p => p.TeamLead)
                .Include(p => p.Manager)
                .Include(p => p.Employees)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();

            // Check if user has access to this project
            if (!User.IsInRole("Manager") && 
                !User.IsInRole("TeamLead") && 
                !project.Employees.Any(e => e.Id == userId))
            {
                return Unauthorized();
            }

            var timelineEvents = await _context.ProjectTimeline
                .Where(t => t.ProjectId == projectId)
                .OrderBy(t => t.Timestamp)
                .Select(t => new
                {
                    t.Event,
                    t.Description,
                    t.Timestamp,
                    t.Color,
                    t.ChangedBy,
                    t.Comments
                })
                .ToListAsync();

            return Ok(timelineEvents);
        }

        [HttpPost("projects/{projectId}")]
        [Authorize(Roles = "Manager,TeamLead")]
        public async Task<IActionResult> AddTimelineEvent(int projectId, [FromBody] TimelineEventDto eventDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Unauthorized();

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();

            // Check if user has access to this project
            if (!User.IsInRole("Manager") && project.TeamLeadId != userId)
            {
                return Unauthorized();
            }

            var timelineEvent = new ProjectTimeline
            {
                ProjectId = projectId,
                Event = eventDto.Event,
                Description = eventDto.Description,
                Timestamp = DateTime.UtcNow,
                Color = eventDto.Color,
                ChangedBy = userId,
                Comments = eventDto.Comments
            };

            _context.ProjectTimeline.Add(timelineEvent);
            await _context.SaveChangesAsync();

            return Ok(timelineEvent);
        }
    }

    public class TimelineEventDto
    {
        public string Event { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public string Comments { get; set; }
    }
}
