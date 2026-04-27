using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class DeleteCollectionSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddDeleteCollectionSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    public DeleteCollectionSteps(
        WebDriverContext webDriverContext,
        BddDeleteCollectionSeedDataContext seedContext,
        LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private class ResetDeleteCollectionDataResponse
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int OwnedGameId { get; set; }
        public string OwnedGameName { get; set; } = "";
    }

    [Given("I am logged in as a BDD delete collection user")]
    public void GivenIAmLoggedInAsABddDeleteCollectionUser()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-delete-collection-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetDeleteCollectionDataResponse>()
            .GetAwaiter()
            .GetResult();

        ArgumentNullException.ThrowIfNull(seedData, "Failed to read BDD delete-collection seed data response.");

        _seedContext.OwnedGameId = seedData.OwnedGameId;
        _seedContext.OwnedGameName = seedData.OwnedGameName;

        _loginHelper.Login(seedData.Username, seedData.Password);
    }

    [When("I click the Remove from Collection button")]
    public void WhenIClickTheRemoveFromCollectionButton()
    {
        var driver = _webDriverContext.Driver!;
        var buttonId = $"remove-collection-btn-{_seedContext.OwnedGameId}";

        var button = driver.FindElement(By.Id(buttonId));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center' });", button);

        // The form has an onsubmit confirm() — override window.confirm to auto-accept so
        // the form submits without a real dialog popping up in the headless run.
        ((IJavaScriptExecutor)driver).ExecuteScript("window.confirm = function() { return true; };");

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].click();", button);
    }

    [Then("the game no longer appears in my collection")]
    public void ThenTheGameNoLongerAppearsInMyCollection()
    {
        var driver = _webDriverContext.Driver!;

        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d => !d.PageSource.Contains(_seedContext.OwnedGameName));

        var body = driver.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Not.Contain(_seedContext.OwnedGameName),
            "Removed game should no longer appear anywhere on the collection page.");
    }
}
