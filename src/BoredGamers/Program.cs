using BoredGamers.Data;
using BoredGamers.Services.Bgg;
using BoredGamers.Services.Games;
using BoredGamers.Services.Email;
using BoredGamers.Services.Collections;
using BoredGamers.Services.GameNightEvents;
using BoredGamers.Services.Posts;
using BoredGamers.Services.Block;
using BoredGamers.Services.ProfilePicture;
using BoredGamers.Services.SocialFeed;
using BoredGamers.Services.Ai;
using BoredGamers.Services.Flags;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BoredGamers.Models;
using BoredGamers.Services;
using BoredGamers.Services.Bdd;
using BoredGamers.Hubs;
using BoredGamers.Services.ContentModeration;
using BoredGamers.Services.Transfers;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
//
//Framework Services
//

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<BddTestDataService>();
builder.Services.AddScoped<BddWishlistTestDataService>();
builder.Services.AddScoped<BddDeleteCollectionTestDataService>();
builder.Services.AddScoped<BddPlaygroupTestDataService>();
builder.Services.AddScoped<BddPostTestDataService>();
builder.Services.AddScoped<BddSocialFeedTestDataService>();
builder.Services.AddScoped<BddDeleteFriendTestDataService>();
builder.Services.AddScoped<BddAverageRatingTestDataService>();
builder.Services.AddScoped<BddSortReviewsTestDataService>();
builder.Services.AddScoped<BddFriendCollectionTestDataService>();
builder.Services.AddScoped<BddBlockTestDataService>();
builder.Services.AddScoped<BddChatTestDataService>();
builder.Services.AddScoped<BddProfilePictureTestDataService>();
builder.Services.AddScoped<BddTradeTestDataService>();
builder.Services.AddScoped<BddViewTradesTestDataService>();
builder.Services.AddScoped<BddAllFriendTradesTestDataService>();
builder.Services.AddScoped<BddTradeCompleteTestDataService>();
// Identity UI uses Razor Pages
builder.Services.AddRazorPages();
builder.Services.AddSignalR();


//
//Database
//

var useBddDatabase = builder.Configuration.GetValue<bool>("UseBddDatabase");
var connectionStringName = useBddDatabase ? "BddConnection" : "DefaultConnection";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//
//Identity
//

// ASP.NET Core Identity (uses EF Core + ApplicationDbContext)
builder.Services
    .AddDefaultIdentity<User>(options =>  // Changed from IdentityUser to User
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.User.RequireUniqueEmail = true;

        // Configure password requirements
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

//
//HTTP Clients
//

builder.Services.AddHttpClient<IBggClient, BggClient>();

//
//Application Services
//

builder.Services.AddScoped<IUserCollectionService, UserCollectionService>();
builder.Services.AddScoped<ReviewService>();

//Register GameService
builder.Services.AddScoped<IGameService, GameService>();
//Register GameNightEventService
builder.Services.AddScoped<IGameNightEventService, GameNightEventService>();
//Sync Service that imports/upserts BGG games into our local database
builder.Services.AddScoped<IGameSyncService, GameSyncService>();

//Profile post service
builder.Services.AddScoped<IProfilePostService, ProfilePostService>();
builder.Services.AddScoped<IPostReplyService, PostReplyService>();
//Social feed service
builder.Services.AddScoped<ISocialFeedService, SocialFeedService>();
//Block service
builder.Services.AddScoped<IBlockService, BlockService>();

//Profile picture service
builder.Services.AddScoped<IProfilePictureService, ProfilePictureService>();

//Email service
builder.Services.AddScoped<IEmailService, EmailService>();

// Game transfer service
builder.Services.AddScoped<IGameTransferService, GameTransferService>();

//AI services (TYP-228)
builder.Services.AddSingleton<IAiClient, AnthropicAiClient>();
builder.Services.AddScoped<IAiRecommendationService, AiRecommendationService>();

// Post flagging (TYP-242)
builder.Services.AddScoped<IPostFlagService, PostFlagService>();

// Content moderation
// Use the fake moderation service in BDD/test mode so tests don't depend on a live API
if (builder.Configuration.GetValue<bool>("UseBddDatabase"))
{
    builder.Services.AddScoped<IContentModerationService, FakeContentModerationService>();
}
else
{
    builder.Services.AddHttpClient<IContentModerationService, OpenAiModerationService>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});
app.UseRouting();

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

