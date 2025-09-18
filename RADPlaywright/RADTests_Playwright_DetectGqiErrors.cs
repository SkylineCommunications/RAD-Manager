namespace RADPlaywright
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using RADPlaywright.Apps;
	using static Microsoft.Playwright.Assertions;


	[TestClass]
	[TestCategory("IntegrationTest")]
	public class RADTests_Playwright_DetectGqiErrors(TestContext testContext) : PlaywrightTestBase(testContext)
	{
		[TestMethod]
		public async Task RADTests_Playwright_DetectGqiErrors_RADManager()
		{
			var app = new RADManagerApp(Context);
			await CheckGqiErrors(app);
		}

		private static async Task CheckGqiErrors(LowCodeAppPage page)
		{
			await page.WaitUntilEverythingIsLoadedAsync();

			var errorListLocator = page.Locator("dma-vr-error-list");

			await Expect(errorListLocator).Not.ToBeVisibleAsync();
		}

		private async Task CheckGqiErrors(LowCodeApp app)
		{
			var initialPage = await app.NavigateToInitialPageAsync();

			await initialPage.LoginAsync(Config.Credentials);
			await initialPage.WaitUntilEverythingIsLoadedAsync();

			await CheckGqiErrors(initialPage);

			var sidebarPages = await initialPage.GetSidebarPagesAsync();

			foreach (var sidebarPage in sidebarPages)
			{
				var page = await app.NavigateToPageAsync(sidebarPage);

				await CheckGqiErrors(page);
			}
		}
	}
}
