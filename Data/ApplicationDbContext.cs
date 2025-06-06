using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectTaskAllocationApp.Models;

namespace ProjectTaskAllocationApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTask> Tasks { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; } // Alias for compatibility
        public DbSet<ProjectTimeline> ProjectTimelines { get; set; }
        public DbSet<ProjectTimeline> ProjectTimeline { get; set; } // Alias for compatibility
        public DbSet<TaskHistory> TaskHistories { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure default roles
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "Manager", NormalizedName = "MANAGER" },
                new IdentityRole { Id = "2", Name = "TeamLead", NormalizedName = "TEAMLEAD" },
                new IdentityRole { Id = "3", Name = "Employee", NormalizedName = "EMPLOYEE" }
            );

            // Configure Project relationships
            builder.Entity<Project>()
                .HasOne(p => p.Manager)
                .WithMany(u => u.ManagedProjects)
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasOne(p => p.TeamLead)
                .WithMany(u => u.TeamLeadProjects)
                .HasForeignKey(p => p.TeamLeadId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasMany(p => p.Tasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ProjectTask relationships
            builder.Entity<ProjectTask>()
                .HasOne(t => t.Employee)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectTask>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectTask>()
                .HasMany(t => t.History)
                .WithOne(h => h.Task)
                .HasForeignKey(h => h.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ProjectTimeline relationships
            builder.Entity<ProjectTimeline>()
                .HasOne(pt => pt.Project)
                .WithMany()
                .HasForeignKey(pt => pt.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectTimeline>()
                .HasOne(pt => pt.User)
                .WithMany()
                .HasForeignKey(pt => pt.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure TaskHistory relationships
            builder.Entity<TaskHistory>()
                .HasOne(th => th.Task)
                .WithMany(t => t.History)
                .HasForeignKey(th => th.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure property constraints and defaults
            builder.Entity<ApplicationUser>()
                .Property(u => u.Performance)
                .HasDefaultValue(100.0);

            builder.Entity<ApplicationUser>()
                .Property(u => u.Workload)
                .HasDefaultValue(0.0);

            builder.Entity<ApplicationUser>()
                .Property(u => u.IsAvailable)
                .HasDefaultValue(true);

            builder.Entity<ProjectTask>()
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Entity<ProjectTask>()
                .Property(t => t.Priority)
                .HasDefaultValue(TaskPriority.Medium);

            builder.Entity<ProjectTask>()
                .Property(t => t.Status)
                .HasDefaultValue(ProjectTaskStatus.ToDo);

            builder.Entity<Project>()
                .Property(p => p.Status)
                .HasDefaultValue(ProjectStatus.NotStarted);

            // Configure indexes for better performance
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.Role);

            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.IsAvailable);

            builder.Entity<Project>()
                .HasIndex(p => p.Status);

            builder.Entity<Project>()
                .HasIndex(p => p.Category);

            builder.Entity<ProjectTask>()
                .HasIndex(t => t.Status);

            builder.Entity<ProjectTask>()
                .HasIndex(t => t.Priority);

            builder.Entity<ProjectTask>()
                .HasIndex(t => t.DueDate);

            builder.Entity<ProjectTimeline>()
                .HasIndex(pt => pt.Date);

            builder.Entity<TaskHistory>()
                .HasIndex(th => th.ChangedAt);

            // Configure string lengths for better database optimization
            builder.Entity<ApplicationUser>()
                .Property(u => u.Name)
                .HasMaxLength(100);

            builder.Entity<ApplicationUser>()
                .Property(u => u.Role)
                .HasMaxLength(50);

            builder.Entity<ApplicationUser>()
                .Property(u => u.Skills)
                .HasMaxLength(2000); // JSON array of skills

            builder.Entity<ProjectTimeline>()
                .Property(pt => pt.Event)
                .HasMaxLength(100);

            builder.Entity<ProjectTimeline>()
                .Property(pt => pt.Description)
                .HasMaxLength(500);

            builder.Entity<ProjectTimeline>()
                .Property(pt => pt.Color)
                .HasMaxLength(7); // Hex color codes

            builder.Entity<TaskHistory>()
                .Property(th => th.Comments)
                .HasMaxLength(1000);

            builder.Entity<TaskHistory>()
                .Property(th => th.Reason)
                .HasMaxLength(500);

            builder.Entity<TaskHistory>()
                .Property(th => th.ChangedBy)
                .HasMaxLength(256); // Match AspNetUsers.Id length

            // Configure decimal precision for performance metrics
            builder.Entity<ApplicationUser>()
                .Property(u => u.Performance)
                .HasPrecision(5, 2);

            builder.Entity<ApplicationUser>()
                .Property(u => u.Workload)
                .HasPrecision(5, 2);
        }
    }
}