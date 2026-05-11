using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddTradeCompleteTestDataService
{
    private const string SenderUserName = "bdd_tc_sender";
    private const string SenderEmail = "bdd_tc_sender@local.test";
    private const string SenderPassword = "BddTcSender123!";

    private const string ReceiverUserName = "bdd_tc_receiver";
    private const string ReceiverEmail = "bdd_tc_receiver@local.test";
    private const string ReceiverPassword = "BddTcReceiver123!";

    private const string HasGameUserName = "bdd_tc_hasgame";
    private const string HasGameEmail = "bdd_tc_hasgame@local.test";
    private const string HasGamePassword = "BddTcHasGame123!";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public BddTradeCompleteTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddTradeCompleteSeedResult> ResetAndSeedAsync()
    {
        await CleanupAsync();

        // Game A: in sender's collection, tradeable (for initiation + duplicate tests)
        var transferGame = await UpsertGameAsync(900080, "BDD TC Transfer Game");
        // Game B: removed from sender, pending transfer to receiver (for accept/decline tests)
        var pendingGame = await UpsertGameAsync(900081, "BDD TC Pending Game");

        var sender = await CreateUserAsync(SenderUserName, SenderEmail, SenderPassword);
        var receiver = await CreateUserAsync(ReceiverUserName, ReceiverEmail, ReceiverPassword);
        var hasGame = await CreateUserAsync(HasGameUserName, HasGameEmail, HasGamePassword);

        // Sender owns Game A (tradeable) — used for initiating transfer tests
        _db.UserGameCollections.AddRange(
            new UserGameCollection { UserId = sender.Id, GameId = transferGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true },
            // hasGame user also owns Game A — used for duplicate rejection test
            new UserGameCollection { UserId = hasGame.Id, GameId = transferGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = false }
        );
        await _db.SaveChangesAsync();

        // Pre-create a pending transfer of Game B from sender → receiver (Game B removed from sender)
        var pendingTransfer = new GameTransfer
        {
            FromUserId = sender.Id,
            ToUserId = receiver.Id,
            GameId = pendingGame.Id,
            Status = GameTransferStatus.Pending,
            InitiatedAt = DateTime.UtcNow
        };
        _db.GameTransfers.Add(pendingTransfer);
        await _db.SaveChangesAsync();

        return new BddTradeCompleteSeedResult
        {
            SenderUsername = SenderUserName,
            SenderPassword = SenderPassword,
            ReceiverUsername = ReceiverUserName,
            ReceiverPassword = ReceiverPassword,
            HasGameUsername = HasGameUserName,
            TransferGameName = transferGame.Name,
            PendingGameName = pendingGame.Name,
            PendingTransferId = pendingTransfer.Id
        };
    }

    private async Task CleanupAsync()
    {
        foreach (var username in new[] { SenderUserName, ReceiverUserName, HasGameUserName })
        {
            var existing = await _db.Users.OfType<User>().FirstOrDefaultAsync(u => u.UserName == username);
            if (existing == null) continue;

            var transfers = await _db.GameTransfers
                .Where(t => t.FromUserId == existing.Id || t.ToUserId == existing.Id)
                .ToListAsync();
            _db.GameTransfers.RemoveRange(transfers);

            var collections = await _db.UserGameCollections.Where(c => c.UserId == existing.Id).ToListAsync();
            _db.UserGameCollections.RemoveRange(collections);
            await _db.SaveChangesAsync();

            await _userManager.DeleteAsync(existing);
        }
    }

    private async Task<Game> UpsertGameAsync(int bggId, string name)
    {
        var game = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == bggId);
        if (game == null)
        {
            game = new Game { BggGameId = bggId, Name = name, YearPublished = 2024, AverageRating = 7.0m, BggNumVoters = 50, LastSyncedAt = DateTime.UtcNow };
            _db.Games.Add(game);
        }
        else
        {
            game.Name = name;
        }
        await _db.SaveChangesAsync();
        return game;
    }

    private async Task<User> CreateUserAsync(string username, string email, string password)
    {
        var user = new User { UserName = username, Email = email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create {username}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        return user;
    }
}

public class BddTradeCompleteSeedResult
{
    public string SenderUsername { get; set; } = "";
    public string SenderPassword { get; set; } = "";
    public string ReceiverUsername { get; set; } = "";
    public string ReceiverPassword { get; set; } = "";
    public string HasGameUsername { get; set; } = "";
    public string TransferGameName { get; set; } = "";
    public string PendingGameName { get; set; } = "";
    public int PendingTransferId { get; set; }
}
