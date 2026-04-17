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

    private class ResetPostDataResponse
    {
        public string OwnerUsername { get; set; } = "";
        public string OwnerPassword { get; set; } = "";
        public string FriendUsername { get; set; } = "";
        public string FriendPassword { get; set; } = "";
        public string ExistingPostContent { get; set; } = "";
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
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain("Post content cannot be empty"));
    }

    [Then("I should not see the post creation form")]
    public void ThenIShouldNotSeeThePostCreationForm()
    {
        var forms = _webDriverContext.Driver!.FindElements(By.Id("post-content"));
        Assert.That(forms, Is.Empty);
    }
}
