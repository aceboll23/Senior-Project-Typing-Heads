using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class GameVotingSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddSeedDataContext _bddSeedDataContext;

    public GameVotingSteps(WebDriverContext webDriverContext, BddSeedDataContext bddSeedDataContext)
    {
        _webDriverContext = webDriverContext;
        _bddSeedDataContext = bddSeedDataContext;
    }

    private class ResetVotingDataResponse
    {
        public string CreatorUsername { get; set; } = "";
        public string CreatorPassword { get; set; } = "";
        public string MemberUsername { get; set; } = "";
        public string MemberPassword { get; set; } = "";
        public int EventId { get; set; }
        public int EventGameId { get; set; }
        public string GameName { get; set; } = "";
    }

    private ResetVotingDataResponse CallResetEndpoint()
    {
        using var httpClient = new HttpClient();
        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-voting-data", null)
            .GetAwaiter()
            .GetResult();
        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetVotingDataResponse>()
            .GetAwaiter()
            .GetResult();

        if (seedData == null)
            throw new InvalidOperationException("Failed to read BDD voting seed data.");

        return seedData;
    }

    private void LoginAs(string username, string password)
    {
        var driver = _webDriverContext.Driver!;
        driver.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Account/Login");
        driver.FindElement(By.Id("UsernameOrEmail")).SendKeys(username);
        driver.FindElement(By.Id("Password")).SendKeys(password);
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        System.Threading.Thread.Sleep(1000);
    }

    private void OpenVotingViaEndpoint()
    {
        using var httpClient = new HttpClient();
        // Use the app's seed endpoint to open voting directly in the DB
        // so we don't have to navigate through the UI for setup steps
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{TestSettings.BaseUrl}/dev/bdd/open-voting/{_bddSeedDataContext.VotingEventId}");
        httpClient.SendAsync(request).GetAwaiter().GetResult();
    }

    [Given("I am a logged-in event creator for voting")]
    public void GivenIAmALoggedInEventCreatorForVoting()
    {
        var seedData = CallResetEndpoint();

        _bddSeedDataContext.VotingEventId = seedData.EventId;
        _bddSeedDataContext.VotingEventGameId = seedData.EventGameId;
        _bddSeedDataContext.VotingGameName = seedData.GameName;
        _bddSeedDataContext.VotingCreatorUsername = seedData.CreatorUsername;
        _bddSeedDataContext.VotingCreatorPassword = seedData.CreatorPassword;
        _bddSeedDataContext.VotingMemberUsername = seedData.MemberUsername;
        _bddSeedDataContext.VotingMemberPassword = seedData.MemberPassword;

        LoginAs(seedData.CreatorUsername, seedData.CreatorPassword);
    }

    [Given("I am a logged-in event member for voting")]
    public void GivenIAmALoggedInEventMemberForVoting()
    {
        var seedData = CallResetEndpoint();

        _bddSeedDataContext.VotingEventId = seedData.EventId;
        _bddSeedDataContext.VotingEventGameId = seedData.EventGameId;
        _bddSeedDataContext.VotingGameName = seedData.GameName;
        _bddSeedDataContext.VotingCreatorUsername = seedData.CreatorUsername;
        _bddSeedDataContext.VotingCreatorPassword = seedData.CreatorPassword;
        _bddSeedDataContext.VotingMemberUsername = seedData.MemberUsername;
        _bddSeedDataContext.VotingMemberPassword = seedData.MemberPassword;

        LoginAs(seedData.MemberUsername, seedData.MemberPassword);
    }

    [Given("voting is already open for the event")]
    public void GivenVotingIsAlreadyOpenForTheEvent()
    {
        using var httpClient = new HttpClient();
        var response = httpClient
            .PostAsync(
                $"{TestSettings.BaseUrl}/dev/bdd/open-voting/{_bddSeedDataContext.VotingEventId}",
                null)
            .GetAwaiter()
            .GetResult();

        response.EnsureSuccessStatusCode();
    }

    [When("I navigate to the voting event details page")]
    public void WhenINavigateToTheVotingEventDetailsPage()
    {
        _webDriverContext.Driver!.Navigate()
            .GoToUrl($"{TestSettings.BaseUrl}/GameNightEvent/Details/{_bddSeedDataContext.VotingEventId}");
    }

    [When("I click the Open Voting button")]
    public void WhenIClickTheOpenVotingButton()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var button = wait.Until(d =>
        {
            var buttons = d.FindElements(By.CssSelector("button[type='submit']"));
            return buttons.FirstOrDefault(b =>
                b.Text.Contains("Open Voting", StringComparison.OrdinalIgnoreCase));
        });

        Assert.That(button, Is.Not.Null, "Open Voting button was not found.");
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", button);
    }

    [When("I click the Close Voting button")]
    public void WhenIClickTheCloseVotingButton()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        // Pre-accept any confirm dialogs
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "window.confirm = function() { return true; }");

        var button = wait.Until(d =>
        {
            var buttons = d.FindElements(By.CssSelector("button[type='submit']"));
            return buttons.FirstOrDefault(b =>
                b.Text.Contains("Close Voting", StringComparison.OrdinalIgnoreCase));
        });

        Assert.That(button, Is.Not.Null, "Close Voting button was not found.");
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center' });", button);
        System.Threading.Thread.Sleep(300);
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", button);
        System.Threading.Thread.Sleep(1000);
    }

    [When("I enter a ranking of 1 for the first game")]
    public void WhenIEnterARankingOf1ForTheFirstGame()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var rankInput = wait.Until(d =>
            d.FindElements(By.CssSelector("input[type='number']"))
             .FirstOrDefault());

        Assert.That(rankInput, Is.Not.Null, "Rank input was not found.");
        rankInput!.Clear();
        rankInput.SendKeys("1");
    }

    [When("I submit my rankings")]
    public void WhenISubmitMyRankings()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var button = wait.Until(d =>
        {
            var buttons = d.FindElements(By.CssSelector("button[type='submit']"));
            return buttons.FirstOrDefault(b =>
                b.Text.Contains("Submit Rankings", StringComparison.OrdinalIgnoreCase));
        });

        Assert.That(button, Is.Not.Null, "Submit Rankings button was not found.");
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", button);
        System.Threading.Thread.Sleep(1000);
    }

    [Then("voting is shown as open")]
    public void ThenVotingIsShownAsOpen()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        wait.Until(d => d.PageSource.Contains("Voting Open",
            StringComparison.OrdinalIgnoreCase));

        Assert.That(driver.PageSource,
            Does.Contain("Voting Open").IgnoreCase);
    }

    [Then("my rankings are saved successfully")]
    public void ThenMyRankingsAreSavedSuccessfully()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        wait.Until(d => d.PageSource.Contains("rankings have been saved",
            StringComparison.OrdinalIgnoreCase));

        Assert.That(driver.PageSource,
            Does.Contain("rankings have been saved").IgnoreCase);
    }

    [Then("voting results are displayed")]
    public void ThenVotingResultsAreDisplayed()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        wait.Until(d => d.PageSource.Contains("Voting Closed",
            StringComparison.OrdinalIgnoreCase));

        Assert.That(driver.PageSource,
            Does.Contain("Voting Closed").IgnoreCase);
    }

    [Then("the winning game is highlighted")]
    public void ThenTheWinningGameIsHighlighted()
    {
        var driver = _webDriverContext.Driver!;
        Assert.That(driver.PageSource, Does.Contain("Winner").IgnoreCase);
    }
}