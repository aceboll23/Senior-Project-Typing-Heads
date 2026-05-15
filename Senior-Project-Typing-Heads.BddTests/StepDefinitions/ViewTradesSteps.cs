using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class ViewTradesSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddViewTradesSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    private class ResetViewTradesResponse
    {
        public string ViewerUsername { get; set; } = "";
        public string ViewerPassword { get; set; } = "";
        public string FriendWithTradesUsername { get; set; } = "";
        public string FriendNoTradesUsername { get; set; } = "";
        public string StrangerUsername { get; set; } = "";
        public string TradeGameName { get; set; } = "";
        public string NonTradeGameName { get; set; } = "";
    }

    public ViewTradesSteps(WebDriverContext webDriverContext, BddViewTradesSeedDataContext seedContext, LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private void ResetAndLogin()
    {
        using var httpClient = new HttpClient();
        var response = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-view-trades-data", null)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var data = response.Content.ReadFromJsonAsync<ResetViewTradesResponse>().GetAwaiter().GetResult();
        ArgumentNullException.ThrowIfNull(data);

        _seedContext.ViewerUsername = data.ViewerUsername;
        _seedContext.ViewerPassword = data.ViewerPassword;
        _seedContext.FriendWithTradesUsername = data.FriendWithTradesUsername;
        _seedContext.FriendNoTradesUsername = data.FriendNoTradesUsername;
        _seedContext.StrangerUsername = data.StrangerUsername;
        _seedContext.TradeGameName = data.TradeGameName;
        _seedContext.NonTradeGameName = data.NonTradeGameName;

        _loginHelper.Login(_seedContext.ViewerUsername, _seedContext.ViewerPassword);
    }

    [Given("I am logged in as the view-trades viewer")]
    public void GivenIAmLoggedInAsTheViewTradesViewer()
    {
        ResetAndLogin();
    }

    [When("I navigate to my trade friend's profile")]
    [Given("I navigate to my trade friend's profile")]
    public void WhenINavigateToMyTradeFriendsProfile()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Profile/{_seedContext.FriendWithTradesUsername}");
    }

    [Then("I should see a View Available Trades button")]
    public void ThenIShouldSeeAViewAvailableTradesButton()
    {
        var btn = _webDriverContext.Driver!.FindElements(By.Id("view-available-trades-btn"));
        Assert.That(btn, Is.Not.Empty);
    }

    [When("I click the View Available Trades button")]
    public void WhenIClickTheViewAvailableTradesButton()
    {
        var btn = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5))
            .Until(d => d.FindElement(By.Id("view-available-trades-btn")));
        btn.Click();
    }

    [Then("I should be on my trade friend's available trades page")]
    public void ThenIShouldBeOnMyTradeFriendsAvailableTradesPage()
    {
        new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5))
            .Until(d => d.Url.Contains($"/collection/trades/{_seedContext.FriendWithTradesUsername}", StringComparison.OrdinalIgnoreCase));
        Assert.That(_webDriverContext.Driver!.Url, Does.Contain($"/collection/trades/{_seedContext.FriendWithTradesUsername}").IgnoreCase);
    }

    [When("I navigate to my trade friend's available trades page")]
    public void WhenINavigateToMyTradeFriendsAvailableTradesPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection/trades/{_seedContext.FriendWithTradesUsername}");
    }

    [Then("I should see the tradeable game on the page")]
    public void ThenIShouldSeeTheTradeableGameOnThePage()
    {
        var pageSource = _webDriverContext.Driver!.PageSource;
        Assert.That(pageSource, Does.Contain(_seedContext.TradeGameName));
    }

    [Then("I should not see the non-tradeable game on the page")]
    public void ThenIShouldNotSeeTheNonTradeableGameOnThePage()
    {
        var pageSource = _webDriverContext.Driver!.PageSource;
        Assert.That(pageSource, Does.Not.Contain(_seedContext.NonTradeGameName));
    }

    [When("I navigate to the no-trades friend's available trades page")]
    public void WhenINavigateToTheNoTradesFriendsAvailableTradesPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection/trades/{_seedContext.FriendNoTradesUsername}");
    }

    [Then("I should see a message that no games are available for trade")]
    public void ThenIShouldSeeAMessageThatNoGamesAreAvailableForTrade()
    {
        var emptyMsg = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5))
            .Until(d => d.FindElements(By.Id("trades-empty-msg")).Count > 0);
        Assert.That(emptyMsg, Is.True);
    }

    [When("I attempt to view a stranger's available trades")]
    public void WhenIAttemptToViewAStrangersAvailableTrades()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection/trades/{_seedContext.StrangerUsername}");
    }

    [Then("access to the trade page should be denied")]
    public void ThenAccessToTheTradepageShouldBeDenied()
    {
        Assert.That(_webDriverContext.Driver!.Url, Does.Contain("AccessDenied").IgnoreCase);
    }

    [When("I attempt to view a user's trades without logging in")]
    public void WhenIAttemptToViewAUserTradesWithoutLoggingIn()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection/trades/anyone");
    }
}
