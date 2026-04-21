using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Senior_Project_Typing_Heads.BddTests.Support;

public class LoginHelper
{
    private readonly WebDriverContext _webDriverContext;

    public LoginHelper(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    public void Login(string username, string password)
    {
        var driver = _webDriverContext.Driver!;
        driver.Manage().Cookies.DeleteAllCookies();
        driver.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Account/Login");
        driver.FindElement(By.Id("UsernameOrEmail")).SendKeys(username);
        driver.FindElement(By.Id("Password")).SendKeys(password);
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d => !d.Url.Contains("/Account/Login"));
    }
}
