using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddChatTestDataService
{
    private const string OwnerUserName = "bdd_chat_owner";
    private const string OwnerEmail = "bdd_chat_owner@local.test";
    private const string OwnerPassword = "BddChatOwner123!";

    private const string MemberUserName = "bdd_chat_member";
    private const string MemberEmail = "bdd_chat_member@local.test";
    private const string MemberPassword = "BddChatMember123!";

    private const string OutsiderUserName = "bdd_chat_outsider";
    private const string OutsiderEmail = "bdd_chat_outsider@local.test";
    private const string OutsiderPassword = "BddChatOutsider123!";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public BddChatTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddChatSeedResult> ResetAndSeedAsync()
    {
        var oldPlaygroups = await _db.Playgroups.Where(p => p.Name == "BDD Chat Playgroup").ToListAsync();
        if (oldPlaygroups.Count > 0)
        {
            var playgroupIds = oldPlaygroups.Select(p => p.Id).ToList();

            var messages = await _db.PlaygroupMessages.Where(m => playgroupIds.Contains(m.PlaygroupId)).ToListAsync();
            if (messages.Count > 0) { _db.PlaygroupMessages.RemoveRange(messages); await _db.SaveChangesAsync(); }

            var members = await _db.PlaygroupMembers.Where(m => playgroupIds.Contains(m.PlaygroupId)).ToListAsync();
            if (members.Count > 0) { _db.PlaygroupMembers.RemoveRange(members); await _db.SaveChangesAsync(); }

            _db.Playgroups.RemoveRange(oldPlaygroups);
            await _db.SaveChangesAsync();
        }

        foreach (var username in new[] { OwnerUserName, MemberUserName, OutsiderUserName })
        {
            var existing = await _db.Users.OfType<User>()
                .FirstOrDefaultAsync(u => u.UserName == username);
            if (existing == null) continue;

            var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == existing.Id);
            if (profile != null) { _db.Set<UserProfile>().Remove(profile); await _db.SaveChangesAsync(); }

            await _userManager.DeleteAsync(existing);
        }

        var owner = new User { UserName = OwnerUserName, Email = OwnerEmail, EmailConfirmed = true };
        var ownerResult = await _userManager.CreateAsync(owner, OwnerPassword);
        if (!ownerResult.Succeeded)
            throw new InvalidOperationException($"Failed to create owner: {string.Join("; ", ownerResult.Errors.Select(e => e.Description))}");

        var ownerProfile = new UserProfile { UserId = owner.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<UserProfile>().Add(ownerProfile);
        await _db.SaveChangesAsync();

        var member = new User { UserName = MemberUserName, Email = MemberEmail, EmailConfirmed = true };
        var memberResult = await _userManager.CreateAsync(member, MemberPassword);
        if (!memberResult.Succeeded)
            throw new InvalidOperationException($"Failed to create member: {string.Join("; ", memberResult.Errors.Select(e => e.Description))}");

        var memberProfile = new UserProfile { UserId = member.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<UserProfile>().Add(memberProfile);
        await _db.SaveChangesAsync();

        var outsider = new User { UserName = OutsiderUserName, Email = OutsiderEmail, EmailConfirmed = true };
        var outsiderResult = await _userManager.CreateAsync(outsider, OutsiderPassword);
        if (!outsiderResult.Succeeded)
            throw new InvalidOperationException($"Failed to create outsider: {string.Join("; ", outsiderResult.Errors.Select(e => e.Description))}");

        var playgroup = new Playgroup
        {
            Name = "BDD Chat Playgroup",
            Description = "Seeded playgroup for BDD chat tests.",
            CreatedByUserId = owner.Id,
            IsPrivate = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Playgroups.Add(playgroup);
        await _db.SaveChangesAsync();

        _db.PlaygroupMembers.AddRange(
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = owner.Id, Role = PlaygroupRole.Owner, JoinedAt = DateTime.UtcNow },
            new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = member.Id, Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        // Seed a message so the chat page has content
        _db.PlaygroupMessages.Add(new PlaygroupMessage
        {
            PlaygroupId = playgroup.Id,
            SenderProfileId = ownerProfile.Id,
            Content = "Welcome to the BDD Chat Playgroup!",
            IsSystemMessage = false,
            SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new BddChatSeedResult
        {
            OwnerUsername = OwnerUserName,
            OwnerPassword = OwnerPassword,
            MemberUsername = MemberUserName,
            MemberPassword = MemberPassword,
            OutsiderUsername = OutsiderUserName,
            OutsiderPassword = OutsiderPassword,
            PlaygroupId = playgroup.Id,
            PlaygroupName = playgroup.Name
        };
    }
}

public class BddChatSeedResult
{
    public string OwnerUsername { get; set; } = "";
    public string OwnerPassword { get; set; } = "";
    public string MemberUsername { get; set; } = "";
    public string MemberPassword { get; set; } = "";
    public string OutsiderUsername { get; set; } = "";
    public string OutsiderPassword { get; set; } = "";
    public int PlaygroupId { get; set; }
    public string PlaygroupName { get; set; } = "";
}
