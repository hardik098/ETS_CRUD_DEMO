using ETS_CRUD_DEMO.Data;
using ETS_CRUD_DEMO.Services.Interfaces;
using ETS_CRUD_DEMO.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ETS_CRUD_DEMO.Controllers
{
    public class AuthController : Controller
    {
        private readonly IVerificationService _verificationService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IVerificationService verificationService,
            ApplicationDbContext context,
            ILogger<AuthController> logger)
        {
            _verificationService = verificationService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Employees");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Email == model.Email && e.IsActive);

            if (employee == null)
            {

                ModelState.AddModelError("", "Email address not found.");
                return View(model);
            }

            var (success, message) = await _verificationService.SendOTPEmail(model.Email);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(model);
            }

            TempData["SuccessMessage"] = "OTP has been sent to your email address.";
            return RedirectToAction("VerifyOTP", new { email = model.Email });
        }

        [HttpGet]
        public IActionResult VerifyOTP(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var model = new OTPVerificationViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(OTPVerificationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, message) = await _verificationService.VerifyOTP(model.Email, model.OTP);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(model);
            }

            var employee = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.Email == model.Email);

            // Create claims for the authenticated user
            var claims = new List<Claim>
              {
                  new Claim(ClaimTypes.Email, employee.Email),
                  new Claim(ClaimTypes.Name, $"{employee.FirstName} {employee.LastName}"),
                  new Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
                  new Claim(ClaimTypes.Role, employee.Role?.RoleName.ToString() ?? "")
              };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Set session data for non-sensitive information
            HttpContext.Session.SetString("UserRole", employee.Role?.RoleName ?? "Employee");

            return RedirectToAction("Index", "Employees");
        }
        [HttpGet]
        public async Task<IActionResult> ResendOTP(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            // Send OTP
            var (success, message) = await _verificationService.SendOTPEmail(email);

            if (success)
            {
                TempData["SuccessMessage"] = "OTP has been resent to your email address.";
            }
            else
            {
                ModelState.AddModelError("", message);
            }

            return RedirectToAction("VerifyOTP", new { email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            /*await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login","Auth");*/

            // Clear the session
            /* HttpContext.Session.Clear();
             return RedirectToAction("Login");*/

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();  // Clear session data on logout
            return RedirectToAction("Login", "Auth");

        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}

