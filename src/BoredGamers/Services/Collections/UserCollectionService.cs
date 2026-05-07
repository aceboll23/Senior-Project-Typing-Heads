using System;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Collections
{
    public interface IUserCollectionService
    {
        Task<bool> AddToCollectionAsync(string userId, int gameId, CancellationToken ct = default);
        Task<bool> IsInCollectionAsync(string userId, int gameId, CancellationToken ct = default);
        Task<bool> AddToWishlistAsync(string userId, int gameId, CancellationToken ct = default);
        Task<bool> IsOnWishlistAsync(string userId, int gameId, CancellationToken ct = default);
        Task<bool> RemoveFromWishlistAsync(string userId, int gameId, CancellationToken ct = default);
        Task<bool> RemoveFromCollectionAsync(string userId, int gameId, CancellationToken ct = default);
        // Returns true = now tradeable, false = now not tradeable, null = game not in user's owned collection
        Task<bool?> ToggleTradeStatusAsync(string userId, int gameId, CancellationToken ct = default);
        // Returns null when viewer is not friends with the owner (or owner not found); empty list when no tradeable games
        Task<List<Game>?> GetFriendTradeableGamesAsync(string viewerUserId, string ownerUsername, CancellationToken ct = default);
        // Returns all tradeable games from all accepted friends, sorted by most recently added, paginated
        Task<(List<FriendTradeItem> Items, int TotalCount)> GetAllFriendsTradeableGamesAsync(string viewerUserId, int page, int pageSize, CancellationToken ct = default);
    }

    public class UserCollectionService : IUserCollectionService
    {
        private readonly ApplicationDbContext _db;

        public UserCollectionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> IsInCollectionAsync(string userId, int gameId, CancellationToken ct = default)
        {
            return await _db.UserGameCollections
                .AnyAsync(x => x.UserId == userId && x.GameId == gameId && x.Status == CollectionStatus.Owned, ct);
        }

        public async Task<bool> IsOnWishlistAsync(string userId, int gameId, CancellationToken ct = default)
        {
            return await _db.UserGameCollections
                .AnyAsync(x => x.UserId == userId && x.GameId == gameId && x.Status == CollectionStatus.Wishlist, ct);
        }

        public async Task<bool> AddToCollectionAsync(string userId, int gameId, CancellationToken ct = default)
        {
            // If already owned, nothing to do
            if (await IsInCollectionAsync(userId, gameId, ct))
                return false;

            // If wishlisted, promote to owned instead of creating a new record
            var existing = await _db.UserGameCollections
                .FirstOrDefaultAsync(x => x.UserId == userId && x.GameId == gameId, ct);

            if (existing != null)
            {
                existing.Status = CollectionStatus.Owned;
                await _db.SaveChangesAsync(ct);
                return true;
            }

            _db.UserGameCollections.Add(new UserGameCollection
            {
                UserId = userId,
                GameId = gameId,
                DateAdded = DateTime.UtcNow,
                Status = CollectionStatus.Owned
            });

            try
            {
                await _db.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public async Task<bool> AddToWishlistAsync(string userId, int gameId, CancellationToken ct = default)
        {
            // Already owned — don't allow wishlisting something you own
            if (await IsInCollectionAsync(userId, gameId, ct))
                return false;

            // Already wishlisted — no duplicates
            if (await IsOnWishlistAsync(userId, gameId, ct))
                return false;

            _db.UserGameCollections.Add(new UserGameCollection
            {
                UserId = userId,
                GameId = gameId,
                DateAdded = DateTime.UtcNow,
                Status = CollectionStatus.Wishlist
            });

            try
            {
                await _db.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public async Task<bool> RemoveFromWishlistAsync(string userId, int gameId, CancellationToken ct = default)
        {
            var entry = await _db.UserGameCollections
                .FirstOrDefaultAsync(x => x.UserId == userId && x.GameId == gameId && x.Status == CollectionStatus.Wishlist, ct);

            if (entry == null)
                return false;

            _db.UserGameCollections.Remove(entry);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RemoveFromCollectionAsync(string userId, int gameId, CancellationToken ct = default)
        {
            var entry = await _db.UserGameCollections
                .FirstOrDefaultAsync(x => x.UserId == userId && x.GameId == gameId && x.Status == CollectionStatus.Owned, ct);

            if (entry == null)
                return false;

            _db.UserGameCollections.Remove(entry);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool?> ToggleTradeStatusAsync(string userId, int gameId, CancellationToken ct = default)
        {
            var entry = await _db.UserGameCollections
                .FirstOrDefaultAsync(x => x.UserId == userId && x.GameId == gameId && x.Status == CollectionStatus.Owned, ct);

            if (entry == null)
                return null;

            entry.IsAvailableForTrade = !entry.IsAvailableForTrade;
            await _db.SaveChangesAsync(ct);
            return entry.IsAvailableForTrade;
        }

        public async Task<List<Game>?> GetFriendTradeableGamesAsync(string viewerUserId, string ownerUsername, CancellationToken ct = default)
        {
            var owner = await _db.Set<User>()
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.UserName == ownerUsername, ct);

            if (owner?.Profile == null)
                return null;

            var viewerProfile = await _db.Set<UserProfile>()
                .FirstOrDefaultAsync(p => p.UserId == viewerUserId, ct);

            if (viewerProfile == null)
                return null;

            var areFriends = await _db.Set<Friendship>()
                .AnyAsync(f =>
                    f.Status == FriendshipStatus.Accepted &&
                    ((f.RequesterProfileId == viewerProfile.Id && f.ReceiverProfileId == owner.Profile.Id) ||
                     (f.RequesterProfileId == owner.Profile.Id && f.ReceiverProfileId == viewerProfile.Id)), ct);

            if (!areFriends)
                return null;

            return await _db.UserGameCollections
                .AsNoTracking()
                .Where(c => c.UserId == owner.Id && c.Status == CollectionStatus.Owned && c.IsAvailableForTrade)
                .Include(c => c.Game)
                .OrderBy(c => c.Game.Name)
                .Select(c => c.Game)
                .ToListAsync(ct);
        }

        public async Task<(List<FriendTradeItem> Items, int TotalCount)> GetAllFriendsTradeableGamesAsync(
            string viewerUserId, int page, int pageSize, CancellationToken ct = default)
        {
            var viewerProfile = await _db.Set<UserProfile>()
                .FirstOrDefaultAsync(p => p.UserId == viewerUserId, ct);
            if (viewerProfile == null)
                return (new List<FriendTradeItem>(), 0);

            var friendProfileIdsAsRequester = await _db.Set<Friendship>()
                .Where(f => f.Status == FriendshipStatus.Accepted && f.RequesterProfileId == viewerProfile.Id)
                .Select(f => f.ReceiverProfileId)
                .ToListAsync(ct);

            var friendProfileIdsAsReceiver = await _db.Set<Friendship>()
                .Where(f => f.Status == FriendshipStatus.Accepted && f.ReceiverProfileId == viewerProfile.Id)
                .Select(f => f.RequesterProfileId)
                .ToListAsync(ct);

            var friendProfileIds = friendProfileIdsAsRequester.Concat(friendProfileIdsAsReceiver).ToList();
            if (!friendProfileIds.Any())
                return (new List<FriendTradeItem>(), 0);

            var blockedByMe = await _db.Set<BlockedUser>()
                .Where(b => b.BlockerProfileId == viewerProfile.Id)
                .Select(b => b.BlockedProfileId)
                .ToListAsync(ct);

            var blockedMe = await _db.Set<BlockedUser>()
                .Where(b => b.BlockedProfileId == viewerProfile.Id)
                .Select(b => b.BlockerProfileId)
                .ToListAsync(ct);

            var blockedProfileIds = blockedByMe.Concat(blockedMe).ToHashSet();
            var activeFriendProfileIds = friendProfileIds.Where(id => !blockedProfileIds.Contains(id)).ToList();
            if (!activeFriendProfileIds.Any())
                return (new List<FriendTradeItem>(), 0);

            var friendProfiles = await _db.Set<UserProfile>()
                .Where(p => activeFriendProfileIds.Contains(p.Id))
                .Select(p => new { p.Id, p.UserId })
                .ToListAsync(ct);

            var friendUserIds = friendProfiles.Select(p => p.UserId).ToList();

            var friendUsers = await _db.Set<User>()
                .Where(u => friendUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync(ct);
            var usernameLookup = friendUsers.ToDictionary(u => u.Id, u => u.UserName ?? "");

            var baseQuery = _db.UserGameCollections
                .Where(c => friendUserIds.Contains(c.UserId) &&
                            c.Status == CollectionStatus.Owned &&
                            c.IsAvailableForTrade);

            var totalCount = await baseQuery.CountAsync(ct);

            var rows = await baseQuery
                .AsNoTracking()
                .Include(c => c.Game)
                .OrderByDescending(c => c.DateAdded)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new { c.Game, c.UserId, c.DateAdded })
                .ToListAsync(ct);

            var items = rows.Select(r => new FriendTradeItem
            {
                Game = r.Game,
                OwnerUserId = r.UserId,
                OwnerUsername = usernameLookup.GetValueOrDefault(r.UserId, ""),
                DateAdded = r.DateAdded
            }).ToList();

            return (items, totalCount);
        }
    }
}