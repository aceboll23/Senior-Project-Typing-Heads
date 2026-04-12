using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;
 
 [Binding]
 public class BrowseGamesSteps
{
  private readonly WebDriverContext _webDriverContext;

  public BrowseGamesSteps(WebDriverContext webDriverContext)
  {
    _webDriverContext = webDriverContext;
  }

  [When("I click the {string} link")]
  public void WhenIClickTheLink(string linkText)
  {
    var link = _webDriverContext.Driver!
      .FindElements(By.TagName("a"))
      .First(a => a.Text.Contains(linkText));

    ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
      "arguments[0].scrollIntoView({ block: 'center' });", link);
    
    ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
      "arguments[0].click();", link);  
  }

  [Then("I should be taken to the browse games page")]
  public void ThenIShouldBeTakenToTheBrowseGamesPage()
  {
    Assert.That(_webDriverContext.Driver!.Url, Does.Contain("/Games"));
    Assert.That(_webDriverContext.Driver.PageSource, Does.Contain("Browse Games"));
  }

  [Then("I should see a list of games from the database")]
  public void ThenIShouldSeeAListOfGamesFromTheDatabase()
  {
    Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain("<ul>"));
    Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain("Games will appear here"));
  }
}