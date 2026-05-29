namespace RADPlaywright
{
	using Microsoft.Playwright;
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
			await Expect(page.GetComponentByText("div", "RAD MANAGER", 3)).ToBeVisibleAsync(); // make sure RAD Manager is loaded

			var errorListLocator = page.Locator("dma-vr-error-list");

			await Expect(errorListLocator).Not.ToBeVisibleAsync();

			//Relational Anomaly Group check
			// Wait for component to fully load first
			await page.Locator("#db-component-1").WaitForAsync(new() { State = WaitForSelectorState.Visible });

			// Small delay for GQI errors to appear if they exist
			await Task.Delay(10000);

			// Now check
			errorListLocator = page.Locator("#db-component-1 dma-vr-error-list");
			await Expect(errorListLocator).Not.ToBeVisibleAsync();

			//Anomaly score check
			errorListLocator = page.Locator("#db-component-5 dma-vr-error-list");

			await Expect(errorListLocator).Not.ToBeVisibleAsync();

			//Trend graph check
			errorListLocator = page.Locator("dma-trend dma-vr-error-list");

			await Expect(errorListLocator).Not.ToBeVisibleAsync();
		}

		private async Task CheckGqiErrors(LowCodeApp app)
		{
			var initialPage = await app.NavigateToPageAsync();

			await initialPage.LoginAsync(Config.Credentials);

			await CheckGqiErrors(initialPage);
		}
	}
}
