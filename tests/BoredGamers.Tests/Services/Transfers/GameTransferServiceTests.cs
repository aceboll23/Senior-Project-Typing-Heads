using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Transfers;
using BoredGamers.Tests.TestUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.Transfers;

[TestFixture]
public class GameTransferServiceTests
{
    private SqliteConnection _conn = null!;
    private ApplicationDbContext _db = null!;
    private GameTransferService _svc = null!;

    private User _sender = null!;
    private User _receiver = null!;
    private Game _game = null!;

    [SetUp]
    public async Task SetUp()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        await _conn.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_conn)
            .Options;
        _db = new TestApplicationDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        _svc = new GameTransferService(_db);

        _sender = new User { Id = "sender-1", UserName = "Sender", Email = "sender@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _receiver = new User { Id = "receiver-1", UserName = "Receiver", Email = "receiver@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Users.AddRange(_sender, _receiver);

        _game = new Game { BggGameId = 9001, Name = "Test Game", LastSyncedAt = DateTime.UtcNow };
        _db.Games.Add(_game);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.DisposeAsync();
        await _conn.DisposeAsync();
    }

    private async Task GiveSenderTheGame(bool tradeable = true)
    {
        _db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = _sender.Id,
            GameId = _game.Id,
            DateAdded = DateTime.UtcNow,
            Status = CollectionStatus.Owned,
            IsAvailableForTrade = tradeable
        });
        await _db.SaveChangesAsync();
    }

    // ── InitiateTransferAsync ────────────────────────────────────────────────

    [Test]
    public async Task InitiateTransferAsync_RemovesGameFromSendersCollection()
    {
        await GiveSenderTheGame();
        await _svc.InitiateTransferAsync(_sender.Id, _game.Id, _receiver.UserName!);
        var stillOwned = await _db.UserGameCollections
            .AnyAsync(c => c.UserId == _sender.Id && c.GameId == _game.Id);
        Assert.That(stillOwned, Is.False);
    }

    [Test]
    public async Task InitiateTransferAsync_CreatesPendingTransfer()
    {
        await GiveSenderTheGame();
        var result = await _svc.InitiateTransferAsync(_sender.Id, _game.Id, _receiver.UserName!);
        Assert.That(result.Success, Is.True);
        var transfer = await _db.GameTransfers.FirstOrDefaultAsync(t => t.FromUserId == _sender.Id);
        Assert.That(transfer, Is.Not.Null);
        Assert.That(transfer!.Status, Is.EqualTo(GameTransferStatus.Pending));
        Assert.That(transfer.ToUserId, Is.EqualTo(_receiver.Id));
    }

    [Test]
    public async Task InitiateTransferAsync_ReturnsFailWhenSenderDoesNotOwnGame()
    {
        var result = await _svc.InitiateTransferAsync(_sender.Id, _game.Id, _receiver.UserName!);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("do not own"));
    }

    [Test]
    public async Task InitiateTransferAsync_ReturnsFailWhenSelfTransfer()
    {
        await GiveSenderTheGame();
        var result = await _svc.InitiateTransferAsync(_sender.Id, _game.Id, _sender.UserName!);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("yourself"));
    }

    [Test]
    public async Task InitiateTransferAsync_ReturnsFailWhenReceiverNotFound()
    {
        await GiveSenderTheGame();
        var result = await _svc.InitiateTransferAsync(_sender.Id, _game.Id, "nonexistent-user");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not found"));
    }

    [Test]
    public async Task InitiateTransferAsync_ReturnsFailWhenReceiverAlreadyOwnsGame()
    {
        await GiveSenderTheGame();
        _db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = _receiver.Id,
            GameId = _game.Id,
            DateAdded = DateTime.UtcNow,
            Status = CollectionStatus.Owned
        });
        await _db.SaveChangesAsync();

        var result = await _svc.InitiateTransferAsync(_sender.Id, _game.Id, _receiver.UserName!);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("already has"));
    }

    [Test]
    public async Task InitiateTransferAsync_ReturnsFailWhenPendingTransferAlreadyExists()
    {
        await GiveSenderTheGame();
        _db.GameTransfers.Add(new GameTransfer
        {
            FromUserId = _sender.Id,
            ToUserId = _receiver.Id,
            GameId = _game.Id,
            Status = GameTransferStatus.Pending,
            InitiatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _svc.InitiateTransferAsync(_sender.Id, _game.Id, _receiver.UserName!);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("pending transfer"));
    }

    // ── AcceptTransferAsync ──────────────────────────────────────────────────

    private async Task<GameTransfer> CreatePendingTransfer()
    {
        var transfer = new GameTransfer
        {
            FromUserId = _sender.Id,
            ToUserId = _receiver.Id,
            GameId = _game.Id,
            Status = GameTransferStatus.Pending,
            InitiatedAt = DateTime.UtcNow
        };
        _db.GameTransfers.Add(transfer);
        await _db.SaveChangesAsync();
        return transfer;
    }

    [Test]
    public async Task AcceptTransferAsync_AddsGameToReceiverCollection()
    {
        var transfer = await CreatePendingTransfer();
        await _svc.AcceptTransferAsync(_receiver.Id, transfer.Id);
        var owned = await _db.UserGameCollections
            .AnyAsync(c => c.UserId == _receiver.Id && c.GameId == _game.Id && c.Status == CollectionStatus.Owned);
        Assert.That(owned, Is.True);
    }

    [Test]
    public async Task AcceptTransferAsync_MarksTransferAsAccepted()
    {
        var transfer = await CreatePendingTransfer();
        await _svc.AcceptTransferAsync(_receiver.Id, transfer.Id);
        var updated = await _db.GameTransfers.FindAsync(transfer.Id);
        Assert.That(updated!.Status, Is.EqualTo(GameTransferStatus.Accepted));
        Assert.That(updated.RespondedAt, Is.Not.Null);
    }

    [Test]
    public async Task AcceptTransferAsync_AddedGameIsNotMarkedAsAvailableForTrade()
    {
        var transfer = await CreatePendingTransfer();
        await _svc.AcceptTransferAsync(_receiver.Id, transfer.Id);
        var entry = await _db.UserGameCollections
            .FirstOrDefaultAsync(c => c.UserId == _receiver.Id && c.GameId == _game.Id);
        Assert.That(entry!.IsAvailableForTrade, Is.False);
    }

    [Test]
    public async Task AcceptTransferAsync_ReturnsFailWhenTransferNotFound()
    {
        var result = await _svc.AcceptTransferAsync(_receiver.Id, 9999);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task AcceptTransferAsync_ReturnsFailWhenUserIsNotRecipient()
    {
        var transfer = await CreatePendingTransfer();
        var result = await _svc.AcceptTransferAsync(_sender.Id, transfer.Id);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Does.Contain("not the recipient"));
    }

    // ── DeclineTransferAsync ─────────────────────────────────────────────────

    [Test]
    public async Task DeclineTransferAsync_MarksTransferAsDeclined()
    {
        var transfer = await CreatePendingTransfer();
        var result = await _svc.DeclineTransferAsync(_receiver.Id, transfer.Id);
        Assert.That(result.Success, Is.True);
        var updated = await _db.GameTransfers.FindAsync(transfer.Id);
        Assert.That(updated!.Status, Is.EqualTo(GameTransferStatus.Declined));
        Assert.That(updated.RespondedAt, Is.Not.Null);
    }

    [Test]
    public async Task DeclineTransferAsync_DoesNotAddGameToReceiverCollection()
    {
        var transfer = await CreatePendingTransfer();
        await _svc.DeclineTransferAsync(_receiver.Id, transfer.Id);
        var owned = await _db.UserGameCollections
            .AnyAsync(c => c.UserId == _receiver.Id && c.GameId == _game.Id);
        Assert.That(owned, Is.False);
    }

    [Test]
    public async Task DeclineTransferAsync_DoesNotRestoreGameToSender()
    {
        var transfer = await CreatePendingTransfer();
        await _svc.DeclineTransferAsync(_receiver.Id, transfer.Id);
        var restored = await _db.UserGameCollections
            .AnyAsync(c => c.UserId == _sender.Id && c.GameId == _game.Id);
        Assert.That(restored, Is.False);
    }

    // ── GetPendingTransfersForUserAsync ──────────────────────────────────────

    [Test]
    public async Task GetPendingTransfersForUserAsync_ReturnsPendingTransfersForUser()
    {
        await CreatePendingTransfer();
        var results = await _svc.GetPendingTransfersForUserAsync(_receiver.Id);
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Game.Name, Is.EqualTo(_game.Name));
        Assert.That(results[0].FromUsername, Is.EqualTo(_sender.UserName));
    }

    [Test]
    public async Task GetPendingTransfersForUserAsync_DoesNotReturnAcceptedTransfers()
    {
        var transfer = await CreatePendingTransfer();
        await _svc.AcceptTransferAsync(_receiver.Id, transfer.Id);
        var results = await _svc.GetPendingTransfersForUserAsync(_receiver.Id);
        Assert.That(results, Is.Empty);
    }
}
