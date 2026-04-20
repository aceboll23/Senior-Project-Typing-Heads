using BoredGamers.Data;
using BoredGamers.Services.Bgg;
using BoredGamers.Services.Games;
using BoredGamers.Services.Email;
using BoredGamers.Services.Collections;
using BoredGamers.Services.GameNightEvents;
using BoredGamers.Services.Posts;
using BoredGamers.Services.Block;
using BoredGamers.Services.SocialFeed;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BoredGamers.Models;
using BoredGamers.Services;
using BoredGamers.Services.Bdd;

var builder = WebApplication.CreateBuilder(args);
//
//Framework Services
//

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<BddTestDataService>();
builder.Services.AddScoped<BddWishlistTestDataService>();
builder.Services.AddScoped<BddPlaygroupTestDataService>();
builder.Services.AddScoped<BddPostTestDataService>();
builder.Services.AddScoped<BddSocialFeedTestDataService>();
builder.Services.AddScoped<BddDeleteFriendTestDataService>();
builder.Services.AddScoped<BddAverageRatingTestDataService>();
builder.Services.AddScoped<BddFriendCollectionTestDataService>();
builder.Services.AddScoped<BddBlockTestDataService>();
// Identity UI uses Razor Pages
builder.Services.AddRazorPages();

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
//Social feed service
builder.Services.AddScoped<ISocialFeedService, SocialFeedService>();
//Block service
builder.Services.AddScoped<IBlockService, BlockService>();

//Email service
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

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

// registers a custom url route
app.MapControllerRoute(
    name: "profile",
    pattern: "Profile/{username}",
    defaults: new { controller = "Profile", action = "Index" });

app.MapControllerRoute(
    name: "playgroupCollection",
    pattern: "Playgroup/Collection/{id}",
    defaults: new { controller = "Playgroup", action = "Collection" });



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
            result.ExistingPostContent
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

    app.MapPost("/dev/bdd/open-voting/{eventId:int}", async (int eventId, ApplicationDbContext db) =>
    {
        var gameNightEvent = await db.GameNightEvents.FindAsync(eventId);
        if (gameNightEvent == null) return Results.NotFound();

        gameNightEvent.VotingStatus = VotingStatus.Open;
        await db.SaveChangesAsync();

        return Results.Ok(new { eventId, votingStatus = "Open" });
    });

}
app.Run();