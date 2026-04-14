using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class PlaygroupSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddPlaygroupSeedDataContext _playgroupSeedDataContext;
    private string _createdPlaygroupName = string.Empty;

    private class ResetPlaygroupDataResponse
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public PlaygroupSteps(WebDriverContext webDriverContext, BddPlaygroupSeedDataContext playgroupSeedDataContext)
    {
        _webDriverContext = webDriverContext;
        _playgroupSeedDataContext = playgroupSeedDataContext;
    }

    [Given("I am logged in as PersonThree")]
    public void GivenIAmLoggedInAsPersonThree()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-playgroup-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetPlaygroupDataResponse>()
            .GetAwaiter()
            .GetResult();

        ArgumentNullException.ThrowIfNull(seedData, "Failed to read BDD playgroup seed data response.");

        _playgroupSeedDataContext.Username = seedData.Username;
        _playgroupSeedDataContext.Password = seedData.Password;

        var driver = _webDriverContext.Driver!;
        driver.Navigate().GoToUrl(TestSettings.BaseUrl + "/Account/Login");
        driver.FindElement(By.Id("UsernameOrEmail")).SendKeys(seedData.Username);
        driver.FindElement(By.Id("Password")).SendKeys(seedData.Password);
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        System.Threading.Thread.Sleep(1000);
    }

    [When("I navigate to the Create Playgroup page")]
    public void WhenINavigateToTheCreatePlaygroupPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(TestSettings.BaseUrl + "/Playgroup/Create");
    }

    [Then("the Create Playgroup form is displayed")]
    public void ThenTheCreatePlaygroupFormIsDisplayed()
    {
        var form = _webDriverContext.Driver!.FindElement(By.TagName("form"));
        Assert.That(form, Is.Not.Null);
    }

    [When("I fill in the Create Playgroup form with a unique name")]
    public void WhenIFillInTheCreatePlaygroupFormWithAUniqueName()
    {
        _createdPlaygroupName = "Test Group BDD " + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        _webDriverContext.Driver!.FindElement(By.Id("name")).SendKeys(_createdPlaygroupName);
    }

    [When("I submit the Create Playgroup form")]
    public void WhenISubmitTheCreatePlaygroupForm()
    {
        _webDriverContext.Driver!.FindElement(By.CssSelector(".btn-primary[type='submit']")).Click();
        System.Threading.Thread.Sleep(1000);
    }

    [Then("I am redirected to the playgroup detail page")]
    public void ThenIAmRedirectedToThePlaygroupDetailPage()
    {
        Assert.That(_webDriverContext.Driver!.Url, Does.Contain("/Playgroup/Details"));
    }

    [Then("the created playgroup name is displayed")]
    public void ThenTheCreatedPlaygroupNameIsDisplayed()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain(_createdPlaygroupName));
    }
}