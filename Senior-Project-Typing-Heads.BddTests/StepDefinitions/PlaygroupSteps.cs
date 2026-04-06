using NUnit.Framework;
using OpenQA.Selenium; 
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class PlaygroupSteps
{
    private readonly WebDriverContext _webDriverContext;
    private string _createdPlaygroupName = string.Empty;

    public PlaygroupSteps(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [Given("I am logged in as PersonThree")]
    public void GivenIAmLoggedInAsPersonThree()
    {
        var driver = _webDriverContext.Driver!;

        driver.Navigate().GoToUrl(TestSettings.BaseUrl + "/Account/Login");

        driver.FindElement(By.Id("UsernameOrEmail")).SendKeys("PersonThree");
        driver.FindElement(By.Id("Password")).SendKeys("123ABc!!!");
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        // Wait for login to complete and redirect
        System.Threading.Thread.Sleep(1000);
    }

    [When("I navigate to the Create Playgroup page")]
    public void WhenINavigateToTheCreatePlaygroupPage()
    {
        _webDriverContext.Driver!.Navigate().GoToUrl(TestSettings.BaseUrl + "/Playgroup/Create");
    }

    [Then("the Create Playgroup form is displayed")]
    public void ThenTheCreatePlaygroupFormIsDisplayed()
    {
        var form = _webDriverContext.Driver!.FindElement(By.TagName("form"));
        Assert.That(form, Is.Not.Null);
    }

        [When("I fill in the Create Playgroup form with a unique name")]
    public void WhenIFillInTheCreatePlaygroupFormWithAUniqueName()
    {
        _createdPlaygroupName = "Test Group BDD " + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        _webDriverContext.Driver!.FindElement(By.Id("name")).SendKeys(_createdPlaygroupName);
    }

    [When("I submit the Create Playgroup form")]
    public void WhenISubmitTheCreatePlaygroupForm()
    {
        _webDriverContext.Driver!.FindElement(By.CssSelector(".btn-primary[type='submit']")).Click();
        System.Threading.Thread.Sleep(1000);
    }

    [Then("I am redirected to the playgroup detail page")]
    public void ThenIAmRedirectedToThePlaygroupDetailPage()
    {
        Assert.That(_webDriverContext.Driver!.Url, Does.Contain("/Playgroup/Details"));
    }

    [Then("the created playgroup name is displayed")]
    public void ThenTheCreatedPlaygroupNameIsDisplayed()
    {
        var body = _webDriverContext.Driver!.FindElement(By.TagName("body")).Text;
        Assert.That(body, Does.Contain(_createdPlaygroupName));
    }
}