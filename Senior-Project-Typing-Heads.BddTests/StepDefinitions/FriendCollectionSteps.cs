using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using System.Net.Http.Json;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class FriendCollectionSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddFriendCollectionSeedDataContext _seedDataContext;
    private readonly LoginHelper _loginHelper;

    public FriendCollectionSteps(
        WebDriverContext webDriverContext,
        BddFriendCollectionSeedDataContext seedDataContext,
        LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedDataContext = seedDataContext;
        _loginHelper = loginHelper;
    }

    private class ResetFriendCollectionDataResponse
    {
        public string ViewerUsername { get; set; } = "";
        public string ViewerPassword { get; set; } = "";
        public string FriendWithGamesUsername { get; set; } = "";
        public string FriendEmptyUsername { get; set; } = "";
        public int OwnedGameId { get; set; }
        public string OwnedGameName { get; set; } = "";
        public string WishlistGameName { get; set; } = "";
    }

    [Given("I am logged in as a BDD friend collection viewer")]
    public void GivenIAmLoggedInAsABddFriendCollectionViewer()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-friend-collection-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetFriendCollectionDataResponse>()
            .GetAwaiter()
            .GetResult();

        ArgumentNullException.ThrowIfNull(seedData, "Failed to read BDD friend collection seed data response.");

        _seedDataContext.ViewerUsername = seedData.ViewerUsername;
        _seedDataContext.FriendWithGamesUsername = seedData.FriendWithGamesUsername;
        _seedDataContext.FriendEmptyUsername = seedData.FriendEmptyUsername;
        _seedDataContext.OwnedGameId = seedData.OwnedGameId;
        _seedDataContext.OwnedGameName = seedData.OwnedGameName;
        _seedDataContext.WishlistGameName = seedData.WishlistGameName;

        _loginHelper.Login(seedData.ViewerUsername, seedData.ViewerPassword);
    }

    [When("I navigate to the friend's collection page")]
    public void WhenINavigateToTheFriendsCollectionPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/collection/{_seedDataContext.FriendWithGamesUsername}");
    }

    [When("I navigate to the empty friend's collection page")]
    public void WhenINavigateToTheEmptyFriendsCollectionPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/collection/{_seedDataContext.FriendEmptyUsername}");
    }

    [When("I navigate to my own collection via the friend route")]
    public void WhenINavigateToMyOwnCollectionViaTheFriendRoute()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/collection/{_seedDataContext.ViewerUsername}");
    }

    [Then("the owned game is shown")]
    public void ThenTheOwnedGameIsShown()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain(_seedDataContext.OwnedGameName));
    }

    [Then("the wishlist game is not shown")]
    public void ThenTheWishlistGameIsNotShown()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Not.Contain(_seedDataContext.WishlistGameName));
    }

    [Then("an empty state message is shown")]
    public void ThenAnEmptyStateMessageIsShown()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain("hasn't added any games"));
    }

    [Then("a back link to the friend's profile is present")]
    public void ThenABackLinkToTheFriendsProfileIsPresent()
    {
        var links = _webDriverContext.Driver!.FindElements(By.TagName("a"));
        var expectedPath = $"/Profile/{_seedDataContext.FriendWithGamesUsername}";
        var hasBackLink = links.Any(l =>
            (l.GetDomAttribute("href") ?? "").Contains(expectedPath, StringComparison.OrdinalIgnoreCase));
        Assert.That(hasBackLink, Is.True, $"Expected a back link pointing to {expectedPath}");
    }

    [Then("I am redirected to my own collection page")]
    public void ThenIAmRedirectedToMyOwnCollectionPage()
    {
        new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10))
            .Until(d =>
            {
                var url = d.Url.TrimEnd('/');
                return url.EndsWith("/collection", StringComparison.OrdinalIgnoreCase)
                    && !url.Contains(_seedDataContext.ViewerUsername, StringComparison.OrdinalIgnoreCase);
            });

        var finalUrl = _webDriverContext.Driver!.Url;
        Assert.That(finalUrl, Does.Contain("collection").IgnoreCase);
        Assert.That(finalUrl, Does.Not.Contain(_seedDataContext.ViewerUsername).IgnoreCase);
    }
}
