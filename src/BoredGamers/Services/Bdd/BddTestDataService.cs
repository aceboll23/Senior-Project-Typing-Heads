using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

namespace BoredGamers.Services.Bdd;

public class BddTestDataService
{
  private const string ReviewTestUserName = "bdd_review_user";
  private const string ReviewTestEmail = "bdd_review_user@local.test";
  private const string ReviewTestPassword = "BddReview123";

  private readonly ApplicationDbContext _db;
  private readonly UserManager<User> _userManager;

  public BddTestDataService(ApplicationDbContext db, UserManager<User> userManager)
  {
    _db = db;
    _userManager = userManager;
  }

  public async Task<BddReviewSeedResult> ResetAndSeedReviewTestDataAsync()
  {
    // 1. Remove any existing seeded review test user and all review data tied to it
    var existingUser = await _db.Users
        .OfType<User>()
        .Include(u => u.Profile)
        .FirstOrDefaultAsync(u => u.UserName == ReviewTestUserName);

    if (existingUser != null)
    {
      var existingReviews = await _db.Reviews
          .Where(r => r.UserId == existingUser.Id)
          .ToListAsync();

      if (existingReviews.Count > 0)
      {
        _db.Reviews.RemoveRange(existingReviews);
      }

      var existingCollections = await _db.UserGameCollections
          .Where(c => c.UserId == existingUser.Id)
          .ToListAsync();

      if (existingCollections.Count > 0)
      {
        _db.UserGameCollections.RemoveRange(existingCollections);
      }

      await _db.SaveChangesAsync();

      await _userManager.DeleteAsync(existingUser);
    }

    // 2. Ensure the two review test games exist
    var createGame = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == 900001);
    if (createGame == null)
    {
      createGame = new Game
      {
        BggGameId = 900001,
        Name = "BDD Create Review Game",
        YearPublished = 2024,
        Description = "Seeded game used for BDD create and invalid review scenarios.",
        MinPlayers = 2,
        MaxPlayers = 4,
        PlayTime = 60,
        AverageRating = 7.50m,
        BggNumVoters = 100
      };

      _db.Games.Add(createGame);
    }
    else
    {
      createGame.Name = "BDD Create Review Game";
      createGame.Description = "Seeded game used for BDD create and invalid review scenarios.";
    }

