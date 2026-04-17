using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class PlaygroupCollectionSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddSeedDataContext _bddSeedDataContext;
    private readonly LoginHelper _loginHelper;

    public PlaygroupCollectionSteps(
        WebDriverContext webDriverContext,
        BddSeedDataContext bddSeedDataContext,
        LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _bddSeedDataContext = bddSeedDataContext;
        _loginHelper = loginHelper;
    }

    private class ResetCollectionDataResponse
    {
        public string MemberUsername { get; set; } = "";
        public string MemberPassword { get; set; } = "";
        public string NonMemberUsername { get; set; } = "";
        public string NonMemberPassword { get; set; } = "";
        public string OwnerUsername { get; set; } = "";
        public int CollectionPlaygroupId { get; set; }
        public int EmptyPlaygroupId { get; set; }
        public string CollectionGameName { get; set; } = "";
    }

    private ResetCollectionDataResponse CallResetEndpoint()
    {
        using var httpClient = new HttpClient();
        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-collection-data", null)
            .GetAwaiter()
            .GetResult();
        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetCollectionDataResponse>()
            .GetAwaiter()
            .GetResult();

        if (seedData == null)
            throw new InvalidOperationException("Failed to read BDD collection seed data response.");

        return seedData;
    }

    [Given("I am a logged-in member of the collection playgroup")]
    public void GivenIAmALoggedInMemberOfTheCollectionPlaygroup()
    {
        var seedData = CallResetEndpoint();

        _bddSeedDataContext.CollectionPlaygroupId = seedData.CollectionPlaygroupId;
        _bddSeedDataContext.EmptyPlaygroupId = seedData.EmptyPlaygroupId;
        _bddSeedDataContext.CollectionGameName = seedData.CollectionGameName;
        _bddSeedDataContext.OwnerUsername = seedData.OwnerUsername;

        _loginHelper.Login(seedData.MemberUsername, seedData.MemberPassword);
    }

    [Given("I am logged in as a non-member")]
    public void GivenIAmLoggedInAsANonMember()
    {
        var seedData = CallResetEndpoint();

        _bddSeedDataContext.CollectionPlaygroupId = seedData.CollectionPlaygroupId;
        _bddSeedDataContext.EmptyPlaygroupId = seedData.EmptyPlaygroupId;
        _bddSeedDataContext.CollectionGameName = seedData.CollectionGameName;
        _bddSeedDataContext.OwnerUsername = seedData.OwnerUsername;

        _loginHelper.Login(seedData.NonMemberUsername, seedData.NonMemberPassword);
    }

    [Given("I am a logged-in member of the empty playgroup")]
    public void GivenIAmALoggedInMemberOfTheEmptyPlaygroup()
    {
        var seedData = CallResetEndpoint();

        _bddSeedDataContext.CollectionPlaygroupId = seedData.CollectionPlaygroupId;
        _bddSeedDataContext.EmptyPlaygroupId = seedData.EmptyPlaygroupId;
        _bddSeedDataContext.CollectionGameName = seedData.CollectionGameName;
        _bddSeedDataContext.OwnerUsername = seedData.OwnerUsername;

        _loginHelper.Login(seedData.MemberUsername, seedData.MemberPassword);
    }

    [When("I navigate to the playgroup details page")]
    public void WhenINavigateToThePlaygroupDetailsPage()
    {
        _webDriverContext.Driver!.Navigate()
            .GoToUrl($"{TestSettings.BaseUrl}/Playgroup/Details/{_bddSeedDataContext.CollectionPlaygroupId}");
    }

    [When("I navigate to the combined collection page directly")]
    public void WhenINavigateToTheCombinedCollectionPageDirectly()
    {
        _webDriverContext.Driver!.Navigate()
            .GoToUrl($"{TestSettings.BaseUrl}/Playgroup/Collection/{_bddSeedDataContext.CollectionPlaygroupId}");
    }

    [When("I navigate to the empty playgroup collection page directly")]
    public void WhenINavigateToTheEmptyPlaygroupCollectionPageDirectly()
    {
        _webDriverContext.Driver!.Navigate()
            .GoToUrl($"{TestSettings.BaseUrl}/Playgroup/Collection/{_bddSeedDataContext.EmptyPlaygroupId}");
    }

    [Then("the View Combined Collection link is visible")]
    public void ThenTheViewCombinedCollectionLinkIsVisible()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5));
        var found = wait.Until(d =>
        {
            var links = d.FindElements(By.TagName("a"));
            return links.Any(a => a.Text.Contains("View Combined Collection",
                StringComparison.OrdinalIgnoreCase));
        });
        Assert.That(found, Is.True, "Expected 'View Combined Collection' link to be visible.");
    }

    [When("I click the View Combined Collection link")]
    public void WhenIClickTheViewCombinedCollectionLink()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        var link = wait.Until(d =>
        {
            var links = d.FindElements(By.TagName("a"));
            return links.FirstOrDefault(a => a.Text.Contains("View Combined Collection",
                StringComparison.OrdinalIgnoreCase));
        });
        Assert.That(link, Is.Not.Null, "'View Combined Collection' link was not found.");
        link!.Click();
    }

    [Then("the combined collection page loads successfully")]
    public void ThenTheCombinedCollectionPageLoadsSuccessfully()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5));
        wait.Until(d => d.Url.Contains("/Playgroup/Collection/"));
        Assert.That(_webDriverContext.Driver!.Url, Does.Contain("/Playgroup/Collection/"));
    }

    [Then("at least one game is listed")]
    public void ThenAtLeastOneGameIsListed()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain(_bddSeedDataContext.CollectionGameName));
    }

    [Then("the owner username is displayed next to the game")]
    public void ThenTheOwnerUsernameIsDisplayedNextToTheGame()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain(_bddSeedDataContext.OwnerUsername));
    }

    [Then("I see a not found page")]
    public void ThenISeeANotFoundPage()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(
            body.Contains("404") || body.Contains("Not Found") || body.Contains("not found"),
            Is.True,
            "Expected a 404 or not found page.");
    }

    [Then("I see the empty collection message")]
    public void ThenISeeTheEmptyCollectionMessage()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain("No games owned yet"));
    }
}
