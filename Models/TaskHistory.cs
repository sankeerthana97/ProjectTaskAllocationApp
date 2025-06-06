using System;
namespace ProjectTaskAllocationApp.Models
{
    public class TaskHistory
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public ProjectTaskStatus Status { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; }
        public string Comments { get; set; }
        public string Reason { get; set; }

        // Added missing properties
        public ProjectTaskStatus PreviousStatus { get; set; }
        public ProjectTaskStatus NewStatus { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ProjectTask Task { get; set; }
    }
}