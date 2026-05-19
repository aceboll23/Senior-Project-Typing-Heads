using System;
using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace BoredGamers.Tests.Playgroups;

[TestFixture]
public class PlaygroupModelTests
{
    private static async Task<ApplicationDbContext> CreateSqliteInMemoryDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        return db;
    }

    // =====================================================================
    // Playgroup Model — Can we create and store playgroups?
    // =====================================================================

    [Test]
    // A playgroup can be created and saved to the database with all required fields
    public async Task Playgroup_CanBeCreatedAndSaved()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Friday Night Games",
            Description = "Weekly game night crew",
            CreatedByUserId = "user-1",
            IsPrivate = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var saved = await db.Playgroups.FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.Name, Is.EqualTo("Friday Night Games"));
        Assert.That(saved.Description, Is.EqualTo("Weekly game night crew"));
        Assert.That(saved.CreatedByUserId, Is.EqualTo("user-1"));
        Assert.That(saved.IsPrivate, Is.True);
    }

    [Test]
    // A playgroup can be created with only required fields (Name, CreatedByUserId)
    // Optional fields like Description and ImageUrl can be null
    public async Task Playgroup_OptionalFieldsCanBeNull()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Minimal Group",
            CreatedByUserId = "user-1",
            Description = null,
            ImageUrl = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var saved = await db.Playgroups.FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.Description, Is.Null);
        Assert.That(saved.ImageUrl, Is.Null);
    }

    [Test]
    // Playgroup stores ImageUrl when provided
    public async Task Playgroup_StoresImageUrl()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Avatar Group",
            CreatedByUserId = "user-1",
            ImageUrl = "https://example.com/group-pic.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var saved = await db.Playgroups.FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.ImageUrl, Is.EqualTo("https://example.com/group-pic.png"));
    }

    [Test]
    // Multiple playgroups can have the same name (names are not unique)
    public async Task Playgroup_DuplicateNamesAllowed()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        db.Playgroups.AddRange(
            new Playgroup { Name = "Game Night", CreatedByUserId = "user-1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Playgroup { Name = "Game Night", CreatedByUserId = "user-2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var count = await db.Playgroups.CountAsync(g => g.Name == "Game Night");
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    // IsPrivate defaults to true (playgroups are private by default)
    public async Task Playgroup_DefaultsToPrivate()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Default Privacy Group",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var saved = await db.Playgroups.FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.IsPrivate, Is.True);
    }

    // =====================================================================
    // PlaygroupMember Model — Can we add members to playgroups?
    // =====================================================================

    [Test]
    // A member can be added to a playgroup with a role
    public async Task PlaygroupMember_CanBeAddedToPlaygroup()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Test Group",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var member = new PlaygroupMember
        {
            PlaygroupId = playgroup.Id,
            UserId = "user-1",
            Role = PlaygroupRole.Owner,
            JoinedAt = DateTime.UtcNow
        };
        db.PlaygroupMembers.Add(member);
        await db.SaveChangesAsync();

        var saved = await db.PlaygroupMembers.FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.PlaygroupId, Is.EqualTo(playgroup.Id));
        Assert.That(saved.UserId, Is.EqualTo("user-1"));
        Assert.That(saved.Role, Is.EqualTo(PlaygroupRole.Owner));
    }

    [Test]
    // Multiple members can belong to the same playgroup
    public async Task PlaygroupMember_MultipleMembers()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Multi Member Group",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        db.PlaygroupMembers.AddRange(
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-1", Role = PlaygroupRole.Owner, JoinedAt = DateTime.UtcNow },
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-2", Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow },
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-3", Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var members = await db.PlaygroupMembers.Where(m => m.PlaygroupId == playgroup.Id).ToListAsync();
        Assert.That(members, Has.Count.EqualTo(3));
        Assert.That(members.Count(m => m.Role == PlaygroupRole.Owner), Is.EqualTo(1));
        Assert.That(members.Count(m => m.Role == PlaygroupRole.Member), Is.EqualTo(2));
    }

    [Test]
    // A user can belong to multiple playgroups
    public async Task PlaygroupMember_UserCanBeInMultipleGroups()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var group1 = new Playgroup { Name = "Group A", CreatedByUserId = "user-1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var group2 = new Playgroup { Name = "Group B", CreatedByUserId = "user-2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Playgroups.AddRange(group1, group2);
        await db.SaveChangesAsync();

        db.PlaygroupMembers.AddRange(
            new PlaygroupMember { PlaygroupId = group1.Id, UserId = "user-1", Role = PlaygroupRole.Owner, JoinedAt = DateTime.UtcNow },
            new PlaygroupMember { PlaygroupId = group2.Id, UserId = "user-1", Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var userGroups = await db.PlaygroupMembers.Where(m => m.UserId == "user-1").ToListAsync();
        Assert.That(userGroups, Has.Count.EqualTo(2));
    }

    [Test]
    // Querying a playgroup's members via navigation/join works
    public async Task PlaygroupMember_CanQueryMembersByPlaygroup()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Query Test Group",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        db.PlaygroupMembers.AddRange(
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-1", Role = PlaygroupRole.Owner, JoinedAt = DateTime.UtcNow },
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-2", Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var memberUserIds = await db.PlaygroupMembers
            .Where(m => m.PlaygroupId == playgroup.Id)
            .Select(m => m.UserId)
            .ToListAsync();

        Assert.That(memberUserIds, Has.Count.EqualTo(2));
        Assert.That(memberUserIds, Does.Contain("user-1"));
        Assert.That(memberUserIds, Does.Contain("user-2"));
    }

    [Test]
    // Removing a member from a playgroup works without affecting the playgroup itself
    public async Task PlaygroupMember_CanBeRemoved()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Remove Test Group",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var owner = new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-1", Role = PlaygroupRole.Owner, JoinedAt = DateTime.UtcNow };
        var member = new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-2", Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow };
        db.PlaygroupMembers.AddRange(owner, member);
        await db.SaveChangesAsync();

        // Remove the regular member
        db.PlaygroupMembers.Remove(member);
        await db.SaveChangesAsync();

        var remaining = await db.PlaygroupMembers.Where(m => m.PlaygroupId == playgroup.Id).ToListAsync();
        Assert.That(remaining, Has.Count.EqualTo(1));
        Assert.That(remaining[0].UserId, Is.EqualTo("user-1"));

        // Playgroup itself still exists
        var groupStillExists = await db.Playgroups.FindAsync(playgroup.Id);
        Assert.That(groupStillExists, Is.Not.Null);
    }

    // =====================================================================
    // Playgroup Helper Methods — IsOwner, IsMember, MemberCount
    // =====================================================================

    [Test]
    // IsOwner returns true for the owner and false for a regular member
    public async Task Playgroup_IsOwner_ReturnsTrueForOwner()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Owner Test",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        db.PlaygroupMembers.AddRange(
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-1", Role = PlaygroupRole.Owner, JoinedAt = DateTime.UtcNow },
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-2", Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var loaded = await db.Playgroups.Include(g => g.Members).FirstAsync(g => g.Id == playgroup.Id);
        Assert.That(loaded.IsOwner("user-1"), Is.True);
        Assert.That(loaded.IsOwner("user-2"), Is.False);
        Assert.That(loaded.IsOwner("user-999"), Is.False);
    }

    [Test]
    // IsMember returns true for any member (owner or regular)
    public async Task Playgroup_IsMember_ReturnsTrueForAnyMember()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Member Test",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        db.PlaygroupMembers.AddRange(
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-1", Role = PlaygroupRole.Owner, JoinedAt = DateTime.UtcNow },
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-2", Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var loaded = await db.Playgroups.Include(g => g.Members).FirstAsync(g => g.Id == playgroup.Id);
        Assert.That(loaded.IsMember("user-1"), Is.True);
        Assert.That(loaded.IsMember("user-2"), Is.True);
        Assert.That(loaded.IsMember("user-999"), Is.False);
    }

    [Test]
    // MemberCount returns the correct number of members
    public async Task Playgroup_MemberCount_ReturnsCorrectCount()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Count Test",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var loaded = await db.Playgroups.Include(g => g.Members).FirstAsync(g => g.Id == playgroup.Id);
        Assert.That(loaded.MemberCount(), Is.EqualTo(0));

        db.PlaygroupMembers.AddRange(
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-1", Role = PlaygroupRole.Owner, JoinedAt = DateTime.UtcNow },
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-2", Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        loaded = await db.Playgroups.Include(g => g.Members).FirstAsync(g => g.Id == playgroup.Id);
        Assert.That(loaded.MemberCount(), Is.EqualTo(2));
    }

    // =====================================================================
    // PlaygroupInvite Model — Can we create and manage invites?
    // =====================================================================

    [Test]
    // A playgroup invite can be created and defaults to Pending
    public async Task PlaygroupInvite_DefaultsToPending()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Invite Test Group",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var invite = new PlaygroupInvite
        {
            PlaygroupId = playgroup.Id,
            InvitedUserId = "user-2",
            InvitedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow
        };
        db.PlaygroupInvites.Add(invite);
        await db.SaveChangesAsync();

        var saved = await db.PlaygroupInvites.FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.Status, Is.EqualTo(InviteStatus.Pending));
        Assert.That(saved.RespondedAt, Is.Null);
    }

    [Test]
    // Accepting an invite changes status and sets RespondedAt
    public async Task PlaygroupInvite_CanBeAccepted()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Accept Test Group",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var invite = new PlaygroupInvite
        {
            PlaygroupId = playgroup.Id,
            InvitedUserId = "user-2",
            InvitedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow
        };
        db.PlaygroupInvites.Add(invite);
        await db.SaveChangesAsync();

        invite.Status = InviteStatus.Accepted;
        invite.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.PlaygroupInvites.FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Status, Is.EqualTo(InviteStatus.Accepted));
        Assert.That(saved.RespondedAt, Is.Not.Null);
    }

    [Test]
    // Declining an invite changes status
    public async Task PlaygroupInvite_CanBeDeclined()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Decline Test Group",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var invite = new PlaygroupInvite
        {
            PlaygroupId = playgroup.Id,
            InvitedUserId = "user-2",
            InvitedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow
        };
        db.PlaygroupInvites.Add(invite);
        await db.SaveChangesAsync();

        invite.Status = InviteStatus.Declined;
        invite.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.PlaygroupInvites.FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Status, Is.EqualTo(InviteStatus.Declined));
    }

    [Test]
    // Deleting a playgroup cascades and removes all members
    public async Task Playgroup_DeleteCascadesMembers()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Cascade Test",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        db.PlaygroupMembers.AddRange(
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-1", Role = PlaygroupRole.Owner, JoinedAt = DateTime.UtcNow },
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = "user-2", Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        db.Playgroups.Remove(playgroup);
        await db.SaveChangesAsync();

        var members = await db.PlaygroupMembers.Where(m => m.PlaygroupId == playgroup.Id).ToListAsync();
        Assert.That(members, Has.Count.EqualTo(0));

        var groups = await db.Playgroups.ToListAsync();
        Assert.That(groups, Has.Count.EqualTo(0));
    }

    [Test]
    // Deleting a playgroup cascades and removes all invites
    public async Task Playgroup_DeleteCascadesInvites()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();

        var playgroup = new Playgroup
        {
            Name = "Invite Cascade Test",
            CreatedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        db.PlaygroupInvites.Add(new PlaygroupInvite
        {
            PlaygroupId = playgroup.Id,
            InvitedUserId = "user-2",
            InvitedByUserId = "user-1",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        db.Playgroups.Remove(playgroup);
        await db.SaveChangesAsync();

        var invites = await db.PlaygroupInvites.ToListAsync();
        Assert.That(invites, Has.Count.EqualTo(0));
    }

}
