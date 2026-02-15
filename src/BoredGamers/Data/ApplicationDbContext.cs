using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BoredGamers.Models;

namespace BoredGamers.Data;

/*
 * ApplicationDbContext
 * --------------------
 * Inherits from IdentityDbContext so ASP.NET Core Identity can create/manage
 * auth tables (AspNetUsers, AspNetRoles, etc.).
 *
 * Add your domain DbSets (Games, Groups, Events, etc.) here in future sprints.
 */
public class ApplicationDbContext : IdentityDbContext
{

    //Domain-level user profile table.
    //Authentication and registration login are handled sepearately by Identity.
    // Removed the Users DbSet - Identity manages this automatically

    //Locally cached board games sourced from BGG for fast homepage loading.
    public DbSet<Game> Games { get; set; }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //IMPORTANT: Always call base first so Identity can configure its tables corrrectly.
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            // Username uniqueness is already handled by Identity
            // Email uniqueness is already handled by Identity
            
            //Automatically set timestamps (UTC)
            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(u => u.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<Game>(entity =>
        {
            //Prevent duplicate games across syncs
            entity.HasIndex(g => g.BggGameId)
                .IsUnique();

            //Fast ordering for Top lists
            entity.HasIndex(g => g.BggRank);

            //Ensure rank is positive when present
            entity.Property(g => g.BggRank)
                .IsRequired();

            entity.Property(g => g.AverageRating)
                .HasPrecision(4, 2);
        });

    }
}
