using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ProjectTaskAllocationApp.Models;

namespace ProjectTaskAllocationApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IEmailConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendProjectAssignmentEmailAsync(ApplicationUser employee, Project project)
        {
            var subject = $"You've Been Assigned to Project: {project.Name}";
            var body = $"Dear {employee.Name},<br/><br/>\n            You have been assigned to work on: {project.Name}<br/>\n            Project Description: {project.Description}<br/>\n            Start Date: {project.StartDate.ToString(\"yyyy-MM-dd\")}<br/>\n            Deadline: {project.Deadline.ToString(\"yyyy-MM-dd\")}<br/><br/>\n            Please log into the system to see your tasks.<br/><br/>\n            Best regards,<br/>\n            Project Management System";

            await SendEmailAsync(employee.Email, subject, body);
        }

        public async Task SendProjectAssignmentToTeamLeadAsync(ApplicationUser teamLead, Project project, List<ApplicationUser> employees)
        {
            var subject = $"New Project to Manage: {project.Name}";
            var employeeNames = string.Join(", ", employees.Select(e => e.Name));
            var body = $"Dear Team Lead,<br/><br/>\n            A new project has been created: {project.Name}<br/>\n            Assigned Employees: {employeeNames}<br/><br/>\n            Please log in to create and assign tasks.<br/><br/>\n            Best regards,<br/>\n            Project Management System";

            await SendEmailAsync(teamLead.Email, subject, body);
        }

        public async Task SendTaskAssignmentEmailAsync(ApplicationUser employee, ProjectTask task)
        {
            var subject = $"New Task Assigned: {task.Name}";
            var body = $"Dear {employee.Name},<br/><br/>\n            You have been assigned a new task: {task.Name}<br/>\n            Project: {task.Project.Name}<br/>\n            Priority: {task.Priority}<br/><br/>\n            Please log in to start working.<br/><br/>\n            Best regards,<br/>\n            Project Management System";

            await SendEmailAsync(employee.Email, subject, body);
        }

        public async Task SendTaskReviewEmailAsync(ApplicationUser manager, ProjectTask task)
        {
            var subject = $"Task Ready for Review: {task.Name}";
            var body = $"Dear Manager,<br/><br/>\n            {task.Employee.Name} has completed task: {task.Name}<br/>\n            Project: {task.Project.Name}<br/><br/>\n            Please log in to review and approve/reject.<br/><br/>\n            Best regards,<br/>\n            Project Management System";

            await SendEmailAsync(manager.Email, subject, body);
        }

        public async Task SendTaskRejectedEmailAsync(ApplicationUser employee, ProjectTask task, string reason)
        {
            var subject = $"Task Rejected: {task.Name}";
            var body = $"Dear {employee.Name},<br/><br/>\n            Your task has been rejected: {task.Name}<br/>\n            Reason: {reason}<br/><br/>\n            Please make corrections and resubmit.<br/><br/>\n            Best regards,<br/>\n            Project Management System";

            await SendEmailAsync(employee.Email, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient(_config.SmtpServer)
                {
                    Port = _config.SmtpPort,
                    Credentials = new NetworkCredential(_config.SmtpUsername, _config.SmtpPassword),
                    EnableSsl = _config.SmtpEnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_config.SmtpUsername),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                throw;
            }
        }
    }
}
