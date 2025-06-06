using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ProjectTaskAllocationApp.Models;

namespace ProjectTaskAllocationApp.Services
{
    public interface IRoleValidationService
    {
        Task<bool> CanCreateManagerAsync(ApplicationUser user);
        Task<bool> CanCreateTeamLeadAsync(ApplicationUser user);
        Task<string> GetExistingManagerIdAsync();
        Task<string> GetExistingTeamLeadIdAsync();
    }
}
