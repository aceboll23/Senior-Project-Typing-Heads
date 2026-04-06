using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class DirectMessageSteps
{
    private readonly WebDriverContext _webDriverContext;

    public DirectMessageSteps(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [When("I click the {string} button link")]
    public void WhenIClickTheButtonLink(string linkText)
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        // Wait for jQuery to be ready
        wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d)
                    .ExecuteScript("return typeof jQuery !== 'undefined' && jQuery.active === 0");
                return ready is true;
            }
            catch { return false; }
        });

        // Message is an <a> tag not a button so search links
        var link = wait.Until(d =>
        {
            var elements = d.FindElements(By.TagName("a"));
            return elements.FirstOrDefault(e =>
                e.Text.Trim().Contains(linkText, StringComparison.OrdinalIgnoreCase));
        });

        Assert.That(link, Is.Not.Null, $"Link containing text '{linkText}' was not found.");
        link!.Click();
    }

    [When("I type {string} into the message input")]
    public void WhenITypeIntoTheMessageInput(string message)
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var input = wait.Until(d => d.FindElement(By.Id("messageInput")));
        input.Clear();
        input.SendKeys(message);
    }

    [When("I click the send button")]
    public void WhenIClickTheSendButton()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        // Wait for jQuery to be ready
        wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d)
                    .ExecuteScript("return typeof jQuery !== 'undefined' && jQuery.active === 0");
                return ready is true;
            }
            catch { return false; }
        });

        var sendBtn = wait.Until(d => d.FindElement(By.Id("sendBtn")));
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", sendBtn);
    }

    [Then("the message {string} appears in the conversation")]
    public void ThenTheMessageAppearsInTheConversation(string expectedMessage)
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        // Wait for jQuery AJAX to complete
        wait.Until(d =>
        {
            try
            {
                var idle = ((IJavaScriptExecutor)d)
                    .ExecuteScript("return typeof jQuery !== 'undefined' && jQuery.active === 0");
                return idle is true;
            }
            catch { return false; }
        });

        var found = wait.Until(d =>
        {
            var history = d.FindElement(By.Id("messageHistory"));
            return history.Text.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase);
        });

        Assert.That(found, Is.True, $"Expected message '{expectedMessage}' to appear in conversation.");
    }
}