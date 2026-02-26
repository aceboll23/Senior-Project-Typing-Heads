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
                .AnyAsync(x => x.UserId == userId && x.GameId == gameId, ct);
        }

        public async Task<bool> AddToCollectionAsync(string userId, int gameId, CancellationToken ct = default)
        {
            // Fast path: already exists
            if (await IsInCollectionAsync(userId, gameId, ct))
                return false;

            _db.UserGameCollections.Add(new UserGameCollection
            {
                UserId = userId,
                GameId = gameId,
                DateAdded = DateTime.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync(ct);
                return true; // added
            }
            catch (DbUpdateException)
            {
                // Handles race condition / double-click / duplicate request
                return false;
            }
        }
    }
}