using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using Senior_Project_Typing_Heads.BddTests.Support;
using System.Net.Http.Json;

namespace Senior_Project_Typing_Heads.BddTests.StepDefinitions;

[Binding]
public class ProfilePictureSteps
{
    private readonly WebDriverContext _webDriverContext;
    private readonly BddProfilePictureSeedDataContext _seedContext;
    private readonly LoginHelper _loginHelper;

    private string? _tempFilePath;

    private class ResetProfilePictureDataResponse
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public ProfilePictureSteps(WebDriverContext webDriverContext, BddProfilePictureSeedDataContext seedContext, LoginHelper loginHelper)
    {
        _webDriverContext = webDriverContext;
        _seedContext = seedContext;
        _loginHelper = loginHelper;
    }

    private void ResetAndLogin()
    {
        using var httpClient = new HttpClient();
        var response = httpClient
            .PostAsync($"{TestSettings.BaseUrl}/dev/bdd/reset-profile-picture-data", null)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var data = response.Content.ReadFromJsonAsync<ResetProfilePictureDataResponse>().GetAwaiter().GetResult();
        ArgumentNullException.ThrowIfNull(data);

        _seedContext.Username = data.Username;
        _seedContext.Password = data.Password;

        _loginHelper.Login(_seedContext.Username, _seedContext.Password);
    }

    [Given("I am logged in as the profile picture test user")]
    public void GivenIAmLoggedInAsTheProfilePictureTestUser()
    {
        ResetAndLogin();
    }

