using System.Threading.Tasks;

namespace ProjectTaskAllocationApp.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}
