using OpenQA.Selenium;
using Reqnroll;
using System.Net.Http.Json;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class LoginSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddSeedDataContext _bddSeedDataContext;
    private readonly LoginHelper _loginHelper;

    public LoginSteps(WebDriverContext webDriverContext, BddSeedDataContext bddSeedDataContext, LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _bddSeedDataContext = bddSeedDataContext;
        _loginHelper = loginHelper;
    }

    private class ResetReviewDataResponse
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int CreateGameId { get; set; }
        public int ExistingReviewGameId { get; set; }
        public string SeededReviewText { get; set; } = "";
    }

    private class ResetGameNightAttendanceDataResponse
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int GameNightEventId { get; set; }
    }

    [Given("I am logged in as a registered user")]
    public void GivenIAmLoggedInAsARegisteredUser()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-review-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetReviewDataResponse>()
            .GetAwaiter()
            .GetResult();

        if (seedData == null)
        {
            throw new InvalidOperationException("Failed to read BDD seed data response.");
        }

        _bddSeedDataContext.CreateGameId = seedData.CreateGameId;
        _bddSeedDataContext.ExistingReviewGameId = seedData.ExistingReviewGameId;
        _bddSeedDataContext.SeededReviewText = seedData.SeededReviewText;

        _loginHelper.Login(seedData.Username, seedData.Password);
    }

    [Given("I am a logged-in member of the playgroup for the event")]
    public void GivenIAmALoggedInMemeberOfThePlaygroupForTheEvent()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-gamenight-attendance-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetGameNightAttendanceDataResponse>()
            .GetAwaiter()
            .GetResult();

        if (seedData == null)
        {
            throw new InvalidOperationException("Failed to read BDD game night attendance seed data response.");
        }

        _bddSeedDataContext.GameNightEventId = seedData.GameNightEventId;

        _loginHelper.Login(seedData.Username, seedData.Password);
    }
}
