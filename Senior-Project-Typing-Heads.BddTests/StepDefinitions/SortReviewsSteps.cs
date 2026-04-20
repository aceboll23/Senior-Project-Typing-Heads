using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using System.Net.Http.Json;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class SortReviewsSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddSortReviewsSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    public SortReviewsSteps(
        WebDriverContext webDriverContext,
        BddSortReviewsSeedDataContext seedContext,
        LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private class ResetSortReviewsDataResponse
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int GameId { get; set; }
    }

    [Given("I am logged in as a BDD sort reviews user")]
    public void GivenIAmLoggedInAsABddSortReviewsUser()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-sort-reviews-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetSortReviewsDataResponse>()
            .GetAwaiter()
            .GetResult();

        ArgumentNullException.ThrowIfNull(seedData, "Failed to read BDD sort reviews seed data response.");

        _seedContext.GameId = seedData.GameId;

        _loginHelper.Login(seedData.Username, seedData.Password);
    }

    [When("I navigate to the sort reviews game details page")]
    public void WhenINavigateToTheSortReviewsGameDetailsPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/Games/Details/{_seedContext.GameId}");
    }

    [When("I select {string} from the review sort dropdown")]
    public void WhenISelectFromTheReviewSortDropdown(string visibleText)
    {
        var driver = _webDriverContext.Driver!;

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        var dropdown = wait.Until(d => d.FindElements(By.Id("review-sort")).FirstOrDefault());

        var select = new SelectElement(dropdown);
        select.SelectByText(visibleText);

        wait.Until(d => d.FindElements(By.CssSelector(".review-card .review-rating")).Count == 3);
    }

    [Then("the first review shown has rating (.*)")]
    public void ThenTheFirstReviewShownHasRating(int rating)
    {
        var driver = _webDriverContext.Driver!;

        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d =>
            {
                var badges = d.FindElements(By.CssSelector(".review-card .review-rating"));
                return badges.Count == 3 && badges[0].Text.Contains($"{rating} /");
            });

        var first = driver.FindElements(By.CssSelector(".review-card .review-rating")).First();
        Assert.That(first.Text, Does.Contain($"{rating} /"),
            $"Expected first review rating to contain '{rating} /' but was: '{first.Text}'");
    }

    [Then("the last review shown has rating (.*)")]
    public void ThenTheLastReviewShownHasRating(int rating)
    {
        var driver = _webDriverContext.Driver!;

        var last = driver.FindElements(By.CssSelector(".review-card .review-rating")).Last();
        Assert.That(last.Text, Does.Contain($"{rating} /"),
            $"Expected last review rating to contain '{rating} /' but was: '{last.Text}'");
    }
}