    [Given("I am on my profile page")]
    public void GivenIAmOnMyProfilePage()
    {
        var driver = _webDriverContext.Driver!;
        driver.Navigate().GoToUrl($"{TestSettings.BaseUrl}/Profile/{_seedContext.Username}");
        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.Id("avatarFileInput")).Count > 0);
    }

    [When("I upload a valid profile picture")]
    public void WhenIUploadAValidProfilePicture()
    {
        // Create a minimal valid JPEG file in temp
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"bdd_avatar_{Guid.NewGuid()}.jpg");
        // Minimal 1x1 JPEG bytes
        var minimalJpeg = new byte[]
        {
            0xFF,0xD8,0xFF,0xE0,0x00,0x10,0x4A,0x46,0x49,0x46,0x00,0x01,
            0x01,0x00,0x00,0x01,0x00,0x01,0x00,0x00,0xFF,0xDB,0x00,0x43,
            0x00,0x08,0x06,0x06,0x07,0x06,0x05,0x08,0x07,0x07,0x07,0x09,
            0x09,0x08,0x0A,0x0C,0x14,0x0D,0x0C,0x0B,0x0B,0x0C,0x19,0x12,
            0x13,0x0F,0x14,0x1D,0x1A,0x1F,0x1E,0x1D,0x1A,0x1C,0x1C,0x20,
            0x24,0x2E,0x27,0x20,0x22,0x2C,0x23,0x1C,0x1C,0x28,0x37,0x29,
            0x2C,0x30,0x31,0x34,0x34,0x34,0x1F,0x27,0x39,0x3D,0x38,0x32,
            0x3C,0x2E,0x33,0x34,0x32,0xFF,0xC0,0x00,0x0B,0x08,0x00,0x01,
            0x00,0x01,0x01,0x01,0x11,0x00,0xFF,0xC4,0x00,0x1F,0x00,0x00,
            0x01,0x05,0x01,0x01,0x01,0x01,0x01,0x01,0x00,0x00,0x00,0x00,
            0x00,0x00,0x00,0x00,0x01,0x02,0x03,0x04,0x05,0x06,0x07,0x08,
            0x09,0x0A,0x0B,0xFF,0xC4,0x00,0xB5,0x10,0x00,0x02,0x01,0x03,
            0x03,0x02,0x04,0x03,0x05,0x05,0x04,0x04,0x00,0x00,0x01,0x7D,
            0x01,0x02,0x03,0x00,0x04,0x11,0x05,0x12,0x21,0x31,0x41,0x06,
            0x13,0x51,0x61,0x07,0x22,0x71,0x14,0x32,0x81,0x91,0xA1,0x08,
            0x23,0x42,0xB1,0xC1,0x15,0x52,0xD1,0xF0,0x24,0x33,0x62,0x72,
            0x82,0x09,0x0A,0x16,0x17,0x18,0x19,0x1A,0x25,0x26,0x27,0x28,
            0x29,0x2A,0x34,0x35,0x36,0x37,0x38,0x39,0x3A,0x43,0x44,0x45,
            0xFF,0xDA,0x00,0x08,0x01,0x01,0x00,0x00,0x3F,0x00,0xFB,0x00,
            0xFF,0xD9
        };
        File.WriteAllBytes(_tempFilePath, minimalJpeg);

        UploadFile(_tempFilePath);
    }

    [When("I upload a file with an invalid type")]
    public void WhenIUploadAFileWithAnInvalidType()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"bdd_avatar_{Guid.NewGuid()}.exe");
        File.WriteAllBytes(_tempFilePath, new byte[512]);
        UploadFile(_tempFilePath);
    }

    [When("I upload a file that is too large")]
    public void WhenIUploadAFileThatIsTooLarge()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"bdd_avatar_{Guid.NewGuid()}.jpg");
        // 6MB file
        File.WriteAllBytes(_tempFilePath, new byte[6 * 1024 * 1024]);
        UploadFile(_tempFilePath);
    }

    private void UploadFile(string filePath)
    {
        var driver = _webDriverContext.Driver!;

        // Mark the form so the change-event auto-submit skips (we'll submit manually via native click)
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "document.getElementById('avatarUploadForm').dataset.manualSubmit = '1';");

        var fileInput = driver.FindElement(By.Id("avatarFileInput"));
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].style.display = 'block';", fileInput);
        fileInput.SendKeys(filePath);

        // Make the submit button visible and click it natively (native click includes file inputs)
        var submitBtn = driver.FindElement(By.Id("avatarSubmitBtn"));
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].style.display = 'block';", submitBtn);
        submitBtn.Click();

        // Wait until either the avatar img (success) or error div (failure) appears
        new WebDriverWait(driver, TimeSpan.FromSeconds(15))
            .Until(d =>
                d.FindElements(By.Id("profile-avatar-img")).Count > 0 ||
                d.FindElements(By.Id("avatar-error")).Count > 0);
    }

    [Then("I should see my profile picture displayed on my profile")]
    public void ThenIShouldSeeMyProfilePictureDisplayedOnMyProfile()
    {
        var driver = _webDriverContext.Driver!;
        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.Id("profile-avatar-img")).Count > 0);

        var img = driver.FindElement(By.Id("profile-avatar-img"));
        Assert.That(img.GetAttribute("src"), Does.Contain("/uploads/avatars/"));
    }

    [Then("I should see a file type error message")]
    public void ThenIShouldSeeAFileTypeErrorMessage()
    {
        var driver = _webDriverContext.Driver!;
        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.Id("avatar-error")).Count > 0);

        var error = driver.FindElement(By.Id("avatar-error"));
        Assert.That(error.Text, Does.Contain("JPG").Or.Contain("PNG").IgnoreCase);
    }

    [Then("I should see a file size error message")]
    public void ThenIShouldSeeAFileSizeErrorMessage()
    {
        var driver = _webDriverContext.Driver!;
        new WebDriverWait(driver, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.Id("avatar-error")).Count > 0);

        var error = driver.FindElement(By.Id("avatar-error"));
        Assert.That(error.Text, Does.Contain("5MB").IgnoreCase);
    }

    [AfterScenario("profile-picture")]
    public void CleanupTempFile()
    {
        if (_tempFilePath != null && File.Exists(_tempFilePath))
            File.Delete(_tempFilePath);
    }
}