    var existingReviewGame = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == 900002);
    if (existingReviewGame == null)
    {
      existingReviewGame = new Game
      {
        BggGameId = 900002,
        Name = "BDD Existing Review Game",
        YearPublished = 2024,
        Description = "Seeded game used for BDD edit and delete review scenarios.",
        MinPlayers = 2,
        MaxPlayers = 5,
        PlayTime = 90,
        AverageRating = 8.10m,
        BggNumVoters = 150
      };

      _db.Games.Add(existingReviewGame);
    }
    else
    {
      existingReviewGame.Name = "BDD Existing Review Game";
      existingReviewGame.Description = "Seeded game used for BDD edit and delete review scenarios.";
    }

    await _db.SaveChangesAsync();

    // 3. Recreate the seeded BDD user
    var user = new User
    {
      UserName = ReviewTestUserName,
      Email = ReviewTestEmail,
      EmailConfirmed = true
    };

    var createUserResult = await _userManager.CreateAsync(user, ReviewTestPassword);
    if (!createUserResult.Succeeded)
    {
      var errors = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
      throw new InvalidOperationException($"Failed to create seeded BDD user: {errors}");
    }

    // 4. Seed one existing review for edit/delete scenarios
    var seededReview = new Review
    {
      GameId = existingReviewGame.Id,
      UserId = user.Id,
      Rating = 8,
      Text = "BDD seeded review text",
      CreatedAt = DateTime.UtcNow
    };

    _db.Reviews.Add(seededReview);
    await _db.SaveChangesAsync();

    return new BddReviewSeedResult
    {
      Username = ReviewTestUserName,
      Password = ReviewTestPassword,
      CreateGameId = createGame.Id,
      ExistingReviewGameId = existingReviewGame.Id,
      SeededReviewText = seededReview.Text
    };
  }

  public async Task<BddGameNightAttendanceSeedResult> ResetAndSeedGameNightAttendanceTestDataAsync()
  {
    var existingUser = await _db.Users
      .OfType<User>()
      .Include(u => u.Profile)
      .FirstOrDefaultAsync(u => u.UserName == ReviewTestUserName);

    if (existingUser != null)
    {
      var existingEvents = await _db.GameNightEvents
        .Where(e => e.CreatedByUserId == existingUser.Id)
        .ToListAsync();

      if (existingEvents.Count > 0)
      {
        _db.GameNightEvents.RemoveRange(existingEvents);
        await _db.SaveChangesAsync();
      }

      var existingMemberships = await _db.PlaygroupMembers
        .Where(m => m.UserId == existingUser.Id)
        .ToListAsync();

      if (existingMemberships.Count > 0)
      {
        _db.PlaygroupMembers.RemoveRange(existingMemberships);
        await _db.SaveChangesAsync();
      }

      await _userManager.DeleteAsync(existingUser);
  
    }

    var user = new User
    {
      UserName = ReviewTestUserName,
      Email = ReviewTestEmail,
      EmailConfirmed = true
    };

    var createUserResult = await _userManager.CreateAsync(user, ReviewTestPassword);
    if (!createUserResult.Succeeded)
    {
      var errors = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
      throw new InvalidOperationException($"Failed to create seeded BDD user: {errors}");
    }

    var playgroup = new Playgroup
    {
      Name = "BDD Attendance Playgroup",
      Description = "Seeded playgroup for game night attendance BDD tests.",
      CreatedByUserId = user.Id,
      IsPrivate = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _db.Playgroups.Add(playgroup);
    await _db.SaveChangesAsync();

    var playgroupMember = new PlaygroupMember
    {
      PlaygroupId = playgroup.Id,
      UserId = user.Id,
      Role = PlaygroupRole.Owner,
      JoinedAt = DateTime.UtcNow
    };

    _db.PlaygroupMembers.Add(playgroupMember);
    await _db.SaveChangesAsync();

    var gameNightEvent = new GameNightEvent
    {
      PlaygroupId = playgroup.Id,
      CreatedByUserId = user.Id,
      Title = "BDD Attendance Game Night",
      EventDateTime = DateTime.UtcNow.AddDays(7),
      Description = "Seeded game night event for attendance BDD tests.",
      CreatedAt = DateTime.UtcNow
    };

    _db.GameNightEvents.Add(gameNightEvent);
    await _db.SaveChangesAsync();

    return new BddGameNightAttendanceSeedResult
    {
      Username = ReviewTestUserName,
      Password = ReviewTestPassword,
      GameNightEventId = gameNightEvent.Id
    };
  }


  

  private const string CollectionMemberUserName = "bdd_collection_member";
  private const string CollectionMemberEmail = "bdd_collection_member@local.test";
  private const string CollectionMemberPassword = "BddCollection123";

  private const string CollectionOwnerUserName = "bdd_collection_owner";
  private const string CollectionOwnerEmail = "bdd_collection_owner@local.test";
  private const string CollectionOwnerPassword = "BddCollection123";

  private const string NonMemberUserName = "bdd_non_member";
  private const string NonMemberEmail = "bdd_non_member@local.test";
  private const string NonMemberPassword = "BddCollection123";

  public async Task<BddCollectionSeedResult> ResetAndSeedCollectionTestDataAsync()
  {
      // Clean up existing test users and their related data
      foreach (var username in new[] { CollectionMemberUserName, CollectionOwnerUserName, NonMemberUserName })
      {
          var existingUser = await _db.Users
              .OfType<User>()
              .FirstOrDefaultAsync(u => u.UserName == username);

          if (existingUser != null)
          {
              var memberships = await _db.PlaygroupMembers
                  .Where(m => m.UserId == existingUser.Id)
                  .ToListAsync();
              if (memberships.Count > 0)
              {
                  _db.PlaygroupMembers.RemoveRange(memberships);
                  await _db.SaveChangesAsync();
              }

              var collections = await _db.UserGameCollections
                  .Where(c => c.UserId == existingUser.Id)
                  .ToListAsync();
              if (collections.Count > 0)
              {
                  _db.UserGameCollections.RemoveRange(collections);
                  await _db.SaveChangesAsync();
              }

              await _userManager.DeleteAsync(existingUser);
          }
      }

      // Clean up old test playgroups
      var oldPlaygroups = await _db.Playgroups
          .Where(p => p.Name == "BDD Collection Playgroup" || p.Name == "BDD Empty Playgroup")
          .ToListAsync();
      if (oldPlaygroups.Count > 0)
      {
          _db.Playgroups.RemoveRange(oldPlaygroups);
          await _db.SaveChangesAsync();
      }

      // Create member user
      var memberUser = new User
      {
          UserName = CollectionMemberUserName,
          Email = CollectionMemberEmail,
          EmailConfirmed = true
      };
      var memberResult = await _userManager.CreateAsync(memberUser, CollectionMemberPassword);
      if (!memberResult.Succeeded)
          throw new InvalidOperationException($"Failed to create member user: {string.Join("; ", memberResult.Errors.Select(e => e.Description))}");

      // Create owner user
      var ownerUser = new User
      {
          UserName = CollectionOwnerUserName,
          Email = CollectionOwnerEmail,
          EmailConfirmed = true
      };
      var ownerResult = await _userManager.CreateAsync(ownerUser, CollectionOwnerPassword);
      if (!ownerResult.Succeeded)
          throw new InvalidOperationException($"Failed to create owner user: {string.Join("; ", ownerResult.Errors.Select(e => e.Description))}");

      // Create non-member user
      var nonMemberUser = new User
      {
          UserName = NonMemberUserName,
          Email = NonMemberEmail,
          EmailConfirmed = true
      };
      var nonMemberResult = await _userManager.CreateAsync(nonMemberUser, NonMemberPassword);
      if (!nonMemberResult.Succeeded)
          throw new InvalidOperationException($"Failed to create non-member user: {string.Join("; ", nonMemberResult.Errors.Select(e => e.Description))}");

      // Ensure the test game exists
      var testGame = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == 900010);
      if (testGame == null)
      {
          testGame = new Game
          {
              BggGameId = 900010,
              Name = "BDD Collection Test Game",
              YearPublished = 2024,
              Description = "Seeded game for BDD collection tests.",
              MinPlayers = 2,
              MaxPlayers = 4,
              PlayTime = 60,
              AverageRating = 7.50m,
              BggNumVoters = 100
          };
          _db.Games.Add(testGame);
          await _db.SaveChangesAsync();
      }

      // Create playgroup with games (owner + member)
      var collectionPlaygroup = new Playgroup
      {
          Name = "BDD Collection Playgroup",
          Description = "Seeded playgroup for BDD collection tests.",
          CreatedByUserId = ownerUser.Id,
          IsPrivate = false,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow
      };
      _db.Playgroups.Add(collectionPlaygroup);
      await _db.SaveChangesAsync();

      // Create empty playgroup (member only)
      var emptyPlaygroup = new Playgroup
      {
          Name = "BDD Empty Playgroup",
          Description = "Seeded playgroup with no game collections.",
          CreatedByUserId = memberUser.Id,
          IsPrivate = false,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow
      };
      _db.Playgroups.Add(emptyPlaygroup);
      await _db.SaveChangesAsync();

      // Add members to collection playgroup
      _db.PlaygroupMembers.AddRange(
          new PlaygroupMember
          {
              PlaygroupId = collectionPlaygroup.Id,
              UserId = ownerUser.Id,
              Role = PlaygroupRole.Owner,
              JoinedAt = DateTime.UtcNow
          },
          new PlaygroupMember
          {
              PlaygroupId = collectionPlaygroup.Id,
              UserId = memberUser.Id,
              Role = PlaygroupRole.Member,
              JoinedAt = DateTime.UtcNow
          }
      );

      // Add member to empty playgroup
      _db.PlaygroupMembers.Add(new PlaygroupMember
      {
          PlaygroupId = emptyPlaygroup.Id,
          UserId = memberUser.Id,
          Role = PlaygroupRole.Owner,
          JoinedAt = DateTime.UtcNow
      });

      await _db.SaveChangesAsync();

      // Add game to owner's collection
      _db.UserGameCollections.Add(new UserGameCollection
      {
          UserId = ownerUser.Id,
          GameId = testGame.Id,
          DateAdded = DateTime.UtcNow
      });

      await _db.SaveChangesAsync();

      return new BddCollectionSeedResult
      {
          MemberUsername = CollectionMemberUserName,
          MemberPassword = CollectionMemberPassword,
          NonMemberUsername = NonMemberUserName,
          NonMemberPassword = NonMemberPassword,
          OwnerUsername = CollectionOwnerUserName,
          CollectionPlaygroupId = collectionPlaygroup.Id,
          EmptyPlaygroupId = emptyPlaygroup.Id,
          CollectionGameName = testGame.Name
      };
  }
}

public class BddReviewSeedResult
{
  public string Username { get; set; } = "";
  public string Password { get; set; } = "";
  public int CreateGameId { get; set; }
  public int ExistingReviewGameId { get; set; }
  public string SeededReviewText { get; set; } = "";
}