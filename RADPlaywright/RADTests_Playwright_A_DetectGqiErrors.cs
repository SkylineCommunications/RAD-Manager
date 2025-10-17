namespace RADPlaywright
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using RADPlaywright.Apps;
	using static Microsoft.Playwright.Assertions;

	[TestClass]
	[DoNotParallelize]
	[TestCategory("IntegrationTest")]
	public class RADTests_Playwright_A_DetectGqiErrors(TestContext testContext) : PlaywrightTestBase(testContext)
	{
		[TestMethod]
		public async Task RADTests_Playwright_DetectGqiErrors_RADManager()
		{
			RADManagerApp? app = null;
			if (Context != null)
			{
			   app = new RADManagerApp(Context);
			}
			else
			{
				Assert.Fail("Browser context is not initialized.");
			}

			if (app != null)
		    {
				await CheckGqiErrors(app);
			}
			else
			{
				Assert.Fail("RADManagerApp is not initialized.");
			}
		}

		private static async Task CheckGqiErrors(LowCodeAppPage page)
		{
			await page.WaitUntilEverythingIsLoadedAsync();
			await Expect(page.GetComponentByText("div", "RAD MANAGER", 3)).ToBeVisibleAsync(); // make sure RAD Manager is loaded

			var errorListLocator = page.Locator("dma-vr-error-list");

			await Expect(errorListLocator).Not.ToBeVisibleAsync();
		}

		private async Task CheckGqiErrors(LowCodeApp app)
		{
			var initialPage = await app.NavigateToPageAsync("RAD%20Manager");

			await initialPage.LoginAsync(Config.Credentials);
			await initialPage.WaitUntilEverythingIsLoadedAsync();

			await CheckGqiErrors(initialPage);

			var sidebarPages = await initialPage.GetSidebarPagesAsync();
			LowCodeAppPage? page = null;
			foreach (var sidebarPage in sidebarPages)
			{
				if(sidebarPage != null)
				{
					 page = await app.NavigateToPageAsync(sidebarPage);
				}

				if(page != null)
				{
					await CheckGqiErrors(page);
				}
			}
		}
	}
}
