using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectTaskAllocationApp.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProjectTaskAllocationApp.Services;

namespace ProjectTaskAllocationApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IRoleValidationService _roleValidationService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            IRoleValidationService roleValidationService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _roleValidationService = roleValidationService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, false, false);
            
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (await _userManager.IsInRoleAsync(user, "Manager"))
                {
                    return RedirectToAction("Index", "Manager");
                }
                else if (await _userManager.IsInRoleAsync(user, "TeamLead"))
                {
                    return RedirectToAction("Index", "TeamLead");
                }
                else if (await _userManager.IsInRoleAsync(user, "Employee"))
                {
                    return RedirectToAction("Index", "Employee");
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(ApplicationUser user, string password, string role)
        {
            if (ModelState.IsValid)
            {
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, password);
                
                var result = await _userManager.CreateAsync(user);
                
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, role);
                    await _signInManager.SignInAsync(user, false);
                    
                    if (role == "Manager")
                    {
                        return RedirectToAction("Index", "Manager");
                    }
                    else if (role == "TeamLead")
                    {
                        return RedirectToAction("Index", "TeamLead");
                    }
                    else if (role == "Employee")
                    {
                        return RedirectToAction("Index", "Employee");
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
