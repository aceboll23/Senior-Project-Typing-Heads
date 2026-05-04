using System;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
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
    }
}