namespace RADPlaywright
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using RADPlaywright.Apps;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public class RADTests_Playwright_B_ValidateMainUserInterface(TestContext testContext) : PlaywrightTestBase(testContext)
	{
		private LowCodeAppPage? page = null;

		[TestMethod]
		public async Task RADTests_Playwright_ValidateUserInterface()
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
				await LogIn(app);
			}
			else
			{
				Assert.Fail("RADManagerApp is not initialized.");
			}

			if (page != null)
			{
				await ValidateUserInterfaceComponents(page);
			}
			else
			{
			    Assert.Fail("LowCodeAppPage is not initialized.");
			}
		}

		private async Task LogIn(LowCodeApp app)
		{
			page = await app.NavigateToPageAsync("RAD%20Manager");

			await page.LoginAsync(Config.Credentials);
		}

		private async Task ValidateUserInterfaceComponents(LowCodeAppPage page)
		{
			var radManager = page.GetComponentByText("div", "RAD MANAGER", 3);
			var addSingleGroupButton = page.GetComponentByTitle("Add Single Group");
			var addSharedGroupButton = page.GetComponentByTitle("Add Shared Model Group");
			var filterOnGroupNameTextBox = page.GetComponentByRole("Filter on group name", Microsoft.Playwright.AriaRole.Textbox);
			var sortingButton = page.GetComponentByText("dma-button","Sorting & filtering", 0);
			var historicalAnomaliesButton = page.GetComponentByText("dma-button","Historical anomalies", 0);
			var dataMinerDocsButton = page.GetComponentByText("div","DataMiner Docs", 0);

			await page.CheckComponentAvailability(radManager);
			await page.CheckComponentAvailability(addSingleGroupButton);
			await page.CheckComponentAvailability(addSharedGroupButton);
			await page.CheckComponentAvailability(filterOnGroupNameTextBox);
			await page.CheckComponentAvailability(sortingButton);
			await page.CheckComponentAvailability(historicalAnomaliesButton);
			await page.CheckComponentAvailability(dataMinerDocsButton);

			await page.CheckComponentAvailability(page.GetComponentByText("Relational Anomaly Groups"));
			await page.CheckComponentAvailability(page.GetComponentById("[id=\"\\31 \"]"));

			// Time range locator
			await page.CheckComponentAvailability(page.GetComponentById("[id=\"\\33 \"]"));

			await page.CheckComponentAvailability(page.GetComponentByText("Trend graph of parameters in"));
			await page.CheckComponentAvailability(page.GetComponentById("[id=\"\\34 \"]"));

			await page.CheckComponentAvailability(page.GetComponentByText("Anomaly score of selected"));
			await page.CheckComponentAvailability(page.GetComponentById("[id=\"\\35 \"]"));

			// Historical anomalies locator
			await page.CheckComponentAvailability(page.GetComponentById("[id=\"\\31 2\"]"));

			// Click DataMiner Docs, wait until the Docs page is loaded and check that the heading is "Working with the RAD Manager"
			var dataMinerDocsPage = page.WaitOnDataMinerDocsPage();
		}
	}
}
