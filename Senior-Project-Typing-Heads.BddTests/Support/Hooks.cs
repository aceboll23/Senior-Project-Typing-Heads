using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Reqnroll;

namespace Senior_Project_Typing_Heads.BddTests.Support;

[Binding]
public class Hooks
{
    private readonly WebDriverContext _webDriverContext;

    public Hooks(WebDriverContext webDriverContext)
    {
        _webDriverContext = webDriverContext;
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        var options = new ChromeOptions();
        options.AddArgument("--start-maximized");

        // Disable password manager and breach warning popups
        options.AddArgument("--disable-save-password-bubble");
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("profile.password_manager_enabled", false);
        options.AddUserProfilePreference("profile.password_manager_leak_detection", false);

        var runHeadless = Environment.GetEnvironmentVariable("BDD_HEADLESS");

        if (!string.IsNullOrWhiteSpace(runHeadless) &&
            runHeadless.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            options.AddArgument("--headless=new");
            options.AddArgument("--window-size=1920,1080");
        }

        _webDriverContext.Driver = new ChromeDriver(options);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _webDriverContext.Driver?.Quit();
        _webDriverContext.Driver?.Dispose();
    }
}