using Reqnroll;
using NUnit.Framework;
using OpenQA.Selenium;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class GameNightAttendanceDefinitions
{
  private readonly WebDriverContext _webDriverContext;
  private readonly BddSeedDataContext _bddSeedDataContext;

  public GameNightAttendanceDefinitions(
    WebDriverContext webDriverContext,
    BddSeedDataContext bddSeedDataContext)
  {
    _webDriverContext = webDriverContext;
    _bddSeedDataContext = bddSeedDataContext;
  }

  [Given("a game night exists for my playgroup")]
  public void GivenAGameNightExistsForMyPlaygroup()
  {
    _webDriverContext.Driver!.Navigate().GoToUrl(
        $"{TestSettings.BaseUrl}/GameNightEvent/Details/{_bddSeedDataContext.GameNightEventId}"
    );
  }

  [Given("a game night event exists for my playgroup")]
  public void GivenAGameNightEventExistsForMyPlaygroup()
  {
    _webDriverContext.Driver!.Navigate().GoToUrl(
      $"{TestSettings.BaseUrl}/GameNightEvent/Details/{_bddSeedDataContext.GameNightEventId}"
    );
  }

  [When("I mark my attendance as {string}")]
  public void WhenIMarkMyAttendanceAs(string attendanceStatus)
  {
    throw new PendingStepException();
  }

  [Then("my attendance for that event should be saved as {string}")]
  public void ThenMyAttendanceForThatEventShouldBeSavedAs(string attendanceStatus)
  {
    throw new PendingStepException();
  }

}