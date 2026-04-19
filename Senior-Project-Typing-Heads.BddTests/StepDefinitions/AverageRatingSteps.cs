using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using System.Net.Http.Json;
using System.Globalization;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class AverageRatingSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddAverageRatingSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    public AverageRatingSteps(
        WebDriverContext webDriverContext,
        BddAverageRatingSeedDataContext seedContext,
        LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private class ResetAverageRatingDataResponse
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int GameId { get; set; }
        public decimal ExpectedAverage { get; set; }
    }

    [Given("I am logged in as a BDD average rating user")]
    public void GivenIAmLoggedInAsABddAverageRatingUser()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-average-rating-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetAverageRatingDataResponse>()
            .GetAwaiter()
            .GetResult();

        ArgumentNullException.ThrowIfNull(seedData, "Failed to read BDD average rating seed data response.");

        _seedContext.GameId = seedData.GameId;
        _seedContext.ExpectedAverage = seedData.ExpectedAverage;

        _loginHelper.Login(seedData.Username, seedData.Password);
    }

    [When("I navigate to the game details page")]
    public void WhenINavigateToTheGameDetailsPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/Games/Details/{_seedContext.GameId}");
    }

    [Then("I see the average user rating displayed")]
    public void ThenISeeTheAverageUserRatingDisplayed()
    {
        var driver = _webDriverContext.Driver!;

        var expectedText = _seedContext.ExpectedAverage.ToString("0.0", CultureInfo.InvariantCulture);

        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d =>
            {
                var el = d.FindElements(By.Id("average-user-rating")).FirstOrDefault();
                return el != null && el.Text.Contains(expectedText);
            });

        var element = driver.FindElement(By.Id("average-user-rating"));
        Assert.That(element.Text, Does.Contain(expectedText),
            $"Expected average-user-rating element to contain '{expectedText}' but was: '{element.Text}'");
    }
}
