using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class FriendRequestSteps
{
    private readonly WebDriverContext _webDriverContext;

    public FriendRequestSteps(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [Given("I am logged in as {string} with password {string}")]
    public void GivenIAmLoggedInAs(string username, string password)
    {
        var driver = _webDriverContext.Driver!;

        driver.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Account/Login");

        driver.FindElement(By.Id("UsernameOrEmail")).SendKeys(username);
        driver.FindElement(By.Id("Password")).SendKeys(password);
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        wait.Until(d => d.Url == $"{TestSettings.BaseUrl}/");
    }

    [Given("I navigate to the profile page of {string}")]
    public void GivenINavigateToTheProfilePageOf(string username)
    {
        _webDriverContext.Driver!.Navigate()
            .GoToUrl($"{TestSettings.BaseUrl}/Profile/{username}");
    }

    [When("I click the {string} button")]
    public void WhenIClickTheButton(string buttonText)
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var button = wait.Until(d =>
        {
            var elements = d.FindElements(By.TagName("button"));
            return elements.FirstOrDefault(e =>
                e.Text.Trim().Contains(buttonText, StringComparison.OrdinalIgnoreCase));
        });

        Assert.That(button, Is.Not.Null, $"Button with text '{buttonText}' was not found.");
        button!.Click();
    }

    [Then("the button changes to {string}")]
    public void ThenTheButtonChangesTo(string expectedText)
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var found = wait.Until(d =>
        {
            var elements = d.FindElements(By.TagName("button"));
            return elements.Any(e =>
                e.Text.Trim().Contains(expectedText, StringComparison.OrdinalIgnoreCase));
        });

        Assert.That(found, Is.True, $"Expected button with text '{expectedText}' to appear.");
    }
}