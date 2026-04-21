using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class SocialFeedSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddSocialFeedSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    private class ResetSocialFeedDataResponse
    {
        public string ViewerUsername { get; set; } = "";
        public string ViewerPassword { get; set; } = "";
        public string FriendUsername { get; set; } = "";
        public string FriendPassword { get; set; } = "";
        public string FriendPostContent { get; set; } = "";
        public string StrangerPostContent { get; set; } = "";
        public string OlderPostContent { get; set; } = "";
        public string NewerPostContent { get; set; } = "";
    }

    public SocialFeedSteps(WebDriverContext webDriverContext, BddSocialFeedSeedDataContext seedContext, LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private void ResetAndPopulateSeedData()
    {
        using var httpClient = new HttpClient();

        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-social-feed-data", null)
            .GetAwaiter()
            .GetResult();

        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetSocialFeedDataResponse>()
            .GetAwaiter()
            .GetResult();

        ArgumentNullException.ThrowIfNull(seedData, "Failed to read BDD social feed seed data response.");

        _seedContext.ViewerUsername = seedData.ViewerUsername;
        _seedContext.ViewerPassword = seedData.ViewerPassword;
        _seedContext.FriendUsername = seedData.FriendUsername;
        _seedContext.FriendPassword = seedData.FriendPassword;
        _seedContext.FriendPostContent = seedData.FriendPostContent;
        _seedContext.StrangerPostContent = seedData.StrangerPostContent;
        _seedContext.OlderPostContent = seedData.OlderPostContent;
        _seedContext.NewerPostContent = seedData.NewerPostContent;
    }

    [Given("I am logged in as the social feed viewer")]
    public void GivenIAmLoggedInAsTheSocialFeedViewer()
    {
        ResetAndPopulateSeedData();
        _loginHelper.Login(_seedContext.ViewerUsername, _seedContext.ViewerPassword);
    }

    [Given("I am logged in as the social feed friend")]
    public void GivenIAmLoggedInAsTheSocialFeedFriend()
    {
        ResetAndPopulateSeedData();
        _loginHelper.Login(_seedContext.FriendUsername, _seedContext.FriendPassword);
    }

    [When("I navigate to the social feed page")]
    public void WhenINavigateToTheSocialFeedPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/SocialFeed");
    }

    [Then("I should see the friend's post content in the feed")]
    public void ThenIShouldSeeTheFriendsPostContentInTheFeed()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(_seedContext.FriendPostContent));
    }

    [Then("I should see the friend's username in the feed")]
    public void ThenIShouldSeeTheFriendsUsernameInTheFeed()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(_seedContext.FriendUsername));
    }

    [Then("I should see the empty feed message")]
    public void ThenIShouldSeeTheEmptyFeedMessage()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain("No posts yet"));
    }

    [Then("the stranger's post should not appear in the feed")]
    public void ThenTheStrangersPostShouldNotAppearInTheFeed()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Not.Contain(_seedContext.StrangerPostContent));
    }

    [Then("the newer post should appear before the older post in the feed")]
    public void ThenTheNewerPostShouldAppearBeforeTheOlderPost()
    {
        var source = _webDriverContext.Driver!.PageSource;
        var newerIndex = source.IndexOf(_seedContext.NewerPostContent, StringComparison.Ordinal);
        var olderIndex = source.IndexOf(_seedContext.OlderPostContent, StringComparison.Ordinal);
        Assert.That(newerIndex, Is.GreaterThanOrEqualTo(0), "Newer post not found on page");
        Assert.That(olderIndex, Is.GreaterThanOrEqualTo(0), "Older post not found on page");
        Assert.That(newerIndex, Is.LessThan(olderIndex), "Newer post should appear before older post");
    }

    [Then("I should see a Social link in the navigation")]
    public void ThenIShouldSeeASocialLinkInTheNavigation()
    {
        var links = _webDriverContext.Driver!.FindElements(By.TagName("a"));
        var socialLink = links.FirstOrDefault(a =>
            a.Text.Trim().Equals("Social", StringComparison.OrdinalIgnoreCase) ||
            (a.GetAttribute("href") ?? "").Contains("/SocialFeed"));

        Assert.That(socialLink, Is.Not.Null, "Social link not found in navigation");
    }
}
