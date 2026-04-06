using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class DeleteAccountSteps
{
    private readonly WebDriverContext _webDriverContext;

    public DeleteAccountSteps(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [When("I click the delete account link")]
    public void WhenIClickTheDeleteAccountLink()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var link = wait.Until(d =>
        {
            var links = d.FindElements(By.TagName("a"));
            return links.FirstOrDefault(a =>
                a.Text.Trim().Contains("Delete Account", StringComparison.OrdinalIgnoreCase));
        });

        Assert.That(link, Is.Not.Null, "Delete Account link was not found.");
        link!.Click();
    }

    [When("I enter my current password {string} in the delete form")]
    public void WhenIEnterMyCurrentPasswordInTheDeleteForm(string password)
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var passwordField = wait.Until(d => d.FindElement(By.Id("CurrentPassword")));
        passwordField.Clear();
        passwordField.SendKeys(password);
    }

    [When("I click the permanently delete button")]
    public void WhenIClickThePermanentlyDeleteButton()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var button = wait.Until(d =>
        {
            var buttons = d.FindElements(By.CssSelector("button[type='submit']"));
            return buttons.FirstOrDefault(b =>
                b.Text.Trim().Contains("Permanently Delete", StringComparison.OrdinalIgnoreCase));
        });

        Assert.That(button, Is.Not.Null, "Permanently Delete button was not found.");
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", button);
    }

    [Then("I am redirected to the home page")]
    public void ThenIAmRedirectedToTheHomePage()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        wait.Until(d => d.Url == $"{TestSettings.BaseUrl}/");

        Assert.That(driver.Url, Is.EqualTo($"{TestSettings.BaseUrl}/"));
    }

    [Then("I am no longer logged in")]
    public void ThenIAmNoLongerLoggedIn()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        // Log In button should be visible in the navbar when logged out
        var found = wait.Until(d =>
        {
            var links = d.FindElements(By.TagName("a"));
            return links.Any(a =>
                a.Text.Trim().Contains("Log In", StringComparison.OrdinalIgnoreCase));
        });

        Assert.That(found, Is.True, "Expected Log In button to be visible after account deletion.");
    }
}