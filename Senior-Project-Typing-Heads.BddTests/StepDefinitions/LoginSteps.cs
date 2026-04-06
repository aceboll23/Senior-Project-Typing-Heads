using OpenQA.Selenium;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class LoginSteps
{
    private readonly WebDriverContext _webDriverContext;

    public LoginSteps(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [Given("I am logged in as a registered user")]
    public void GivenIAmLoggedInAsARegisteredUser()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Account/Login");

        _webDriverContext.Driver.FindElement(By.Id("UsernameOrEmail")).SendKeys("BggTesting");
        _webDriverContext.Driver.FindElement(By.Id("Password")).SendKeys("Testing123");

        _webDriverContext.Driver.FindElement(By.CssSelector("button[type='submit']")).Click();
    }
}