using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class CommonSteps
{
    private readonly WebDriverContext _webDriverContext;

    public CommonSteps(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [Given("I am on the home page")]
    public void GivenIAmOnTheHomePage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(TestSettings.BaseUrl);
    }

    [Then("the page loads")]
    public void ThenThePageLoads()
    {
        Assert.That(_webDriverContext.Driver!.Title, Is.Not.Null);
    }
}
