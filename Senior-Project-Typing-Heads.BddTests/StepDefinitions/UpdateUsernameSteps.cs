using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class UpdateUsernameSteps
{
    private readonly WebDriverContext _webDriverContext;

    public UpdateUsernameSteps(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [Given("I navigate to the settings page")]
    public void GivenINavigateToTheSettingsPage()
    {
        _webDriverContext.Driver!.Navigate()
            .GoToUrl($"{TestSettings.BaseUrl}/Settings");
    }

    [When("I clear the username field and type {string}")]
    public void WhenIClearTheUsernameFieldAndType(string newUsername)
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var usernameField = wait.Until(d => d.FindElement(By.Id("Username")));
        usernameField.Clear();
        usernameField.SendKeys(newUsername);
    }

    [When("I enter my current password {string} in the settings form")]
    public void WhenIEnterMyCurrentPasswordInTheSettingsForm(string password)
    {
        var driver = _webDriverContext.Driver!;
        var passwordField = driver.FindElement(By.Id("CurrentPassword"));
        passwordField.Clear();
        passwordField.SendKeys(password);
    }

    [When("I click the save changes button")]
    public void WhenIClickTheSaveChangesButton()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var button = wait.Until(d =>
        {
            var buttons = d.FindElements(By.CssSelector("button[type='submit']"));
            return buttons.FirstOrDefault(b =>
                b.Text.Trim().Contains("Save Changes", StringComparison.OrdinalIgnoreCase));
        });

        Assert.That(button, Is.Not.Null, "Save Changes button was not found.");
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", button);
    }

    [Then("I see the success message")]
    public void ThenISeeTheSuccessMessage()
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var found = wait.Until(d =>
        {
            var alerts = d.FindElements(By.CssSelector(".alert-success"));
            return alerts.Any(a => a.Displayed);
        });

        Assert.That(found, Is.True, "Expected a success message to be displayed.");
    }

    [Then("the navbar displays {string}")]
    public void ThenTheNavbarDisplays(string expectedUsername)
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var found = wait.Until(d =>
        {
            var navbar = d.FindElement(By.CssSelector(".navbar"));
            return navbar.Text.Contains(expectedUsername, StringComparison.OrdinalIgnoreCase);
        });

        Assert.That(found, Is.True, $"Expected navbar to display '{expectedUsername}'.");
    }
}