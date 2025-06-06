using ProjectTaskAllocationApp.Models;
using System.Threading.Tasks;

namespace ProjectTaskAllocationApp.Services
{
    public interface ITaskService
    {
        Task StartTaskAsync(ProjectTask task, string userId);
        Task CompleteTaskAsync(ProjectTask task, string userId);
        Task AcceptTaskAsync(ProjectTask task, string userId);
        Task RejectTaskAsync(ProjectTask task, string userId, string reason);
    }
}