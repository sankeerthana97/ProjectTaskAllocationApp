using System.Threading.Tasks;
using ProjectTaskAllocationApp.Models;
using System.Collections.Generic;

namespace ProjectTaskAllocationApp.Services
{
    public interface IEmailService
    {
        
        Task SendProjectAssignmentToTeamLeadAsync(ApplicationUser teamLead, Project project, List<ApplicationUser> employees);
        Task SendTaskAssignmentEmailAsync(ApplicationUser employee, ProjectTask task);
        Task SendTaskReviewEmailAsync(ApplicationUser manager, ProjectTask task);
        Task SendTaskRejectedEmailAsync(ApplicationUser employee, ProjectTask task, string reason);
        Task SendProjectAssignmentEmailAsync(ApplicationUser employee, Project project);

    }
}
