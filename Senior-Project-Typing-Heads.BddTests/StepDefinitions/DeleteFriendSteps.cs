using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using System.Net.Http.Json;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class DeleteFriendSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddDeleteFriendSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    public DeleteFriendSteps(WebDriverContext webDriverContext, BddDeleteFriendSeedDataContext seedContext, LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private class ResetDeleteFriendDataResponse
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FriendUsername { get; set; } = "";
        public int FriendProfileId { get; set; }
    }

    [Given("I am logged in as a BDD delete friend user")]
    public void GivenIAmLoggedInAsABddDeleteFriendUser()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-delete-friend-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetDeleteFriendDataResponse>()
            .GetAwaiter()
            .GetResult();

        ArgumentNullException.ThrowIfNull(seedData, "Failed to read BDD delete friend seed data response.");

        _seedContext.FriendUsername = seedData.FriendUsername;
        _seedContext.FriendProfileId = seedData.FriendProfileId;

        _loginHelper.Login(seedData.Username, seedData.Password);
    }

    [When("I navigate to my friends page")]
    public void WhenINavigateToMyFriendsPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Friends");
    }

    [When("I click the Remove Friend button")]
    public void WhenIClickTheRemoveFriendButton()
    {
        var driver = _webDriverContext.Driver!;
        var buttonId = $"remove-friend-btn-{_seedContext.FriendProfileId}";

        var button = driver.FindElement(By.Id(buttonId));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center' });", button);

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].click();", button);
    }

    [Then("the friend no longer appears in my friends list")]
    public void ThenTheFriendNoLongerAppearsInMyFriendsList()
    {
        var driver = _webDriverContext.Driver!;

        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d => !d.PageSource.Contains(_seedContext.FriendUsername));

        var body = driver.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Not.Contain(_seedContext.FriendUsername),
            "Removed friend should no longer appear in the friends list.");
    }
}
