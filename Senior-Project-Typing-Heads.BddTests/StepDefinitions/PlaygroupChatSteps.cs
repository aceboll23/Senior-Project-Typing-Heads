using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class PlaygroupChatSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddChatSeedDataContext _seedDataContext;
    private readonly LoginHelper _loginHelper;

    private class ResetChatDataResponse
    {
        public string OwnerUsername { get; set; } = "";
        public string OwnerPassword { get; set; } = "";
        public string MemberUsername { get; set; } = "";
        public string MemberPassword { get; set; } = "";
        public string OutsiderUsername { get; set; } = "";
        public string OutsiderPassword { get; set; } = "";
        public int PlaygroupId { get; set; }
        public string PlaygroupName { get; set; } = "";
    }

    public PlaygroupChatSteps(
        WebDriverContext webDriverContext,
        BddChatSeedDataContext seedDataContext,
        LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedDataContext = seedDataContext;
        _loginHelper = loginHelper;
    }

    private void EnsureSeedData()
    {
        if (_seedDataContext.PlaygroupId != 0) return;

        using var httpClient = new HttpClient();
        var resetResponse = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-chat-data", null)
            .GetAwaiter().GetResult();
        resetResponse.EnsureSuccessStatusCode();

        var seedData = resetResponse.Content
            .ReadFromJsonAsync<ResetChatDataResponse>()
            .GetAwaiter().GetResult();
        ArgumentNullException.ThrowIfNull(seedData, "Failed to read BDD chat seed data.");

        _seedDataContext.OwnerUsername = seedData.OwnerUsername;
        _seedDataContext.OwnerPassword = seedData.OwnerPassword;
        _seedDataContext.MemberUsername = seedData.MemberUsername;
        _seedDataContext.MemberPassword = seedData.MemberPassword;
        _seedDataContext.OutsiderUsername = seedData.OutsiderUsername;
        _seedDataContext.OutsiderPassword = seedData.OutsiderPassword;
        _seedDataContext.PlaygroupId = seedData.PlaygroupId;
        _seedDataContext.PlaygroupName = seedData.PlaygroupName;
    }

    [Given("I am logged in as a BDD chat member")]
    public void GivenIAmLoggedInAsABddChatMember()
    {
        EnsureSeedData();
        _loginHelper.Login(_seedDataContext.MemberUsername, _seedDataContext.MemberPassword);
    }

    [Given("I am logged in as a BDD chat outsider")]
    public void GivenIAmLoggedInAsABddChatOutsider()
    {
        EnsureSeedData();
        _loginHelper.Login(_seedDataContext.OutsiderUsername, _seedDataContext.OutsiderPassword);
    }

    [Given("I am logged in as the BDD chat owner")]
    public void GivenIAmLoggedInAsTheBddChatOwner()
    {
        EnsureSeedData();
        _loginHelper.Login(_seedDataContext.OwnerUsername, _seedDataContext.OwnerPassword);
    }

    [When("I navigate to the group chat page")]
    public void WhenINavigateToTheGroupChatPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/Playgroup/Chat/{_seedDataContext.PlaygroupId}");
    }

    [Then("the chat page is displayed with the playgroup name")]
    public void ThenTheChatPageIsDisplayedWithThePlaygroupName()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain(_seedDataContext.PlaygroupName));
    }

    [Then("I receive a not found response")]
    public void ThenIReceiveANotFoundResponse()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain("404").Or.Contain("Not Found").IgnoreCase);
    }

    [When("I send the message {string}")]
    public void WhenISendTheMessage(string message)
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        var textarea = wait.Until(d => d.FindElement(By.Id("message-input")));
        textarea.Clear();
        textarea.SendKeys(message);

        _webDriverContext.Driver!.FindElement(By.Id("send-btn")).Click();

        wait.Until(d => d.FindElements(By.CssSelector(".chat-message")).Any(
            el => el.Text.Contains(message)));
    }

    [Then("the message {string} is visible in the chat")]
    public void ThenTheMessageIsVisibleInTheChat(string message)
    {
        var messages = _webDriverContext.Driver!.FindElements(By.CssSelector(".chat-message"));
        Assert.That(messages.Any(m => m.Text.Contains(message)), Is.True,
            $"Expected message '{message}' to be visible in chat.");
    }

    [When("I attempt to send an empty message")]
    public void WhenIAttemptToSendAnEmptyMessage()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.Id("message-input")));
        // Leave textarea empty, just click send
        _webDriverContext.Driver!.FindElement(By.Id("send-btn")).Click();
    }

    [Then("the send button remains inactive")]
    public void ThenTheSendButtonRemainsInactive()
    {
        // The JS handler returns early on empty — no AJAX call is made.
        // The error panel should still be hidden.
        var errorPanel = _webDriverContext.Driver!.FindElement(By.Id("send-error"));
        Assert.That(errorPanel.GetDomAttribute("class"), Does.Contain("d-none"),
            "Error panel should not be shown after attempting empty send.");
    }

    [When("I leave the playgroup")]
    public void WhenILeaveThePlaygroup()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
            $"{TestSettings.BaseUrl}/Playgroup/Details/{_seedDataContext.PlaygroupId}");

        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        var leaveForm = wait.Until(d =>
            d.FindElements(By.CssSelector("form[action*='LeavePlaygroup']")).FirstOrDefault());

        Assert.That(leaveForm, Is.Not.Null, "Leave Playgroup button not found.");

        // Accept the confirm dialog
        ((IJavaScriptExecutor)_webDriverContext.Driver!).ExecuteScript(
            "window.confirm = function() { return true; };");
        leaveForm!.FindElement(By.CssSelector("button[type='submit']")).Click();

        wait.Until(d => d.Url.Contains("/Playgroup") && !d.Url.Contains("Details"));
    }

    [Then("a system message about the member leaving is visible")]
    public void ThenASystemMessageAboutTheMemberLeavingIsVisible()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain("left the playgroup").IgnoreCase);
    }
}
