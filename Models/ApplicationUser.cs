using Microsoft.AspNetCore.Identity;

namespace ProjectTaskAllocationApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public DateTime? LastLogin { get; set; }
        
        // Employee Profile Properties
        public string Skills { get; set; } // JSON array of skills
        public int Experience { get; set; } // In years
        public double Performance { get; set; } = 100; // Starts at 100%
        public double Workload { get; set; } = 0; // Starts at 0%
        public bool IsAvailable { get; set; } = true; // Based on performance >= 40%
        
        // Navigation Properties
        public virtual ICollection<Project> Projects { get; set; }
        public virtual ICollection<ProjectTask> Tasks { get; set; }
    }
}
