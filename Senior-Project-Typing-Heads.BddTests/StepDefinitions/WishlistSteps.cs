using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class WishlistSteps
{
    private readonly WebDriverContext _webDriverContext;

    public WishlistSteps(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [When("I navigate to the Dune game details page")]
    public void WhenINavigateToTheDuneGameDetailsPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(TestSettings.BaseUrl + "/Games/Details/122");
    }

    [When("I click the Add to Wishlist button")]
    public void WhenIClickTheAddToWishlistButton()
    {
        _webDriverContext.Driver!.FindElement(By.Id("addToWishlistBtn")).Click();
        System.Threading.Thread.Sleep(1000);
    }

    [Then("the Dune game appears in my wishlist")]
    public void ThenTheDuneGameAppearsInMyWishlist()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(TestSettings.BaseUrl + "/Collection");
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain("Dune"));
    }

    [Then("the Add to Wishlist button is disabled or shows On Wishlist")]
    public void ThenTheAddToWishlistButtonIsDisabledOrShowsOnWishlist()
    {
        var button = _webDriverContext.Driver!.FindElement(By.Id("addToWishlistBtn"));
        var isDisabled = !button.Enabled || button.GetAttribute("disabled") != null;
        var showsOnWishlist = button.Text.Contains("On Wishlist");
        Assert.That(isDisabled || showsOnWishlist, Is.True, "Button should be disabled or show 'On Wishlist'");
    }
}
