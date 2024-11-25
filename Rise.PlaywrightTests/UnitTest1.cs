using Microsoft.Playwright.NUnit;

namespace Rise.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
public class CounterTests : PageTest
{
    // Might want to use AppSettings.json for this.
    private const string ServerBaseUrl = "https://localhost:5001";
    [Test]
    public async Task Clicking_Login_Page()
    {
        // Navigate to the counter page
        await Page.GotoAsync($"{ServerBaseUrl}/embedded-login");
        // Wait until the counter page is really there.
        await Page.WaitForSelectorAsync("data-test-id=login-header");
        // Fill in user@hogent.be as email
        await Page.GetByTestId("login-email").FillAsync("user@hogent.be");
        // Fill in test.be as password
        await Page.GetByTestId("login-password").FillAsync("test");
        // Click on Login
        await Page.ClickAsync("data-test-id=login-button");
        // Assert
        await Expect(Page).ToHaveURLAsync($"{ServerBaseUrl}/");
    }
}