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

}