using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BoredGamers.Models;

namespace BoredGamers.Controllers;

public class AccountController : Controller
{
    
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    
}