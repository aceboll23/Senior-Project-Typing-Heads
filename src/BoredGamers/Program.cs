using BoredGamers.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

/*
 * Register ApplicationDbContext with dependency injection.
 * The connection string is read from appsettings.json (or user-secrets).
 *
 * IMPORTANT:
 *  - Local development can use SQL Server LocalDB
 *  - Azure SQL will replace this connection string later
 */
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity (uses EF Core + ApplicationDbContext)
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        // Sprint 0: keep simple; tighten policy later if needed
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Identity UI uses Razor Pages
builder.Services.AddRazorPages();

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