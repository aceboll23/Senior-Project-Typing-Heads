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

        // Configure UserProfile
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasOne(p => p.User)
                    .WithOne(u => u.Profile)
                    .HasForeignKey<UserProfile>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(p => p.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                    
                entity.HasIndex(p => p.UserId)
                    .IsUnique();
            });
            
            // Configure Friendship
            modelBuilder.Entity<Friendship>(entity =>
            {
                // Requester relationship
                entity.HasOne(f => f.RequesterProfile)
                    .WithMany(p => p.SentFriendRequests)
                    .HasForeignKey(f => f.RequesterProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Receiver relationship
                entity.HasOne(f => f.ReceiverProfile)
                    .WithMany(p => p.ReceivedFriendRequests)
                    .HasForeignKey(f => f.ReceiverProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Prevent duplicate friend requests
                entity.HasIndex(f => new { f.RequesterProfileId, f.ReceiverProfileId })
                    .IsUnique();
                
                // Index for querying by status
                entity.HasIndex(f => f.Status);
                
                entity.Property(f => f.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(f => f.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });
            
            // Configure BlockedUser
            modelBuilder.Entity<BlockedUser>(entity =>
            {
                // Blocker relationship
                entity.HasOne(b => b.BlockerProfile)
                    .WithMany(p => p.BlockedUsers)
                    .HasForeignKey(b => b.BlockerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // BlockedUser relationship
                entity.HasOne(b => b.BlockedProfile)
                    .WithMany(p => p.BlockedByUsers)
                    .HasForeignKey(b => b.BlockedProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Prevent duplicate blocks
                entity.HasIndex(b => new { b.BlockerProfileId, b.BlockedProfileId })
                    .IsUnique();
                
                entity.Property(b => b.BlockedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });
            
            // Configure Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.UserProfile)
                    .WithMany(p => p.Notifications)
                    .HasForeignKey(n => n.UserProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Index for querying unread notifications
                entity.HasIndex(n => new { n.UserProfileId, n.IsRead });
                
                // Index for querying by type
                entity.HasIndex(n => n.Type);
                
                entity.Property(n => n.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });
            
            // Configure FriendRequestRateLimit
            modelBuilder.Entity<FriendRequestRateLimit>(entity =>
            {
                entity.HasOne(r => r.UserProfile)
                    .WithMany(p => p.FriendRequestRateLimits)
                    .HasForeignKey(r => r.UserProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Index for efficient rate limit checking
                entity.HasIndex(r => new { r.UserProfileId, r.RequestDate });
                
                entity.Property(r => r.RequestSentAt)
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
                .IsRequired(false);

            entity.ToTable(t => t.HasCheckConstraint("CK_Games_BggRank_Positive", "[BggRank] IS NULL OR [BggRank] > 0"));

            entity.Property(g => g.AverageRating)
                .HasPrecision(4, 2);
        });

    }
}
