using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using System.Net.Http.Json;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class WishlistSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddWishlistSeedDataContext _wishlistSeedDataContext;
    private readonly LoginHelper _loginHelper;

    public WishlistSteps(WebDriverContext webDriverContext, BddWishlistSeedDataContext wishlistSeedDataContext, LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _wishlistSeedDataContext = wishlistSeedDataContext;
        _loginHelper = loginHelper;
    }

    private class ResetWishlistDataResponse
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int GameNotOnWishlistId { get; set; }
        public int GameAlreadyOnWishlistId { get; set; }
        public string GameNotOnWishlistName { get; set; } = "";
        public string GameAlreadyOnWishlistName { get; set; } = "";
    }

    [Given("I am logged in as a BDD wishlist user")]
    public void GivenIAmLoggedInAsABddWishlistUser()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-wishlist-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetWishlistDataResponse>()
            .GetAwaiter()
            .GetResult();

        ArgumentNullException.ThrowIfNull(seedData, "Failed to read BDD wishlist seed data response.");

        _wishlistSeedDataContext.GameNotOnWishlistId = seedData.GameNotOnWishlistId;
        _wishlistSeedDataContext.GameAlreadyOnWishlistId = seedData.GameAlreadyOnWishlistId;
        _wishlistSeedDataContext.GameNotOnWishlistName = seedData.GameNotOnWishlistName;
        _wishlistSeedDataContext.GameAlreadyOnWishlistName = seedData.GameAlreadyOnWishlistName;

        _loginHelper.Login(seedData.Username, seedData.Password);
    }

    [When("I navigate to the game not on my wishlist")]
    public void WhenINavigateToTheGameNotOnMyWishlist()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/Games/Details/{_wishlistSeedDataContext.GameNotOnWishlistId}");
    }

    [When("I navigate to the game already on my wishlist")]
    public void WhenINavigateToTheGameAlreadyOnMyWishlist()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/Games/Details/{_wishlistSeedDataContext.GameAlreadyOnWishlistId}");
    }

    [When("I click the Add to Wishlist button")]
    public void WhenIClickTheAddToWishlistButton()
    {
        var driver = _webDriverContext.Driver!;
        driver.FindElement(By.Id("addToWishlistBtn")).Click();

        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d =>
            {
                var btn = d.FindElement(By.Id("addToWishlistBtn"));
                return !btn.Enabled || btn.GetAttribute("disabled") != null || btn.Text.Contains("On Wishlist");
            });
    }

    [Then("the game appears in my wishlist")]
    public void ThenTheGameAppearsInMyWishlist()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Collection");
        var body = _webDriverContext.Driver.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain(_wishlistSeedDataContext.GameNotOnWishlistName));
    }

    [Then("the Add to Wishlist button is disabled or shows On Wishlist")]
    public void ThenTheAddToWishlistButtonIsDisabledOrShowsOnWishlist()
    {
        var button = _webDriverContext.Driver!.FindElement(By.Id("addToWishlistBtn"));
        var isDisabled = !button.Enabled || button.GetAttribute("disabled") != null;
        var showsOnWishlist = button.Text.Contains("On Wishlist");
        Assert.That(isDisabled || showsOnWishlist, Is.True, "Button should be disabled or show 'On Wishlist'");
    }

    [Then("the wishlisted game appears in the wishlist section but not the owned section")]
    public void ThenTheWishlistedGameAppearsInTheWishlistSectionButNotTheOwnedSection()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Collection");
        var body = _webDriverContext.Driver.FindElement(By.TagName("body")).Text;

        var wishlistHeadingIndex = body.IndexOf("My Wishlist", StringComparison.OrdinalIgnoreCase);
        Assert.That(wishlistHeadingIndex, Is.GreaterThan(0), "My Wishlist section should exist on the page");

        var ownedSection = body.Substring(0, wishlistHeadingIndex);
        var wishlistSection = body.Substring(wishlistHeadingIndex);

        Assert.That(wishlistSection, Does.Contain(_wishlistSeedDataContext.GameNotOnWishlistName),
            "Wishlisted game should appear in the wishlist section");
        Assert.That(ownedSection, Does.Not.Contain(_wishlistSeedDataContext.GameNotOnWishlistName),
            "Wishlisted game should not appear in the owned section");
    }

    [Given("I am not logged in")]
    public void GivenIAmNotLoggedIn()
    {
        // Delete all cookies to ensure no active session
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}");
        _webDriverContext.Driver.Manage().Cookies.DeleteAllCookies();
    }

    [When("I navigate to the game not on my wishlist as a guest")]
    public void WhenINavigateToTheGameNotOnMyWishlistAsAGuest()
    {
        using var httpClient = new HttpClient();
        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-wishlist-data", null)
            .GetAwaiter()
            .GetResult();
        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetWishlistDataResponse>()
            .GetAwaiter()
            .GetResult();

        ArgumentNullException.ThrowIfNull(seedData);
        _wishlistSeedDataContext.GameNotOnWishlistId = seedData.GameNotOnWishlistId;

        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/Games/Details/{_wishlistSeedDataContext.GameNotOnWishlistId}");
    }

    [Then("the Add to Wishlist button is not visible")]
    public void ThenTheAddToWishlistButtonIsNotVisible()
    {
        var buttons = _webDriverContext.Driver!.FindElements(By.Id("addToWishlistBtn"));
        Assert.That(buttons.Count, Is.EqualTo(0), "Add to Wishlist button should not be visible to unauthenticated users");
    }
}
