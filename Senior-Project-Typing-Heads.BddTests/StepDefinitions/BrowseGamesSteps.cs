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

  [When("I enter a minimum rating of {string}")]
  public void WhenIEnterAMinimumRatingOf(string rating)
  {
    var minRatingInput = _webDriverContext.Driver!.FindElement(By.Id("minRating"));
    minRatingInput.Clear();
    minRatingInput.SendKeys(rating);
  }

  [When("I apply the browse filters")]
  public void WhenIApplyTheBrowseFilters()
  {
    var buttons = _webDriverContext.Driver!.FindElements(By.TagName("button"));
    var applyButton = buttons.First(b => b.Text.Contains("Apply Filters"));
    ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
      "arguments[0].scrollIntoView({ block: 'center' });", applyButton);
    
    ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
      "arguments[0].click();", applyButton);
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
    Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain("card"));
    Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain("Browse Games"));
  }

  [Then("I should see filtered browse game results")]
  public void ThenIShouldSeeFilteredBrowseGameResults()
  {
    Assert.That(_webDriverContext.Driver!.Url, Does.Contain("minRating=8.0"));
    Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain("Browse Games"));
    Assert.That(_webDriverContext.Driver!.PageSource, Does. Contain("card"));
  }
}