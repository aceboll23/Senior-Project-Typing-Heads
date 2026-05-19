using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;

namespace BoredGamers.Middleware;

public class BannedUserMiddleware
{
    private readonly RequestDelegate _next;

    public BannedUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<User> userManager, SignInManager<User> signInManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user != null && user.IsBanned)
            {
                await signInManager.SignOutAsync();
                context.Response.Redirect("/Account/Login?banned=true");
                return;
            }
        }

        await _next(context);
    }
}
