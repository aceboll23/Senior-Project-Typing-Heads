using BoredGamers.Data;
using BoredGamers.Services.Bgg;
using BoredGamers.Services.Games;
using BoredGamers.Services.Email;
using BoredGamers.Services.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BoredGamers.Models;
using BoredGamers.Services;

var builder = WebApplication.CreateBuilder(args);
//
//Framework Services
//

// Add services to the container.
builder.Services.AddControllersWithViews();
// Identity UI uses Razor Pages
builder.Services.AddRazorPages();

//
//Database
//

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

/*
 * Register ApplicationDbContext with dependency injection.
 * The connection string is read from appsettings.json (or user-secrets).
 *
 * IMPORTANT:
 *  - Local development can use SQL Server LocalDB
 *  - Azure SQL will replace this connection string later
 */
// Register BGG client for Top games sync (HTTP-based)

//
//Identity
//

// ASP.NET Core Identity (uses EF Core + ApplicationDbContext)
builder.Services
    .AddDefaultIdentity<User>(options =>  // Changed from IdentityUser to User
    {
        options.SignIn.RequireConfirmedAccount = false;
        
        // Configure password requirements
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();
        
//
//HTTP Clients
//

builder.Services.AddHttpClient<IBggClient, BggClient>();

//
//Application Services
//

builder.Services.AddScoped<IUserCollectionService, UserCollectionService>();
builder.Services.AddScoped<ReviewService>();

//Register GameService
builder.Services.AddScoped<IGameService, GameService>();
//Sync Service that imports/upserts BGG games into our local database
builder.Services.AddScoped<IGameSyncService, GameSyncService>();

//Email service
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "conversation",
    pattern: "Messages/Conversation/{username}",
    defaults: new { controller = "Messages", action = "Conversation" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map Identity endpoints (Razor Pages)
app.MapRazorPages();

// registers a custom url route
app.MapControllerRoute(
    name: "profile",
    pattern: "Profile/{username}",
    defaults: new { controller = "Profile", action = "Index" });



if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/backfill-bgg-voters", async (IGameSyncService sync, IConfiguration config, string? key, CancellationToken ct) =>
    {
        //Read expected key from configuration
        var expectedKey = config["DevBackfillKey"];

        //If a key is configured and it doesn't match, reject
        if (!string.IsNullOrWhiteSpace(expectedKey) && key != expectedKey)
            return Results.Unauthorized();

        var updated = await sync.BackfillBggNumVotersAsync(ct);
        return Results.Ok(new { Updated = updated });
    });
}
app.Run();