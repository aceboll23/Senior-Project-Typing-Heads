using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class ProfilePostSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddPostSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    private string? _tempImagePath;

    private class ResetPostDataResponse
    {
        public string OwnerUsername { get; set; } = "";
        public string OwnerPassword { get; set; } = "";
        public string FriendUsername { get; set; } = "";
        public string FriendPassword { get; set; } = "";
        public string ExistingPostContent { get; set; } = "";
        public int SeededGameId { get; set; }
        public string SeededGameName { get; set; } = "";
    }

    public ProfilePostSteps(WebDriverContext webDriverContext, BddPostSeedDataContext seedContext, LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private void ResetAndPopulateSeedData()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-post-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetPostDataResponse>()
            .GetAwaiter()
            .GetResult();

        ArgumentNullException.ThrowIfNull(seedData, "Failed to read BDD post seed data response.");

        _seedContext.OwnerUsername = seedData.OwnerUsername;
        _seedContext.OwnerPassword = seedData.OwnerPassword;
        _seedContext.FriendUsername = seedData.FriendUsername;
        _seedContext.FriendPassword = seedData.FriendPassword;
        _seedContext.ExistingPostContent = seedData.ExistingPostContent;
        _seedContext.SeededGameId = seedData.SeededGameId;
        _seedContext.SeededGameName = seedData.SeededGameName;
    }

    [AfterScenario("post-image")]
    public void CleanupTempImage()
    {
        if (_tempImagePath != null && File.Exists(_tempImagePath))
            File.Delete(_tempImagePath);
    }

    [Given("I am logged in as the post test owner")]
    public void GivenIAmLoggedInAsThePostTestOwner()
    {
        ResetAndPopulateSeedData();
        _loginHelper.Login(_seedContext.OwnerUsername, _seedContext.OwnerPassword);
    }

    [Given("I am logged in as a friend of the post owner")]
    public void GivenIAmLoggedInAsAFriendOfThePostOwner()
    {
        ResetAndPopulateSeedData();
        _loginHelper.Login(_seedContext.FriendUsername, _seedContext.FriendPassword);
    }

    [Given("I am on my own profile page")]
    public void GivenIAmOnMyOwnProfilePage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/Profile/{_seedContext.OwnerUsername}");
    }

    [Given("I navigate to my own profile page")]
    public void GivenINavigateToMyOwnProfilePage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/Profile/{_seedContext.OwnerUsername}");
    }

    [When("I enter post content {string}")]
    public void WhenIEnterPostContent(string content)
    {
        var textarea = _webDriverContext.Driver!.FindElement(By.Id("post-content"));
        textarea.Clear();
        textarea.SendKeys(content);
    }

    [When("I click the Post button")]
    public void WhenIClickThePostButton()
    {
        var btn = _webDriverContext.Driver!.FindElement(By.Id("post-submit-btn"));
        ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center' });", btn);
        btn.Click();
        new WebDriverWait(_webDriverContext.Driver, TimeSpan.FromSeconds(10))
            .Until(d => {
                try { var _ = btn.TagName; return false; }
                catch (StaleElementReferenceException) { return true; }
            });
        new WebDriverWait(_webDriverContext.Driver, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.Id("post-submit-btn")).Count > 0);
    }

    [When("I navigate to the post owner's profile")]
    public void WhenINavigateToThePostOwnersProfile()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/Profile/{_seedContext.OwnerUsername}");
    }

    [When("I click the delete button on my post")]
    public void WhenIClickTheDeleteButtonOnMyPost()
    {
        var deleteBtn = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5))
            .Until(d => d.FindElement(By.CssSelector("[id^='delete-post-btn-']")));

        ((IJavaScriptExecutor)_webDriverContext.Driver!).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center' });", deleteBtn);
        ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
            "arguments[0].click();", deleteBtn);
    }

    [When("I submit the post form with no content")]
    public void WhenISubmitThePostFormWithNoContent()
    {
        var btn = _webDriverContext.Driver!.FindElement(By.Id("post-submit-btn"));
        ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center' });", btn);
        btn.Click();
        new WebDriverWait(_webDriverContext.Driver, TimeSpan.FromSeconds(10))
            .Until(d => {
                try { var _ = btn.TagName; return false; }
                catch (StaleElementReferenceException) { return true; }
            });
        new WebDriverWait(_webDriverContext.Driver, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.Id("post-submit-btn")).Count > 0);
    }

    [When("I attach an image to the post")]
    public void WhenIAttachAnImageToThePost()
    {
        _tempImagePath = Path.Combine(Path.GetTempPath(), $"bdd_post_{Guid.NewGuid()}.jpg");
        var minimalJpeg = new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
            0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
            0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
            0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20,
            0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
            0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
            0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
            0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x1F, 0x00, 0x00,
            0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0xFF, 0xC4, 0x00, 0xB5, 0x10, 0x00, 0x02, 0x01, 0x03,
            0x03, 0x02, 0x04, 0x03, 0x05, 0x05, 0x04, 0x04, 0x00, 0x00, 0x01, 0x7D,
            0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0xFB, 0xFF,
            0xD9
        };
        File.WriteAllBytes(_tempImagePath, minimalJpeg);

        var driver = _webDriverContext.Driver!;
        var fileInput = driver.FindElement(By.Id("post-image-input"));
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].style.display = 'block';", fileInput);
        fileInput.SendKeys(_tempImagePath);
    }

    [When("I select the {string} category")]
    public void WhenISelectTheCategory(string categoryName)
    {
        var select = _webDriverContext.Driver!.FindElement(By.Id("post-category"));
        var options = select.FindElements(By.TagName("option"));
        var option = options.FirstOrDefault(o => o.Text.Contains(categoryName));
        option?.Click();
    }

    [When("I select the seeded game from the game dropdown")]
    public void WhenISelectTheSeededGameFromTheGameDropdown()
    {
        var select = _webDriverContext.Driver!.FindElement(By.Id("post-game"));
        var options = select.FindElements(By.TagName("option"));
        var option = options.FirstOrDefault(o => o.Text.Contains(_seedContext.SeededGameName));
        option?.Click();
    }

    [Then("I should see {string} on the profile page")]
    public void ThenIShouldSeeOnTheProfilePage(string expectedText)
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(expectedText));
    }

    [Then("I should see the seeded post content on the profile page")]
    public void ThenIShouldSeeTheSeededPostContentOnTheProfilePage()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(_seedContext.ExistingPostContent));
    }

    [Then("the deleted post should not appear on the profile")]
    public void ThenTheDeletedPostShouldNotAppearOnTheProfile()
    {
        new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(5))
            .Until(d => !d.PageSource.Contains(_seedContext.ExistingPostContent));
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Not.Contain(_seedContext.ExistingPostContent));
    }

    [Then("I should see a post content error on the profile page")]
    public void ThenIShouldSeeAPostContentErrorOnTheProfilePage()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain("Post must include either text or an image"));
    }

    [Then("I should not see the post creation form")]
    public void ThenIShouldNotSeeThePostCreationForm()
    {
        var forms = _webDriverContext.Driver!.FindElements(By.Id("post-content"));
        Assert.That(forms, Is.Empty);
    }

    [Then("I should see the post image on the profile page")]
    public void ThenIShouldSeeThePostImageOnTheProfilePage()
    {
        new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.CssSelector("img[id^='post-image-']")).Count > 0);
        var img = _webDriverContext.Driver!.FindElement(By.CssSelector("img[id^='post-image-']"));
        Assert.That(img.GetAttribute("src"), Does.Contain("/uploads/posts/"));
    }

    [Then("I should see the {string} badge on the profile page")]
    public void ThenIShouldSeeTheBadgeOnTheProfilePage(string badgeText)
    {
        new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.CssSelector("[id^='post-category-badge-']")).Count > 0);
        var badge = _webDriverContext.Driver!.FindElement(By.CssSelector("[id^='post-category-badge-']"));
        Assert.That(badge.Text, Does.Contain(badgeText));
    }

    [Then("I should see the seeded game name on the profile page")]
    public void ThenIShouldSeeTheSeededGameNameOnTheProfilePage()
    {
        new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.CssSelector("[id^='post-game-link-']")).Count > 0);
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(_seedContext.SeededGameName));
    }

    [Then("the character counter should show {int}")]
    public void ThenTheCharacterCounterShouldShow(int expected)
    {
        var counter = _webDriverContext.Driver!.FindElement(By.Id("post-char-counter"));
        Assert.That(counter.Text, Is.EqualTo(expected.ToString()));
    }
}