// When Uploads:BasePath is configured (e.g. /home/uploads on Azure), serve uploaded
// files from that persistent directory at the /uploads URL path.
var uploadsBasePath = app.Configuration["Uploads:BasePath"];
if (!string.IsNullOrWhiteSpace(uploadsBasePath))
{
    Directory.CreateDirectory(Path.Combine(uploadsBasePath, "avatars"));
    Directory.CreateDirectory(Path.Combine(uploadsBasePath, "posts"));
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsBasePath),
        RequestPath = "/uploads"
    });
}
app.MapStaticAssets();

app.MapControllerRoute(
    name: "conversation",
    pattern: "Messages/Conversation/{username}",
    defaults: new { controller = "Messages", action = "Conversation" });

app.MapControllerRoute(
    name: "deleteAccount",
    pattern: "Settings/DeleteAccount",
    defaults: new { controller = "Settings", action = "DeleteAccount" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "settings",
    pattern: "Settings",
    defaults: new { controller = "Settings", action = "Index" });

// Map Identity endpoints (Razor Pages)
app.MapRazorPages();
app.MapHub<PlaygroupChatHub>("/hubs/playgroup-chat");
app.MapHub<DirectMessageHub>("/hubs/direct-message");
app.MapHub<UserNotificationHub>("/hubs/user-notifications");

// registers a custom url route
app.MapControllerRoute(
    name: "profile",
    pattern: "Profile/{username}",
    defaults: new { controller = "Profile", action = "Index" });

app.MapControllerRoute(
    name: "playgroupCollection",
    pattern: "Playgroup/Collection/{id}",
    defaults: new { controller = "Playgroup", action = "Collection" });

app.MapControllerRoute(
    name: "userSearch",
    pattern: "UserSearch",
    defaults: new { controller = "UserSearch", action = "Index" });



if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/backfill-bgg-voters", async (IGameSyncService sync, IConfiguration config, string? key, CancellationToken ct) =>
    {
        //Read expected key from configuration
        var expectedKey = config["DevBackfillKey"];

        //If a key is configured and it doesn't match, reject
        if (!string.IsNullOrWhiteSpace(expectedKey) && key != expectedKey)
            return Results.Unauthorized();

        var updated = await sync.BackfillBggNumVotersAsync(ct);
        return Results.Ok(new { Updated = updated });
    });

    app.MapPost("/dev/bdd/reset-review-data", async (BddTestDataService bddTestDataService) =>
    {
        var result = await bddTestDataService.ResetAndSeedReviewTestDataAsync();

        return Results.Ok(new
        {
            result.Username,
            result.Password,
            result.CreateGameId,
            result.ExistingReviewGameId,
            result.SeededReviewText
        });
    });

    app.MapPost("/dev/bdd/reset-gamenight-attendance-data", async (BddTestDataService bddTestDataService) =>
    {
        var result = await bddTestDataService.ResetAndSeedGameNightAttendanceTestDataAsync();

        return Results.Ok(new
        {
            result.Username,
            result.Password,
            result.GameNightEventId
        });
    });

    app.MapPost("/dev/bdd/reset-collection-data", async (BddTestDataService bddTestDataService) =>
    {
        var result = await bddTestDataService.ResetAndSeedCollectionTestDataAsync();
        return Results.Ok(new
        {
            result.MemberUsername,
            result.MemberPassword,
            result.NonMemberUsername,
            result.NonMemberPassword,
            result.OwnerUsername,
            result.CollectionPlaygroupId,
            result.EmptyPlaygroupId,
            result.CollectionGameName
        });
    });

    app.MapPost("/dev/bdd/reset-wishlist-data", async (BddWishlistTestDataService bddWishlistTestDataService) =>
    {
        var result = await bddWishlistTestDataService.ResetAndSeedWishlistTestDataAsync();

        return Results.Ok(new
        {
            result.Username,
            result.Password,
            result.GameNotOnWishlistId,
            result.GameAlreadyOnWishlistId,
            result.GameNotOnWishlistName,
            result.GameAlreadyOnWishlistName
        });
    });

    app.MapPost("/dev/bdd/reset-delete-collection-data", async (BddDeleteCollectionTestDataService bddDeleteCollectionTestDataService) =>
    {
        var result = await bddDeleteCollectionTestDataService.ResetAndSeedDeleteCollectionTestDataAsync();

        return Results.Ok(new
        {
            result.Username,
            result.Password,
            result.OwnedGameId,
            result.OwnedGameName
        });
    });

    app.MapPost("/dev/bdd/reset-playgroup-data", async (BddPlaygroupTestDataService bddPlaygroupTestDataService) =>
    {
        var result = await bddPlaygroupTestDataService.ResetAndSeedPlaygroupTestDataAsync();

        return Results.Ok(new
        {
            result.Username,
            result.Password
        });
    });

    app.MapPost("/dev/bdd/reset-post-data", async (BddPostTestDataService bddPostTestDataService) =>
    {
        var result = await bddPostTestDataService.ResetAndSeedPostTestDataAsync();

        return Results.Ok(new
        {
            result.OwnerUsername,
            result.OwnerPassword,
            result.FriendUsername,
            result.FriendPassword,
            result.ExistingPostContent,
            result.SeededGameId,
            result.SeededGameName
        });
    });

    app.MapPost("/dev/bdd/reset-social-feed-data", async (BddSocialFeedTestDataService bddSocialFeedTestDataService) =>
    {
        var result = await bddSocialFeedTestDataService.ResetAndSeedSocialFeedTestDataAsync();

        return Results.Ok(new
        {
            result.ViewerUsername,
            result.ViewerPassword,
            result.FriendUsername,
            result.FriendPassword,
            result.FriendPostContent,
            result.StrangerPostContent,
            result.OlderPostContent,
            result.NewerPostContent
        });
    });

    app.MapPost("/dev/bdd/reset-block-data", async (BddBlockTestDataService svc) =>
    {
        var result = await svc.ResetWithFriendshipAsync();
        return Results.Ok(new
        {
            result.BlockerUsername,
            result.BlockerPassword,
            result.TargetUsername,
            result.TargetPassword
        });
    });

    app.MapPost("/dev/bdd/reset-block-data-preblocked", async (BddBlockTestDataService svc) =>
    {
        var result = await svc.ResetWithBlockAsync();
        return Results.Ok(new
        {
            result.BlockerUsername,
            result.BlockerPassword,
            result.TargetUsername,
            result.TargetPassword,
            result.TargetPostContent
        });
    });

    app.MapGet("/dev/bdd/config-check", (IConfiguration config) =>
    {
        return Results.Ok(new
        {
            UseBddDatabase = config["UseBddDatabase"],
            DefaultConnectionFound = !string.IsNullOrWhiteSpace(config.GetConnectionString("DefaultConnection")),
            BddConnectionFound = !string.IsNullOrWhiteSpace(config.GetConnectionString("BddConnection"))
        });
    });

    app.MapPost("/dev/bdd/reset-delete-friend-data", async (BddDeleteFriendTestDataService bddDeleteFriendTestDataService) =>
    {
        var result = await bddDeleteFriendTestDataService.ResetAndSeedDeleteFriendTestDataAsync();
        return Results.Ok(new
        {
            result.Username,
            result.Password,
            result.FriendUsername,
            result.FriendProfileId
        });
    });

    app.MapPost("/dev/bdd/reset-average-rating-data", async (BddAverageRatingTestDataService bddAverageRatingTestDataService) =>
    {
        var result = await bddAverageRatingTestDataService.ResetAndSeedAverageRatingTestDataAsync();
        return Results.Ok(new
        {
            result.Username,
            result.Password,
            result.GameId,
            result.ExpectedAverage
        });
    });

    app.MapPost("/dev/bdd/reset-sort-reviews-data", async (BddSortReviewsTestDataService bddSortReviewsTestDataService) =>
    {
        var result = await bddSortReviewsTestDataService.ResetAndSeedSortReviewsTestDataAsync();
        return Results.Ok(new
        {
            result.Username,
            result.Password,
            result.GameId
        });
    });

    app.MapPost("/dev/bdd/reset-voting-data", async (BddTestDataService bddTestDataService) =>
    {
        var result = await bddTestDataService.ResetAndSeedVotingTestDataAsync();
        return Results.Ok(new
        {
            result.CreatorUsername,
            result.CreatorPassword,
            result.MemberUsername,
            result.MemberPassword,
            result.EventId,
            result.EventGameId,
            result.GameName
        });
    });

    app.MapPost("/dev/bdd/reset-friend-collection-data", async (BddFriendCollectionTestDataService svc) =>
    {
        var result = await svc.ResetAndSeedAsync();
        return Results.Ok(new
        {
            result.ViewerUsername,
            result.ViewerPassword,
            result.FriendWithGamesUsername,
            result.FriendEmptyUsername,
            result.OwnedGameId,
            result.OwnedGameName,
            result.WishlistGameName
        });
    });

    app.MapPost("/dev/bdd/reset-profile-picture-data", async (BddProfilePictureTestDataService svc) =>
    {
        var result = await svc.ResetAndSeedAsync();
        return Results.Ok(new
        {
            result.Username,
            result.Password
        });
    });

    app.MapPost("/dev/bdd/reset-trade-data", async (BddTradeTestDataService svc) =>
    {
        var result = await svc.ResetWithGameAsync();
        return Results.Ok(new
        {
            result.OwnerUsername,
            result.OwnerPassword,
            result.GameId,
            result.GameName
        });
    });

    app.MapPost("/dev/bdd/reset-trade-data-pretraded", async (BddTradeTestDataService svc) =>
    {
        var result = await svc.ResetWithTradedGameAsync();
        return Results.Ok(new
        {
            result.OwnerUsername,
            result.OwnerPassword,
            result.GameId,
            result.GameName
        });
    });

    app.MapPost("/dev/bdd/reset-view-trades-data", async (BddViewTradesTestDataService svc) =>
    {
        var result = await svc.ResetAndSeedAsync();
        return Results.Ok(new
        {
            result.ViewerUsername,
            result.ViewerPassword,
            result.FriendWithTradesUsername,
            result.FriendNoTradesUsername,
            result.StrangerUsername,
            result.TradeGameName,
            result.NonTradeGameName
        });
    });

    app.MapPost("/dev/bdd/reset-all-friend-trades-data", async (BddAllFriendTradesTestDataService svc) =>
    {
        var result = await svc.ResetAndSeedAsync();
        return Results.Ok(new
        {
            result.ViewerUsername,
            result.ViewerPassword,
            result.Friend1Username,
            result.Friend2Username,
            result.Friend1TradeGameName,
            result.Friend2TradeGameName,
            result.NoTradeGameName,
            result.StrangerUsername
        });
    });

    app.MapPost("/dev/bdd/reset-trade-complete-data", async (BddTradeCompleteTestDataService svc) =>
    {
        var result = await svc.ResetAndSeedAsync();
        return Results.Ok(new
        {
            result.SenderUsername,
            result.SenderPassword,
            result.ReceiverUsername,
            result.ReceiverPassword,
            result.HasGameUsername,
            result.TransferGameName,
            result.PendingGameName,
            result.PendingTransferId
        });
    });

    app.MapPost("/dev/bdd/reset-chat-data", async (BddChatTestDataService svc) =>
    {
        var result = await svc.ResetAndSeedAsync();
        return Results.Ok(new
        {
            result.OwnerUsername,
            result.OwnerPassword,
            result.MemberUsername,
            result.MemberPassword,
            result.OutsiderUsername,
            result.OutsiderPassword,
            result.PlaygroupId,
            result.PlaygroupName
        });
    });

    app.MapPost("/dev/bdd/open-voting/{eventId:int}", async (int eventId, ApplicationDbContext db) =>
    {
        var gameNightEvent = await db.GameNightEvents.FindAsync(eventId);
        if (gameNightEvent == null) return Results.NotFound();

        gameNightEvent.VotingStatus = VotingStatus.Open;
        await db.SaveChangesAsync();

        return Results.Ok(new { eventId, votingStatus = "Open" });
    });

    app.MapPost("/dev/bdd/seed-games", async (ApplicationDbContext db) =>
    {
        if (await db.Games.AnyAsync())
            return Results.Ok(new { message = "Games already seeded" });

        var games = new[]
        {
            new Game { BggGameId = 1, Name = "Gloomhaven",         AverageRating = 8.8m, YearPublished = 2017, LastSyncedAt = DateTime.UtcNow },
            new Game { BggGameId = 2, Name = "Pandemic",           AverageRating = 7.6m, YearPublished = 2008, LastSyncedAt = DateTime.UtcNow },
            new Game { BggGameId = 3, Name = "Terraforming Mars",  AverageRating = 8.4m, YearPublished = 2016, LastSyncedAt = DateTime.UtcNow },
            new Game { BggGameId = 4, Name = "Ticket to Ride",     AverageRating = 7.4m, YearPublished = 2004, LastSyncedAt = DateTime.UtcNow },
            new Game { BggGameId = 5, Name = "Wingspan",           AverageRating = 8.1m, YearPublished = 2019, LastSyncedAt = DateTime.UtcNow },
        };

        db.Games.AddRange(games);
        await db.SaveChangesAsync();
        return Results.Ok(new { message = "Games seeded", count = games.Length });
    });

}

if (Environment.GetEnvironmentVariable("CI") == "true")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.Run();