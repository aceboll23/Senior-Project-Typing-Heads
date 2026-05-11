using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class ReportPostSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddPostSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    private IWebElement? _reportBtn;

    public ReportPostSteps(WebDriverContext webDriverContext, BddPostSeedDataContext seedContext, LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    [When("I click the Report button on a post")]
    public void WhenIClickTheReportButtonOnAPost()
    {
        var driver = _webDriverContext.Driver!;

        _reportBtn = new WebDriverWait(driver, TimeSpan.FromSeconds(5))
            .Until(d => d.FindElement(By.CssSelector(".report-post-btn")));

        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center' });", _reportBtn);
        _reportBtn.Click();
    }

    [When("I accept the report confirmation")]
    public void WhenIAcceptTheReportConfirmation()
    {
        var driver = _webDriverContext.Driver!;

        new WebDriverWait(driver, TimeSpan.FromSeconds(5))
            .Until(d =>
            {
                try { d.SwitchTo().Alert(); return true; }
                catch { return false; }
            });
        driver.SwitchTo().Alert().Accept();
    }

    [Then("the Report button should change to {string}")]
    public void ThenTheReportButtonShouldChangeTo(string expectedText)
    {
        var driver = _webDriverContext.Driver!;

        new WebDriverWait(driver, TimeSpan.FromSeconds(5))
            .Until(d =>
            {
                var btn = d.FindElement(By.CssSelector(".report-post-btn"));
                return btn.Text.Contains(expectedText);
            });

        _reportBtn = driver.FindElement(By.CssSelector(".report-post-btn"));
        Assert.That(_reportBtn.Text, Does.Contain(expectedText));
    }

    [Then("the Report button should be disabled")]
    public void ThenTheReportButtonShouldBeDisabled()
    {
        var driver = _webDriverContext.Driver!;
        var btn = driver.FindElement(By.CssSelector(".report-post-btn"));
        Assert.That(btn.GetAttribute("disabled"), Is.Not.Null);
    }
}
