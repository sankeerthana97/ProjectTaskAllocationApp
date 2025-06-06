using Microsoft.AspNetCore.Identity;
using ProjectTaskAllocationApp.Models;
using System.Security.Claims;
using System.Text.Json;

namespace ProjectTaskAllocationApp.Data
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DataSeeder(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            // Seed roles
            await SeedRolesAsync();
            
            // Seed users
            await SeedUsersAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roles = new[] { "Manager", "TeamLead", "Employee" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private async Task SeedUsersAsync()
        {
            var defaultUsers = new[]
            {
                new
                {
                    Email = "manager@example.com",
                    Password = "Manager@123",
                    Name = "John Manager",
                    Skills = "Project Management, Leadership",
                    Experience = 10,
                    Role = "Manager"
                },
                new
                {
                    Email = "teamlead@example.com",
                    Password = "TeamLead@123",
                    Name = "Sarah TeamLead",
                    Skills = "Technical Leadership, Development",
                    Experience = 7,
                    Role = "TeamLead"
                },
                new
                {
                    Email = "employee@example.com",
                    Password = "Employee@123",
                    Name = "Mike Employee",
                    Skills = "Development, Testing",
                    Experience = 3,
                    Role = "Employee"
                }
            };

            foreach (var user in defaultUsers)
            {
                var existingUser = await _userManager.FindByEmailAsync(user.Email);
                if (existingUser == null)
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = user.Email,
                        Email = user.Email,
                        Name = user.Name,
                        Skills = user.Skills,
                        Experience = user.Experience
                    };

                    var result = await _userManager.CreateAsync(newUser, user.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(newUser, user.Role);
                    }
                }
            }
        }
    }
}
