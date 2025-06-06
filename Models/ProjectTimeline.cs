using System;
using System.Collections.Generic;

namespace ProjectTaskAllocationApp.Models
{
    public class ProjectTimeline
    {
        public int Id { get; set; }
        public string ProjectId { get; set; }
        public Project Project { get; set; }
        public DateTime Date { get; set; }
        public string Event { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Color { get; set; }

        public static ProjectTimeline CreateProjectCreatedEvent(Project project, ApplicationUser user)
        {
            return new ProjectTimeline
            {
                ProjectId = project.Id,
                Date = project.CreatedAt,
                Event = "Project Created",
                Description = $"Project '{project.Name}' was created",
                UserId = user.Id,
                Color = "#00FF00" // Green
            };
        }

        public static ProjectTimeline CreateTaskCreatedEvent(ProjectTask task, ApplicationUser user)
        {
            return new ProjectTimeline
            {
                ProjectId = task.ProjectId,
                Date = task.CreatedAt,
                Event = "Task Created",
                Description = $"Task '{task.Name}' was created",
                UserId = user.Id,
                Color = "#0000FF" // Blue
            };
        }

        public static ProjectTimeline CreateTaskStatusChangedEvent(ProjectTask task, ApplicationUser user)
        {
            return new ProjectTimeline
            {
                ProjectId = task.ProjectId,
                Date = DateTime.UtcNow,
                Event = "Task Status Changed",
                Description = $"Task '{task.Name}' status changed to {task.Status}",
                UserId = user.Id,
                Color = task.PriorityColor
            };
        }

        public static ProjectTimeline CreateProjectStatusChangedEvent(Project project, ApplicationUser user)
        {
            return new ProjectTimeline
            {
                ProjectId = project.Id,
                Date = DateTime.UtcNow,
                Event = "Project Status Changed",
                Description = $"Project status changed to {project.Status}",
                UserId = user.Id,
                Color = project.Status switch
                {
                    ProjectStatus.Active => "#00FF00",    // Green
                    ProjectStatus.Completed => "#0000FF", // Blue
                    ProjectStatus.OnHold => "#FFFF00",   // Yellow
                    ProjectStatus.Cancelled => "#FF0000", // Red
                    _ => "#000000"                       // Black
                }
            };
        }
    }
}
