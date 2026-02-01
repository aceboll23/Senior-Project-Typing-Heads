using Microsoft.EntityFrameworkCore;
using BoredGamers.Models;

namespace BoredGamers.Data;

/*
 * ApplicationDbContext
 * --------------------
 * This class is the central EF Core database context for the BoredGamers app.
 *
 * Responsibilities:
 *   - Defines which models map to database tables
 *   - Manages database connections via EF Core
 *   - Serves as the primary data access entry point for the application
 *
 * IMPORTANT TEAM NOTE:
 *   - Schema changes should be made by updating models and creating EF migrations
 *   - Do NOT manually edit the database schema in Azure
 *   - One designated DB owner will apply migrations to the shared dev database
 */
public class ApplicationDbContext : DbContext
{
    /*
     * DbContextOptions are provided by dependency injection.
     * The connection string is configured in Program.cs using appsettings.json
     */
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

  

}
