using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class ContentModerationSteps
{
    private readonly WebDriverContext _webDriverContext;

    private const string CleanContent = "Just had an amazing game night with my friends!";
    private const string InappropriateContent = "BDD_FLAG_THIS test message that should be blocked";

    public ContentModerationSteps(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [When("I submit a profile post with clearly inappropriate content")]
    public void WhenISubmitAProfilePostWithClearlyInappropriateContent()
    {
        SubmitPost(InappropriateContent);
    }

    [When("I submit a profile post with clean content")]
    public void WhenISubmitAProfilePostWithCleanContent()
    {
        SubmitPost(CleanContent);
    }

    private void SubmitPost(string content)
    {
        var driver = _webDriverContext.Driver!;
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var textarea = wait.Until(d => d.FindElement(By.Id("post-content")));
        textarea.Clear();
        textarea.SendKeys(content);

        var submitBtn = driver.FindElement(By.Id("post-submit-btn"));
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", submitBtn);
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", submitBtn);
    }

    [Then("I should see a content moderation error message")]
    public void ThenIShouldSeeAContentModerationErrorMessage()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        wait.Until(d => d.PageSource.Contains("inappropriate", StringComparison.OrdinalIgnoreCase));
        Assert.That(_webDriverContext.Driver!.PageSource,
            Does.Contain("inappropriate").IgnoreCase);
    }

    [Then("the inappropriate post should not appear on the profile")]
    public void ThenTheInappropriatePostShouldNotAppearOnTheProfile()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Not.Contain(InappropriateContent));
    }

    [Then("the post should appear on the profile")]
    public void ThenThePostShouldAppearOnTheProfile()
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        wait.Until(d => d.PageSource.Contains(CleanContent));
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(CleanContent));
    }
}