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
    }
}