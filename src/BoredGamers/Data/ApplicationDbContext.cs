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
    public DbSet<UserGameCollection> UserGameCollections { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<DirectMessage> DirectMessages { get; set; }
    public DbSet<Playgroup> Playgroups { get; set; }
    public DbSet<PlaygroupMember> PlaygroupMembers { get; set; }
    public DbSet<GameNightEvent> GameNightEvents { get; set; }
    public DbSet<GameNightEventGame> GameNightEventGames { get; set; } 
    public DbSet<PlaygroupInvite> PlaygroupInvites { get; set; }
    public DbSet<GameVote> GameVotes { get; set; }

    public DbSet<EventResponse> EventResponses { get; set; }
    public DbSet<ProfilePost> ProfilePosts { get; set; }
    public DbSet<PlaygroupMessage> PlaygroupMessages { get; set; }
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
        //User game colleciton
        modelBuilder.Entity<Game>(entity =>
        {
            //Prevent duplicate games across syncs
            entity.HasIndex(g => g.BggGameId)
                .IsUnique();

            entity.Property(g => g.AverageRating)
                .HasPrecision(4, 2);
        });

        modelBuilder.Entity<UserGameCollection>(entity =>
        {
            // Prevent duplicate adds for the same user + same game
            entity.HasIndex(x => new { x.UserId, x.GameId })
                .IsUnique();

            entity.Property(x => x.DateAdded)
                .HasDefaultValueSql("GETUTCDATE()");
        });
        //User Reviews
        modelBuilder.Entity<Review>(entity =>
        {
            //Prevent duplicate reviews for same user + same game
            entity.HasIndex(r => new { r.UserId, r.GameId })
                .IsUnique();

            entity.Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });
        

        //Configure DirectMessage
        modelBuilder.Entity<DirectMessage>(entity =>
        {
            entity.HasOne(m => m.SenderProfile)
                .WithMany(p => p.SentMessages)
                .HasForeignKey(m => m.SenderProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.RecipientProfile)
                .WithMany(p => p.ReceivedMessages)
                .HasForeignKey(m => m.RecipientProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // Fast lookup for conversation between two users
            entity.HasIndex(m => new { m.SenderProfileId, m.RecipientProfileId });

            // Fast lookup for unread messages
            entity.HasIndex(m => new { m.RecipientProfileId, m.Status });

            entity.Property(m => m.SentAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

                // Configure Playgroup
        modelBuilder.Entity<Playgroup>(entity =>
        {
            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            entity.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure PlaygroupMember
        modelBuilder.Entity<PlaygroupMember>(entity =>
        {
            entity.HasOne(m => m.Playgroup)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.PlaygroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => new { m.PlaygroupId, m.UserId })
                .IsUnique();

            entity.Property(m => m.JoinedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure PlaygroupInvite
        modelBuilder.Entity<PlaygroupInvite>(entity =>
        {
            entity.HasOne(i => i.Playgroup)
                .WithMany()
                .HasForeignKey(i => i.PlaygroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate pending invites
            entity.HasIndex(i => new { i.PlaygroupId, i.InvitedUserId })
                .IsUnique();

            entity.Property(i => i.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure GameNightEvent
        modelBuilder.Entity<GameNightEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Playgroup)
                .WithMany(p => p.GameNightEvents)
                .HasForeignKey(e => e.PlaygroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedGameNightEvents)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.PlaygroupId);
            entity.HasIndex(e => e.EventDateTime);
        });

        //Configure GameNightEventGame
        modelBuilder.Entity<GameNightEventGame>(entity =>
        {
            entity.HasKey(eg => eg.Id);
            
            entity.HasOne(eg => eg.GameNightEvent)
                .WithMany(e => e.EventGames)
                .HasForeignKey(eg => eg.GameNightEventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(eg => eg.Game)
                .WithMany()
                .HasForeignKey(eg => eg.GameId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(eg => eg.User)
                .WithMany(u => u.GameNightEventGames)
                .HasForeignKey(eg => eg.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(eg => new { eg.GameNightEventId, eg.GameId })
                .IsUnique();
        });

        // Configure ProfilePost
        modelBuilder.Entity<ProfilePost>(entity =>
        {
            entity.HasOne(p => p.UserProfile)
                .WithMany(up => up.Posts)
                .HasForeignKey(p => p.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => p.UserProfileId);

            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            entity.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure EventResponse
        modelBuilder.Entity<EventResponse>(entity =>
        {
            entity.HasOne(r => r.GameNightEvent)
                .WithMany()
                .HasForeignKey(r => r.GameNightEventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => new { r.GameNightEventId, r.UserId })
                .IsUnique();

            entity.Property(r => r.RespondedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        // Game Night Voting
        modelBuilder.Entity<GameVote>(entity =>
        {
            entity.HasOne(v => v.GameNightEvent)
                .WithMany(e => e.GameVotes)
                .HasForeignKey(v => v.GameNightEventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.GameNightEventGame)
                .WithMany()
                .HasForeignKey(v => v.GameNightEventGameId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One rank per user per game per event
            entity.HasIndex(v => new { v.GameNightEventId, v.GameNightEventGameId, v.UserId })
                .IsUnique();

            entity.Property(v => v.SubmittedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure PlaygroupMessage
        modelBuilder.Entity<PlaygroupMessage>(entity =>
        {
            entity.HasOne(m => m.Playgroup)
                .WithMany(g => g.Messages)
                .HasForeignKey(m => m.PlaygroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.SenderProfile)
                .WithMany(p => p.SentGroupMessages)
                .HasForeignKey(m => m.SenderProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => new { m.PlaygroupId, m.SentAt });

            entity.Property(m => m.SentAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }

}
