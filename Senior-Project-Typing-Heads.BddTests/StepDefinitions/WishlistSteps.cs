using OpenQA.Selenium;
using Reqnroll;
using System.Net.Http.Json;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class WishlistSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddWishlistSeedDataContext _wishlistSeedDataContext;

    public WishlistSteps(WebDriverContext webDriverContext, BddWishlistSeedDataContext wishlistSeedDataContext)
    {
        _webDriverContext = webDriverContext;
        _wishlistSeedDataContext = wishlistSeedDataContext;
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

        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Account/Login");
        _webDriverContext.Driver.FindElement(By.Id("UsernameOrEmail")).SendKeys(seedData.Username);
        _webDriverContext.Driver.FindElement(By.Id("Password")).SendKeys(seedData.Password);
        _webDriverContext.Driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        Thread.Sleep(1000);
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
        _webDriverContext.Driver!.FindElement(By.Id("addToWishlistBtn")).Click();
        Thread.Sleep(1000);
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
}