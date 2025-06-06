using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProjectTaskAllocationApp.Models
{
    public class ProjectTask
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        public DateTime? Deadline { get; set; }
        public ProjectTaskStatus Status { get; set; }
        public string EmployeeId { get; set; }

        // Added missing properties
        public string AssignedToId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        [ForeignKey("EmployeeId")]
        public ApplicationUser Employee { get; set; }

        // Added missing navigation property
        [ForeignKey("AssignedToId")]
        public ApplicationUser AssignedTo { get; set; }

        [Required]
        public string ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project Project { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewComments { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateTime? DueDate { get; set; }
        public ICollection<TaskHistory> History { get; set; }

        public bool IsOverdue => Status != ProjectTaskStatus.Done &&
                               Status != ProjectTaskStatus.Rejected &&
                               (DueDate.HasValue && DueDate.Value < DateTime.Now);

        public string PriorityLevel => Priority switch
        {
            TaskPriority.Critical => "Critical",
            TaskPriority.Major => "Major",
            TaskPriority.Medium => "Medium",
            TaskPriority.Minor => "Minor",
            TaskPriority.Low => "Low",
            _ => "Normal"
        };

        public string PriorityColor => Priority switch
        {
            TaskPriority.Critical => "#FF0000", // Red
            TaskPriority.Major => "#FFA500",    // Orange
            TaskPriority.Medium => "#FFFF00",   // Yellow
            TaskPriority.Minor => "#00FF00",    // Green
            TaskPriority.Low => "#00FFFF",      // Cyan
            _ => "#FFFFFF"                      // White
        };

        public bool CanStart()
        {
            return Status == ProjectTaskStatus.ToDo;
        }

        public bool CanMarkComplete()
        {
            return Status == ProjectTaskStatus.InProgress;
        }

        public bool CanReview()
        {
            return Status == ProjectTaskStatus.Review;
        }

        public bool CanReject()
        {
            return Status == ProjectTaskStatus.Review;
        }
    }

    public enum ProjectTaskStatus
    {
        ToDo,
        InProgress,
        Review,
        Done,
        Rejected
    }

    public enum TaskPriority
    {
        Critical,
        Major,
        Medium,
        Minor,
        Low
    }
}