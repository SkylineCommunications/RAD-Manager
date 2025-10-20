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
			await page.WaitUntilEverythingIsLoadedAsync();
		}

		private async Task ValidateUserInterfaceComponents(LowCodeAppPage page)
		{
			await page.WaitUntilEverythingIsLoadedAsync();

			var radManager = page.GetComponentByText("div", "RAD MANAGER", 3);
			var addGroupButton = page.GetComponentByTitle("Add Group");
			var editGroupButton = page.GetComponentByTitle("Edit Group");
			var removeGroupButton = page.GetComponentByTitle("Remove Group");
			var specifyTrainingRangeButton = page.GetComponentByTitle("Specify Training Range");
			var dataMinerDocsButton = page.GetComponentByText("div","DataMiner Docs", 0);

			await page.CheckComponentAvailability(radManager);
			await page.CheckComponentAvailability(addGroupButton);
			await page.CheckComponentAvailability(editGroupButton);
			await page.CheckComponentAvailability(removeGroupButton);
			await page.CheckComponentAvailability(specifyTrainingRangeButton);
			await page.CheckComponentAvailability(dataMinerDocsButton);

			await page.CheckComponentAvailability(page.GetComponentByText("Relational Anomaly Groups"));
			await page.CheckComponentAvailability(page.GetComponentById("[id=\"\\31 \"]"));

			// Time range locator
			/*await page.CheckComponentAvailability(page.GetComponentById("[id=\"\\33 \"]"));

			await page.CheckComponentAvailability(page.GetComponentByText("Trend graph of parameters in"));
			await page.CheckComponentAvailability(page.GetComponentById("[id=\"\\34 \"]"));

			await page.CheckComponentAvailability(page.GetComponentByText("Anomaly score of selected"));
			await page.CheckComponentAvailability(page.GetComponentById("[id=\"\\35 \"]"));

			await page.CheckComponentAvailability(page.GetComponentByText("Historical anomalies in"));
			await page.CheckComponentAvailability(page.GetComponentById("[id=\"\\31 2\"]"));*/

			// Click DataMiner Docs, wait until the Docs page is loaded and check that the heading is "Working with the RAD Manager"
			var dataMinerDocsPage = page.WaitOnDataMinerDocsPage();
		}
	}
}
