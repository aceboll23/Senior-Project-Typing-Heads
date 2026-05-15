using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class TradeSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddTradeSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    private class ResetTradeDataResponse
    {
        public string OwnerUsername { get; set; } = "";
        public string OwnerPassword { get; set; } = "";
        public int GameId { get; set; }
        public string GameName { get; set; } = "";
    }

    public TradeSteps(WebDriverContext webDriverContext, BddTradeSeedDataContext seedContext, LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private void ResetFreshAndLogin()
    {
        using var httpClient = new HttpClient();
        var response = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-trade-data", null)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var data = response.Content.ReadFromJsonAsync<ResetTradeDataResponse>().GetAwaiter().GetResult();
        ArgumentNullException.ThrowIfNull(data);

        _seedContext.OwnerUsername = data.OwnerUsername;
        _seedContext.OwnerPassword = data.OwnerPassword;
        _seedContext.GameId = data.GameId;
        _seedContext.GameName = data.GameName;

        _loginHelper.Login(_seedContext.OwnerUsername, _seedContext.OwnerPassword);
    }

    private void ResetPreTradedAndLogin()
    {
        using var httpClient = new HttpClient();
        var response = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-trade-data-pretraded", null)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var data = response.Content.ReadFromJsonAsync<ResetTradeDataResponse>().GetAwaiter().GetResult();
        ArgumentNullException.ThrowIfNull(data);

        _seedContext.OwnerUsername = data.OwnerUsername;
        _seedContext.OwnerPassword = data.OwnerPassword;
        _seedContext.GameId = data.GameId;
        _seedContext.GameName = data.GameName;

        _loginHelper.Login(_seedContext.OwnerUsername, _seedContext.OwnerPassword);
    }

    [Given("I am logged in as the trade test owner with a game in their collection")]
    public void GivenIAmLoggedInAsTheTradeTestOwnerWithAGameInTheirCollection()
    {
        ResetFreshAndLogin();
    }

    [Given("I am logged in as the trade test owner with a game marked as available for trade")]
    public void GivenIAmLoggedInAsTheTradeTestOwnerWithAGameMarkedAsAvailableForTrade()
    {
        ResetPreTradedAndLogin();
    }

    [Given("I am not logged in as any user")]
    public void GivenIAmNotLoggedInAsAnyUser()
    {
        _webDriverContext.Driver!.Manage().Cookies.DeleteAllCookies();
    }

    [When("I click the Mark as Trade button for my game")]
    public void WhenIClickTheMarkAsTradeButtonForMyGame()
    {
        var btn = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5))
            .Until(d => d.FindElement(By.Id($"mark-trade-btn-{_seedContext.GameId}")));
        btn.Click();

        new WebDriverWait(_webDriverContext.Driver, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.Id($"mark-trade-btn-{_seedContext.GameId}")).Count == 0);
    }

    [When("I click the Remove from Trade button for my game")]
    public void WhenIClickTheRemoveFromTradeButtonForMyGame()
    {
        var btn = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5))
            .Until(d => d.FindElement(By.Id($"remove-trade-btn-{_seedContext.GameId}")));
        btn.Click();

        new WebDriverWait(_webDriverContext.Driver, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.Id($"remove-trade-btn-{_seedContext.GameId}")).Count == 0);
    }

    [Then("I should see a Mark as Trade button for my game")]
    public void ThenIShouldSeeAMarkAsTradeButtonForMyGame()
    {
        var btn = _webDriverContext.Driver!.FindElements(By.Id($"mark-trade-btn-{_seedContext.GameId}"));
        Assert.That(btn, Is.Not.Empty);
    }

    [Then("I should see the Available for Trade badge for my game")]
    public void ThenIShouldSeeTheAvailableForTradeBadgeForMyGame()
    {
        var badge = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5))
            .Until(d => d.FindElements(By.Id($"trade-badge-{_seedContext.GameId}")).Count > 0);
        Assert.That(badge, Is.True);
    }

    [Then("I should not see the Available for Trade badge for my game")]
    public void ThenIShouldNotSeeTheAvailableForTradeBadgeForMyGame()
    {
        var badges = _webDriverContext.Driver!.FindElements(By.Id($"trade-badge-{_seedContext.GameId}"));
        Assert.That(badges, Is.Empty);
    }

    [Then("I should be redirected to the login page")]
    public void ThenIShouldBeRedirectedToTheLoginPage()
    {
        Assert.That(_webDriverContext.Driver!.Url, Does.Contain("/Account/Login").IgnoreCase);
    }
}
