using BoredGamers.Data;
using BoredGamers.Services.Bgg;
using BoredGamers.Services.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

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
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        // Sprint 0: keep simple; tighten policy later if needed
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

//
//HTTP Clients
//

builder.Services.AddHttpClient<IBggClient, BggClient>();

//
//Application Services
//

//Register GameService
builder.Services.AddScoped<IGameService, GameService>();
//Sync Service that imports/upserts BGG games into our local database
builder.Services.AddScoped<IGameSyncService, GameSyncService>();

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
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map Identity endpoints (Razor Pages)
app.MapRazorPages();

app.Run();