using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IEmailService _emailService;

    public SettingsController(ApplicationDbContext db,UserManager<User> userManager,SignInManager<User> signInManager,IEmailService emailService)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
    }

    // GET /Settings
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null){ 
            return Challenge();
        }
        var model = new SettingsViewModel
        {
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty
        };

        // Show pending email notice if they have one awaiting verification
        if (!string.IsNullOrEmpty(user.PendingEmail))
        { 
            ViewData["PendingEmail"] = user.PendingEmail;
        }
        return View(model);
    }

    // POST /Settings
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await _userManager.GetUserAsync(User);
        if (user == null){
             return Challenge();
        }
        // Verify current password before making any changes
        var passwordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
        if (!passwordValid)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
            return View(model);
        }

        var usernameChanged = !string.Equals(user.UserName, model.Username, StringComparison.OrdinalIgnoreCase);
        var emailChanged = !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase);

        // CheckS username uniqueness if changed
        if (usernameChanged)
        {
            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Username), "That username is already taken.");
                return View(model);
            }

            var setUsernameResult = await _userManager.SetUserNameAsync(user, model.Username);
            if (!setUsernameResult.Succeeded)
            {
                foreach (var error in setUsernameResult.Errors){
                    ModelState.AddModelError(nameof(model.Username), error.Description);
                }
                return View(model);
            }
        }

        // Handle email change — store as pending and send verification
        if (emailChanged)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "That email is already in use.");
                return View(model);
            }

            // Store the new email as pending until verified
            var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

            user.PendingEmail = model.Email;
            user.EmailVerificationToken = token;
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Build verification link
            var verifyLink = Url.Action(
                "VerifyEmail",
                "Settings",
                new { token },
                Request.Scheme);

            var subject = "BoredGamers — Verify Your New Email";
            var body = $@"
                <p>Hi {user.UserName},</p>
                <p>You requested to change your email address on BoredGamers.</p>
                <p><a href='{verifyLink}'>Click here to confirm your new email address</a></p>
                <p>This link expires in 24 hours.</p>
                <p>If you did not request this, you can ignore this email. Your current email will remain unchanged.</p>";

            await _emailService.SendEmailAsync(model.Email, subject, body);

            ViewData["PendingEmail"] = model.Email;
            ViewData["SuccessMessage"] = usernameChanged
                ? "Username updated. A verification email has been sent to your new address."
                : "A verification email has been sent to your new address.";

            return View(model);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Refresh the auth cookie so the new username shows in the nav immediately
        await _signInManager.RefreshSignInAsync(user);

        ViewData["SuccessMessage"] = "Your settings have been updated.";
        return View(model);
    }

    // GET /Settings/VerifyEmail?token=xxx
    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Index");

        var user = await _db.Users.OfType<User>()
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token && u.EmailVerificationTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            ViewData["ErrorMessage"] = "This verification link is invalid or has expired.";
            return View("VerifyEmailError");
        }

        // Apply the pending email
        var setEmailResult = await _userManager.SetEmailAsync(user, user.PendingEmail);
        if (!setEmailResult.Succeeded)
        {
            ViewData["ErrorMessage"] = "Could not update your email address. It may already be in use.";
            return View("VerifyEmailError");
        }

        // Clear pending email fields
        user.PendingEmail = null;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _signInManager.RefreshSignInAsync(user);

        return View("VerifyEmailSuccess");
    }
}