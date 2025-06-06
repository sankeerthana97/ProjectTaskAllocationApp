using System.ComponentModel.DataAnnotations;
namespace ProjectTaskAllocationApp.Models
{
    public class TaskAssignmentModel
    {
        [Required]
        public string EmployeeId { get; set; } = string.Empty;
        // Optional: Add additional properties for future use
        public string? Notes { get; set; }
        public DateTime? AssignedDate { get; set; } = DateTime.UtcNow;
        public string? AssignedBy { get; set; }
    }
}