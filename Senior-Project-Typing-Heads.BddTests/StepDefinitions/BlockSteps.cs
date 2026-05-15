using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class BlockSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddBlockSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    private class ResetBlockDataResponse
    {
        public string BlockerUsername { get; set; } = "";
        public string BlockerPassword { get; set; } = "";
        public string TargetUsername { get; set; } = "";
        public string TargetPassword { get; set; } = "";
        public string TargetPostContent { get; set; } = "";
    }

    public BlockSteps(WebDriverContext webDriverContext, BddBlockSeedDataContext seedContext, LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private void ResetFreshAndLogin(bool asTarget = false)
    {
        using var httpClient = new HttpClient();
        var response = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-block-data", null)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var data = response.Content.ReadFromJsonAsync<ResetBlockDataResponse>().GetAwaiter().GetResult();
        ArgumentNullException.ThrowIfNull(data);

        _seedContext.BlockerUsername = data.BlockerUsername;
        _seedContext.BlockerPassword = data.BlockerPassword;
        _seedContext.TargetUsername = data.TargetUsername;
        _seedContext.TargetPassword = data.TargetPassword;

        _loginHelper.Login(
            asTarget ? _seedContext.TargetUsername : _seedContext.BlockerUsername,
            asTarget ? _seedContext.TargetPassword : _seedContext.BlockerPassword);
    }

    private void ResetPreBlockedAndLogin(bool asTarget = false)
    {
        using var httpClient = new HttpClient();
        var response = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-block-data-preblocked", null)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var data = response.Content.ReadFromJsonAsync<ResetBlockDataResponse>().GetAwaiter().GetResult();
        ArgumentNullException.ThrowIfNull(data);

        _seedContext.BlockerUsername = data.BlockerUsername;
        _seedContext.BlockerPassword = data.BlockerPassword;
        _seedContext.TargetUsername = data.TargetUsername;
        _seedContext.TargetPassword = data.TargetPassword;
        _seedContext.TargetPostContent = data.TargetPostContent;

        _loginHelper.Login(
            asTarget ? _seedContext.TargetUsername : _seedContext.BlockerUsername,
            asTarget ? _seedContext.TargetPassword : _seedContext.BlockerPassword);
    }

    [Given("I am logged in as the block test blocker with a friendship")]
    public void GivenIAmLoggedInAsTheBlockTestBlockerWithAFriendship()
    {
        ResetFreshAndLogin();
    }

    [Given("I am logged in as the block test target who has been blocked")]
    public void GivenIAmLoggedInAsTheBlockTestTargetWhoHasBeenBlocked()
    {
        ResetPreBlockedAndLogin(asTarget: true);
    }

    [Given("I am logged in as the block test blocker with a pre-existing block")]
    public void GivenIAmLoggedInAsTheBlockTestBlockerWithPreExistingBlock()
    {
        ResetPreBlockedAndLogin();
    }

    [Given("I am on the target user's profile page")]
    public void GivenIAmOnTheTargetUsersProfilePage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Profile/{_seedContext.TargetUsername}");
    }

    [When("I click the Block User button")]
    public void WhenIClickTheBlockUserButton()
    {
        var btn = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5))
            .Until(d => d.FindElement(By.Id("block-user-btn")));
        btn.Click();

        // Handle the confirm dialog
        new WebDriverWait(_webDriverContext.Driver, TimeSpan.FromSeconds(5))
            .Until(d => {
                try { d.SwitchTo().Alert(); return true; }
                catch { return false; }
            });
        _webDriverContext.Driver!.SwitchTo().Alert().Accept();

        // Wait for the page to reflect the block result (success message or Blocked badge)
        new WebDriverWait(_webDriverContext.Driver, TimeSpan.FromSeconds(10))
            .Until(d => d.PageSource.Contains("has been blocked", StringComparison.OrdinalIgnoreCase)
                     || d.PageSource.Contains("Blocked", StringComparison.Ordinal));
    }

    [Then("I should see a block success message")]
    public void ThenIShouldSeeABlockSuccessMessage()
    {
        Assert.That(_webDriverContext.Driver!.PageSource,
            Does.Contain("has been blocked").IgnoreCase);
    }

    [When("I navigate to the blocker's profile")]
    public void WhenINavigateToTheBlockersProfile()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Profile/{_seedContext.BlockerUsername}");
    }

    [Then("I should see a profile not available message")]
    public void ThenIShouldSeeAProfileNotAvailableMessage()
    {
        Assert.That(_webDriverContext.Driver!.PageSource,
            Does.Contain("not available").IgnoreCase);
    }

    [Then("I should not see the blocked user's post in the feed")]
    public void ThenIShouldNotSeeTheBlockedUsersPostInTheFeed()
    {
        Assert.That(_webDriverContext.Driver!.PageSource,
            Does.Not.Contain(_seedContext.TargetPostContent));
    }

    [Then("the target user should no longer appear in my friends list")]
    public void ThenTheTargetUserShouldNoLongerAppearInMyFriendsList()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Friends");
        Assert.That(_webDriverContext.Driver!.PageSource,
            Does.Not.Contain(_seedContext.TargetUsername));
    }

    [When("I navigate to the blocked users page")]
    public void WhenINavigateToTheBlockedUsersPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Settings/BlockedUsers");
    }

    [Then("I should see the blocked user in the list")]
    public void ThenIShouldSeeTheBlockedUserInTheList()
    {
        Assert.That(_webDriverContext.Driver!.PageSource,
            Does.Contain(_seedContext.TargetUsername));
    }

    [When("I click the Unblock button for the target user")]
    public void WhenIClickTheUnblockButtonForTheTargetUser()
    {
        var btn = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5))
            .Until(d => d.FindElement(By.Id($"unblock-btn-{_seedContext.TargetUsername}")));
        btn.Click();

        // Wait until the unblock button is gone (redirect completed and user removed from list)
        new WebDriverWait(_webDriverContext.Driver, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.Id($"unblock-btn-{_seedContext.TargetUsername}")).Count == 0);
    }

    [Then("the target user should no longer appear in the blocked list")]
    public void ThenTheTargetUserShouldNoLongerAppearInTheBlockedList()
    {
        Assert.That(_webDriverContext.Driver!.PageSource,
            Does.Not.Contain(_seedContext.TargetUsername));
    }
}
