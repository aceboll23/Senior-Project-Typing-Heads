using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class ReviewSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly ReviewTestContext _reviewTestContext;
    private readonly BddSeedDataContext _bddSeedDataContext;

    public ReviewSteps(WebDriverContext webDriverContext, ReviewTestContext reviewTestContext, BddSeedDataContext bddSeedDataContext)
    {
        _webDriverContext = webDriverContext;
        _reviewTestContext = reviewTestContext;
        _bddSeedDataContext = bddSeedDataContext;
    }

    [Given("I am on a game details page for a game I have not reviewed")]
    public void GivenIAmOnAGameDetailsPageForAGameIHaveNotReviewed()
    {
      _webDriverContext.Driver!.Navigate().GoToUrl(
        $"{TestSettings.BaseUrl}/Games/Details/{_bddSeedDataContext.CreateGameId}");
    }

    [Given("I am on a game details page for a game I have already reviewed")]
    public void GivenIAmOnAGameDetailsPageForAGameIHaveAlreadyReviewed()
    {
      _webDriverContext.Driver!.Navigate().GoToUrl(
        $"{TestSettings.BaseUrl}/Games/Details/{_bddSeedDataContext.ExistingReviewGameId}");
      _reviewTestContext.CurrentReviewText = _bddSeedDataContext.SeededReviewText;
    }

    [Given("I am on a clean game details page for invalid review testing")]
    public void GivenIAmOnACleanGameDetailsPageForInvalidReviewTesting()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(
          $"{TestSettings.BaseUrl}/Games/Details/{_bddSeedDataContext.CreateGameId}");
    }

    [When("I select a rating of {int}")]
    public void WhenISelectARatingOf(int rating)
    {
        var ratingDropdown = _webDriverContext.Driver!.FindElement(By.Id("rating"));
        ratingDropdown.SendKeys(rating.ToString());
    }

    [When("I enter review text {string}")]
    public void WhenIEnterReviewText(string reviewText)
    {
        _reviewTestContext.CurrentReviewText = reviewText;
        var textArea = _webDriverContext.Driver!.FindElement(By.Id("text"));
        textArea.Clear();
        textArea.SendKeys(reviewText);
    }

    [When("I leave the review text blank")]
    public void WhenILeaveTheReviewTextBlank()
    {
        var textArea = _webDriverContext.Driver!.FindElement(By.Id("text"));
        textArea.Clear();
    }

    [When("I submit the review form")]
    public void WhenISubmitTheReviewForm()
    {
      var buttons = _webDriverContext.Driver!.FindElements(By.TagName("button"));
      var submitButton = buttons.First(b => b.Text.Contains("Submit Review"));

      ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
        "arguments[0].scrollIntoView({ block: 'center' });", submitButton);

      ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
        "arguments[0].click();", submitButton);
      
    }

    [When("I click the Edit review action")]
    public void WhenIClickTheEditReviewAction()
    {
        var editLink = _webDriverContext.Driver!
            .FindElements(By.TagName("a"))
            .First(a => a.Text.Contains("Edit"));
        ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center' });", editLink);
        ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
            "arguments[0].click();", editLink);
    }

    [When("I change the rating to {int}")]
    public void WhenIChangeTheRatingTo(int rating)
    {
        var ratingDropdown = _webDriverContext.Driver!.FindElement(By.Id("rating"));
        ratingDropdown.SendKeys(rating.ToString());
    }

    [When("I change the review text to {string}")]
    public void WhenIChangeTheReviewTextTo(string updatedText)
    {
        _reviewTestContext.UpdatedReviewText = updatedText;
        var textArea = _webDriverContext.Driver!.FindElement(By.Id("text"));
        textArea.Clear();
        textArea.SendKeys(updatedText);
    }

    [When("I save my review changes")]
    public void WhenISaveMyReviewChanges()
    {
        var buttons = _webDriverContext.Driver!.FindElements(By.TagName("button"));
        var saveButton = buttons.First(b => b.Text.Contains("Save Changes"));

        ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center' });", saveButton);
        ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
            "arguments[0].click();", saveButton);
    }

    [When("I click the Delete review action")]
    public void WhenIClickTheDeleteReviewAction()
    {
        var deleteButtons = _webDriverContext.Driver!
            .FindElements(By.TagName("button"));
        
        var deleteButton = deleteButtons.First(b => b.Text.Contains("Delete"));

        ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center' });", deleteButton);
        ((IJavaScriptExecutor)_webDriverContext.Driver).ExecuteScript(
            "arguments[0].click();", deleteButton);
    }

    [When("I accept the delete confirmation")]
    public void WhenIAcceptTheDeleteConfirmation()
    {
        var alert = _webDriverContext.Driver!.SwitchTo().Alert();
        alert.Accept();
    }

    [Then("I should see the message {string}")]
    public void ThenIShouldSeeTheMessage(string expectedMessage)
    {
        var wait = new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10));
        wait.Until(d => d.PageSource.Contains(expectedMessage));
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(expectedMessage));
    }

    [Then("I should see my review text {string}")]
    public void ThenIShouldSeeMyReviewText(string expectedReviewText)
    {
        new WebDriverWait(_webDriverContext.Driver!, TimeSpan.FromSeconds(10))
            .Until(d => d.PageSource.Contains(expectedReviewText));
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(expectedReviewText));
    }

    [Then("I should not see my deleted review text")]
    public void ThenIShouldNotSeeMyDeletedReviewText()
    {
        Assert.That(
            _webDriverContext.Driver!.PageSource,
            Does.Not.Contain(_reviewTestContext.CurrentReviewText));
        
    }

    [Then("the review form should still be visible")]
    public void ThenTheReviewFormShouldStillBeVisible()
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain("Write a review"));
    }

    [Then("I should see {string}")]
    public void ThenIShouldSee(string expectedText)
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Contain(expectedText));
    }
    [Then("I should not see my review text {string}")]
    public void ThenIShouldNotSeeMyReviewText(string unexpectedReviewText)
    {
        Assert.That(_webDriverContext.Driver!.PageSource, Does.Not.Contain(unexpectedReviewText));
    }
}