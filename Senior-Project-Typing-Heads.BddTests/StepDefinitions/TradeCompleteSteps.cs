using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class TradeCompleteSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddTradeCompleteSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    private class ResetTradeCompleteResponse
    {
        public string SenderUsername { get; set; } = "";
        public string SenderPassword { get; set; } = "";
        public string ReceiverUsername { get; set; } = "";
        public string ReceiverPassword { get; set; } = "";
        public string HasGameUsername { get; set; } = "";
        public string TransferGameName { get; set; } = "";
        public string PendingGameName { get; set; } = "";
        public int PendingTransferId { get; set; }
    }

    public TradeCompleteSteps(
        WebDriverContext webDriverContext,
        BddTradeCompleteSeedDataContext seedContext,
        LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private void ResetAndSeed()
    {
        using var httpClient = new HttpClient();
        var response = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-trade-complete-data", null)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var data = response.Content.ReadFromJsonAsync<ResetTradeCompleteResponse>().GetAwaiter().GetResult();
        ArgumentNullException.ThrowIfNull(data);

        _seedContext.SenderUsername = data.SenderUsername;
        _seedContext.SenderPassword = data.SenderPassword;
        _seedContext.ReceiverUsername = data.ReceiverUsername;
        _seedContext.ReceiverPassword = data.ReceiverPassword;
        _seedContext.HasGameUsername = data.HasGameUsername;
        _seedContext.TransferGameName = data.TransferGameName;
        _seedContext.PendingGameName = data.PendingGameName;
        _seedContext.PendingTransferId = data.PendingTransferId;
    }

    [Given("I am logged in as the trade-complete sender")]
    public void GivenIAmLoggedInAsTheTCsender()
    {
        ResetAndSeed();
        _loginHelper.Login(_seedContext.SenderUsername, _seedContext.SenderPassword);
    }

    [Given("I am logged in as the trade-complete receiver")]
    public void GivenIAmLoggedInAsTheTCReceiver()
    {
        ResetAndSeed();
        _loginHelper.Login(_seedContext.ReceiverUsername, _seedContext.ReceiverPassword);
    }

    [Then("I should see a Transfer Game button for the tradeable game")]
    public void ThenIShouldSeeATransferGameButton()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.PageSource.Contains("Transfer Game", StringComparison.OrdinalIgnoreCase));
        Assert.That(driver.PageSource, Does.Contain("Transfer Game").IgnoreCase);
    }

    [When("I initiate a transfer of the tradeable game to the receiver")]
    public void WhenIInitiateTransfer()
    {
        var driver = _webDriverContext.Driver!;
        driver.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection");
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        var transferBtn = wait.Until(d => d.FindElements(By.LinkText("Transfer Game"))
            .FirstOrDefault(e => e.Displayed));
        Assert.That(transferBtn, Is.Not.Null, "Transfer Game button not found");
        transferBtn!.Click();

        // Enter receiver username
        wait.Until(d => d.FindElement(By.Id("toUsername")));
        var input = driver.FindElement(By.Id("toUsername"));
        input.SendKeys(_seedContext.ReceiverUsername);
        driver.FindElement(By.Id("transfer-preview-btn")).Click();

        // On confirm page — click confirm
        wait.Until(d => d.FindElement(By.Id("transfer-confirm-btn")));
        driver.FindElement(By.Id("transfer-confirm-btn")).Click();

        // Wait for redirect back to collection
        wait.Until(d => d.Url.Contains("/collection", StringComparison.OrdinalIgnoreCase));
    }

    [Then("the tradeable game should no longer appear in my collection")]
    public void ThenTradeableGameNotInCollection()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        wait.Until(d => !d.FindElements(By.LinkText("Transfer Game")).Any(e => e.Displayed)
                        || d.PageSource.Contains("Transfer initiated", StringComparison.OrdinalIgnoreCase));
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Not.Contain(_seedContext.TransferGameName));
    }

    [Then("I should see a transfer initiated success message")]
    public void ThenIShouldSeeTransferInitiatedMessage()
    {
        Assert.That(_webDriverContext.Driver!.PageSource,
            Does.Contain("Transfer initiated").IgnoreCase);
    }

    [Then("I should see the pending transfers section")]
    public void ThenIShouldSeeThePendingTransfersSection()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElements(By.Id("pending-transfers-section")).Count > 0);
        Assert.That(_webDriverContext.Driver!.FindElements(By.Id("pending-transfers-section")), Is.Not.Empty);
    }

    [Then("I should see the pending game in the pending transfers section")]
    public void ThenIShouldSeePendingGameInSection()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(_seedContext.PendingGameName));
    }

    [When("I accept the pending game transfer")]
    public void WhenIAcceptThePendingTransfer()
    {
        var driver = _webDriverContext.Driver!;
        driver.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection");
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        var acceptBtn = wait.Until(d => d.FindElement(By.Id($"accept-transfer-btn-{_seedContext.PendingTransferId}")));
        acceptBtn.Click();
        wait.Until(d => d.Url.Contains("/collection", StringComparison.OrdinalIgnoreCase));
    }

    [Then("the pending game should appear in my collection")]
    public void ThenPendingGameInCollection()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        wait.Until(d => d.PageSource.Contains(_seedContext.PendingGameName));
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(_seedContext.PendingGameName));
    }


    [When("I decline the pending game transfer")]
    public void WhenIDeclineThePendingTransfer()
    {
        var driver = _webDriverContext.Driver!;
        driver.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection");
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        var declineBtn = wait.Until(d => d.FindElement(By.Id($"decline-transfer-btn-{_seedContext.PendingTransferId}")));
        declineBtn.Click();
        wait.Until(d => d.Url.Contains("/collection", StringComparison.OrdinalIgnoreCase));
    }

    [Then("the pending game should not appear in my collection")]
    public void ThenPendingGameNotInCollection()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Not.Contain(_seedContext.PendingGameName));
    }

    [When("I navigate to the transfer confirmation page for the tradeable game and the receiver")]
    public void WhenINavigateToTransferConfirmPage()
    {
        var driver = _webDriverContext.Driver!;
        // Navigate directly to transfer page for the tradeable game
        driver.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection");
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        var transferLink = wait.Until(d => d.FindElements(By.LinkText("Transfer Game"))
            .FirstOrDefault(e => e.Displayed));
        Assert.That(transferLink, Is.Not.Null);
        transferLink!.Click();

        wait.Until(d => d.FindElement(By.Id("toUsername")));
        driver.FindElement(By.Id("toUsername")).SendKeys(_seedContext.ReceiverUsername);
        driver.FindElement(By.Id("transfer-preview-btn")).Click();

        wait.Until(d => d.FindElement(By.Id("transfer-confirm-btn")));
    }

    [When("I cancel the transfer")]
    public void WhenICancelTheTransfer()
    {
        var driver = _webDriverContext.Driver!;
        var cancelBtn = driver.FindElement(By.Id("transfer-cancel-btn"));
        cancelBtn.Click();
        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d => d.Url.Contains("/collection", StringComparison.OrdinalIgnoreCase));
    }

    [Then("the tradeable game should still appear in my collection")]
    public void ThenTradeableGameStillInCollection()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        wait.Until(d => d.PageSource.Contains(_seedContext.TransferGameName));
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(_seedContext.TransferGameName));
    }

    [When("I try to transfer the tradeable game to the user who already owns it")]
    public void WhenITransferToUserWhoAlreadyOwns()
    {
        var driver = _webDriverContext.Driver!;
        driver.Navigate().GoToUrl($"{TestSettings.BaseUrl}/collection");
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        var transferLink = wait.Until(d => d.FindElements(By.LinkText("Transfer Game"))
            .FirstOrDefault(e => e.Displayed));
        Assert.That(transferLink, Is.Not.Null);
        transferLink!.Click();

        wait.Until(d => d.FindElement(By.Id("toUsername")));
        driver.FindElement(By.Id("toUsername")).SendKeys(_seedContext.HasGameUsername);
        driver.FindElement(By.Id("transfer-preview-btn")).Click();

        wait.Until(d => d.FindElement(By.Id("transfer-confirm-btn")));
        driver.FindElement(By.Id("transfer-confirm-btn")).Click();

        wait.Until(d => d.Url.Contains("/collection", StringComparison.OrdinalIgnoreCase));
    }

    [Then("I should see an error that the receiving user already has the game")]
    public void ThenIShouldSeeAlreadyHasGameError()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        wait.Until(d => d.PageSource.Contains("already has", StringComparison.OrdinalIgnoreCase));
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain("already has").IgnoreCase);
    }
}
