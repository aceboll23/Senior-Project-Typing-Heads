using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class AllFriendTradesSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddAllFriendTradesSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    private class ResetAllFriendTradesResponse
    {
        public string ViewerUsername { get; set; } = "";
        public string ViewerPassword { get; set; } = "";
        public string Friend1Username { get; set; } = "";
        public string Friend2Username { get; set; } = "";
        public string Friend1TradeGameName { get; set; } = "";
        public string Friend2TradeGameName { get; set; } = "";
        public string NoTradeGameName { get; set; } = "";
        public string StrangerUsername { get; set; } = "";
    }

    public AllFriendTradesSteps(
        WebDriverContext webDriverContext,
        BddAllFriendTradesSeedDataContext seedContext,
        LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private void ResetAndLogin()
    {
        using var httpClient = new HttpClient();
        var response = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-all-friend-trades-data", null)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var data = response.Content.ReadFromJsonAsync<ResetAllFriendTradesResponse>().GetAwaiter().GetResult();
        ArgumentNullException.ThrowIfNull(data);

        _seedContext.ViewerUsername = data.ViewerUsername;
        _seedContext.ViewerPassword = data.ViewerPassword;
        _seedContext.Friend1Username = data.Friend1Username;
        _seedContext.Friend2Username = data.Friend2Username;
        _seedContext.Friend1TradeGameName = data.Friend1TradeGameName;
        _seedContext.Friend2TradeGameName = data.Friend2TradeGameName;
        _seedContext.NoTradeGameName = data.NoTradeGameName;
        _seedContext.StrangerUsername = data.StrangerUsername;

        _loginHelper.Login(_seedContext.ViewerUsername, _seedContext.ViewerPassword);
    }

    [Given("I am logged in as the all-friend-trades viewer")]
    public void GivenIAmLoggedInAsTheAllFriendTradesViewer()
    {
        ResetAndLogin();
    }

    [Then("I should see a Friends Trades link in the nav")]
    public void ThenIShouldSeeAFriendsTradesLinkInTheNav()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5));
        var link = wait.Until(d => d.FindElement(By.Id("nav-friends-trades-link")));
        Assert.That(link, Is.Not.Null);
    }

    [When("I navigate to the Friends Trades page")]
    public void WhenINavigateToTheFriendsTradesPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection/all-friend-trades");
    }

    [When("I navigate to the all friend trades page directly")]
    public void WhenINavigateToTheAllFriendTradesPageDirectly()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection/all-friend-trades");
    }

    [Then("I should see friend1's tradeable game on the page")]
    public void ThenIShouldSeeFriend1TradeableGameOnThePage()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        wait.Until(d => d.PageSource.Contains(_seedContext.Friend1TradeGameName));
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(_seedContext.Friend1TradeGameName));
    }

    [Then("I should see friend2's tradeable game on the page")]
    public void ThenIShouldSeeFriend2TradeableGameOnThePage()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(_seedContext.Friend2TradeGameName));
    }

    [Then("I should not see the non-tradeable game on the all-trades page")]
    public void ThenIShouldNotSeeTheNonTradeableGameOnTheAllTradesPage()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Not.Contain(_seedContext.NoTradeGameName));
    }

    [Then("I should see a message button for friend1's game")]
    public void ThenIShouldSeeAMessageButtonForFriend1sGame()
    {
        var pageSource = _webDriverContext.Driver!.PageSource;
        Assert.That(pageSource, Does.Contain(_seedContext.Friend1Username).IgnoreCase);
    }
}
