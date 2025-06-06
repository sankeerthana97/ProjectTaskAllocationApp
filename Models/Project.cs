using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectTaskAllocationApp.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Requirements { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public ProjectCategory Category { get; set; }

        [Required]
        public ProjectStatus Status { get; set; }

        public string ManagerId { get; set; }

        public string TeamLeadId { get; set; }

        [ForeignKey("ManagerId")]
        public ApplicationUser Manager { get; set; }

        [ForeignKey("TeamLeadId")]
        public virtual ApplicationUser TeamLead { get; set; }

        public virtual ICollection<ProjectTaskAllocationApp.Models.ProjectTask> Tasks { get; set; }

        // Task statistics
        public int TotalTasks => Tasks?.Count ?? 0;
        public int CompletedTasks => Tasks?.Count(t => t.Status == ProjectTaskStatus.Done) ?? 0;
        public int InProgressTasks => Tasks?.Count(t => t.Status == ProjectTaskStatus.InProgress) ?? 0;
        public int PendingTasks => Tasks?.Count(t => t.Status == ProjectTaskStatus.ToDo) ?? 0;

        // Project progress calculation
        public int Progress => TotalTasks > 0 ? (CompletedTasks * 100) / TotalTasks : 0;
    }

    public enum ProjectStatus
    {
        Active,
        NotStarted,
        InProgress,
        Completed,
        OnHold,
        Cancelled
    }

    public enum ProjectCategory
    {
        Critical,
        Major,
        Medium,
        Minor,
        Low
    }
}
