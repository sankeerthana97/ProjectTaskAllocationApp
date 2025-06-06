using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ProjectTaskAllocationApp.Models;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using ProjectTaskAllocationApp.Data;
using Microsoft.AspNetCore.Authorization;

namespace ProjectTaskAllocationApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                var role = await _userManager.GetRolesAsync(user);

                var token = GenerateJwtToken(user, role.FirstOrDefault());
                var redirectUrl = GetRedirectUrl(role.FirstOrDefault());

                return Ok(new { token, redirectUrl });
            }

            return Unauthorized(new { message = "Invalid credentials" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                Skills = model.Skills,
                Experience = model.Experience
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                
                var token = GenerateJwtToken(user, model.Role);
                var redirectUrl = GetRedirectUrl(model.Role);

                return Ok(new { token, redirectUrl });
            }

            return BadRequest(new { message = "Registration failed", errors = result.Errors });
        }

        private string GenerateJwtToken(ApplicationUser user, string role)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, role),
                new Claim("name", user.Name)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:ExpireDays"]));

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GetRedirectUrl(string role)
        {
            switch (role)
            {
                case "Manager":
                    return "/manager/dashboard";
                case "TeamLead":
                    return "/teamlead/dashboard";
                case "Employee":
                    return "/employee/dashboard";
                default:
                    return "/login";
            }
        }
    }

    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Skills { get; set; }
        public int Experience { get; set; }
        public string Role { get; set; }
    }
}
