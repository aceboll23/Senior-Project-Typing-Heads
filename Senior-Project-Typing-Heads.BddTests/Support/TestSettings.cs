namespace Senior_Project_Typing_Heads.BddTests.Support;

public static class TestSettings
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("BDD_BASE_URL")
        ?? "http://localhost:5144";
}