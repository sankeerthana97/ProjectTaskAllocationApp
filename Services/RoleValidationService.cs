using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ProjectTaskAllocationApp.Services
{
    public class RoleValidationService : IRoleValidationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RoleValidationService> _logger;

        public RoleValidationService(UserManager<ApplicationUser> userManager, ILogger<RoleValidationService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> CanCreateManagerAsync(ApplicationUser user)
        {
            try
            {
                // Check if there's already a manager
                var existingManager = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Role == "Manager");

                if (existingManager != null)
                {
                    _logger.LogInformation("Cannot create new manager - existing manager found: {ManagerId}", existingManager.Id);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking manager creation");
                throw;
            }
        }

        public async Task<bool> CanCreateTeamLeadAsync(ApplicationUser user)
        {
            try
            {
                // Check if there's already a team lead
                var existingTeamLead = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Role == "TeamLead");

                if (existingTeamLead != null)
                {
                    _logger.LogInformation("Cannot create new team lead - existing team lead found: {TeamLeadId}", existingTeamLead.Id);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking team lead creation");
                throw;
            }
        }

        public async Task<string> GetExistingManagerIdAsync()
        {
            try
            {
                var manager = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Role == "Manager");

                return manager?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting existing manager");
                throw;
            }
        }

        public async Task<string> GetExistingTeamLeadIdAsync()
        {
            try
            {
                var teamLead = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Role == "TeamLead");

                return teamLead?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting existing team lead");
                throw;
            }
        }
    }
}
