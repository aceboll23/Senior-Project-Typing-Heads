using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class ThemeToggleSteps
{
    private readonly WebDriverContext _webDriverContext;

    public ThemeToggleSteps(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [When("I click the theme toggle button")]
    public void WhenIClickTheThemeToggleButton()
    {
        _webDriverContext.Driver!.FindElement(By.Id("themeToggle")).Click();
        Thread.Sleep(300);
    }

    [When("I click the theme toggle button again")]
    public void WhenIClickTheThemeToggleButtonAgain()
    {
        _webDriverContext.Driver!.FindElement(By.Id("themeToggle")).Click();
        Thread.Sleep(300);
    }

    [When("I reload the page")]
    public void WhenIReloadThePage()
    {
        _webDriverContext.Driver!.Navigate().Refresh();
        Thread.Sleep(500);
    }

    [Then("the page is in light theme")]
    public void ThenThePageIsInLightTheme()
    {
        var html = _webDriverContext.Driver!.FindElement(By.TagName("html"));
        var theme = html.GetAttribute("data-theme");
        Assert.That(theme, Is.EqualTo("light"), "Expected data-theme='light' on <html> element");
    }

    [Then("the page is in dark theme")]
    public void ThenThePageIsInDarkTheme()
    {
        var html = _webDriverContext.Driver!.FindElement(By.TagName("html"));
        var theme = html.GetAttribute("data-theme");
        Assert.That(theme, Is.Null.Or.Not.EqualTo("light"), "Expected dark theme (no data-theme='light' on <html> element)");
    }
}
