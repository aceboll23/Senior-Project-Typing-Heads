using Microsoft.AspNetCore.Mvc;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using BoredGamers.Data;
using BoredGamers.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

public class AccountController : Controller
{

    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, ApplicationDbContext db, IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _emailService = emailService;
    }
    /*----- Password reset implementation -----*/
    // GET /Account/ForgotPassword
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // POST /Account/ForgotPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        // Always show the same confirmation message whether or not the email exists (prevents enumeration attacks)
        if (user == null || user.IsBanned || user.IsDeactivated) 
        {
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // Generates a secure random token
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        
        // Store token and expiry on the user
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Builds the reset link
        var resetLink = Url.Action(
            "ResetPassword",
            "Account",
            new { token = token },
            Request.Scheme);

        // Send email
        var subject = "BoredGamers Password Reset";
        var body = $@"
            <p>Hi {user.UserName},</p>
            <p>You requested a password reset for your BoredGamers account.</p>
            <p><a href='{resetLink}'>Click here to reset your password</a></p>
            <p>This link expires in 15 minutes.</p>
            <p>If you did not request this, you can ignore this email.</p>";

        await _emailService.SendEmailAsync(user.Email!, subject, body);

        return RedirectToAction("ForgotPasswordConfirmation");
    }

    // GET /Account/ForgotPasswordConfirmation
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    // GET /Account/ResetPassword?token=xxx
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login");

        // Check if token exists and is not expired
        var user = await _db.Users.OfType<User>()
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token && u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            ViewData["ErrorMessage"] = "This reset link is invalid or has expired.";
            return View("ResetPasswordError");
        }

        var model = new ResetPasswordViewModel { Token = token };
        return View(model);
    }

    // POST /Account/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Find user by token and check expiry
        var user = await _db.Users
            .OfType<User>()
            .FirstOrDefaultAsync(u =>
                u.PasswordResetToken == model.Token &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            ViewData["ErrorMessage"] = "This reset link is invalid or has expired.";
            return View("ResetPasswordError");
        }

        // Reset the password using Identity
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // Invalidate the token so it can only be used once
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return RedirectToAction("ResetPasswordSuccess");
    }

    // GET /Account/ResetPasswordSuccess
    [HttpGet]
    public IActionResult ResetPasswordSuccess()
    {
        return View();
    }

    /*----- Profile registration implementation ----- */
    //GET for /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    //Get user data
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);

        return View(user);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Birthday.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (model.Birthday.Value > today)
                ModelState.AddModelError("Birthday", "Birthday cannot be in the future.");
            else if (model.Birthday.Value > today.AddYears(-10))
                ModelState.AddModelError("Birthday", "You must be at least 10 years old to register.");
        }

        if (ModelState.IsValid)
        {
            //creates user object
            var user = new User
            {
                UserName = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Birthday = model.Birthday,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            //creates user with password
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Create the UserProfile for the new user
                var profile = new UserProfile
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Set<UserProfile>().Add(profile);
                await _db.SaveChangesAsync();

                //signs in user and redirects to homepage
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(model);
    }

    //GET and POST for /Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByNameAsync(model.UsernameOrEmail);

            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(model.UsernameOrEmail);
            }

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    isPersistent: false,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }

    // Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}