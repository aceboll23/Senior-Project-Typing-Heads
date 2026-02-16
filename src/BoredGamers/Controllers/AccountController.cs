using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;

namespace BoredGamers.Controllers;

public class AccountController : Controller
{

    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

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
}