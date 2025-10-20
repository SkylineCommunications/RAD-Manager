namespace RADPlaywright
{
	using Microsoft.Playwright;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NUnit.Framework;
	using RADPlaywright.Apps;

	[TestClass]
	[DoNotParallelize]
	[Order(1)]
	[Microsoft.VisualStudio.TestTools.UnitTesting.Ignore("Test not compatible with the old RAD Manager app")]
	[TestCategory("IntegrationTest")]
	public class RADTests_Playwright_C_AddEditTrainRemoveGroup(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext testContext) : PlaywrightTestBase(testContext)
	{
		private LowCodeAppPage? page = null;

		[TestMethod]
		public async Task RADTests_Playwright_AddEditTrainRemoveGroup()
		{
			RADManagerApp? app = null;
			if (Context != null)
			{
				app = new RADManagerApp(Context);
			}
			else
			{
				Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Browser context is not initialized.");
			}

			if (app != null)
			{
				await LogIn(app);
			}
			else
			{
				Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("RADManagerApp is not initialized.");
			}

			if (page != null)
			{
				await RemoveGroupsFromPreviousRun(page);

				await ValidateAddGroupPanelComponents(page);
				await AddGroup(page);

				await ValidateEditGroupPanelComponents(page);
				await EditGroup(page);

				await ValidateRemoveGroupPanelComponents(page);
				await RemoveGroup(page);
			}
			else
			{
				Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("LowCodeAppPage is not initialized.");
			}
		}

		private async Task LogIn(LowCodeApp app)
		{
		    page = await app.NavigateToPageAsync("RAD%20Manager");

		    await page.LoginAsync(Config.Credentials);
		    await page.WaitUntilEverythingIsLoadedAsync();
		}

		private async Task ValidateAddGroupPanelComponents(LowCodeAppPage page)
		{
			var addGroupButton = page.GetComponentByTitle("Add Group");
			await addGroupButton.ClickAsync();

			await page.CheckComponentAvailability(page.GetComponentByText("div", "Close", 0));
			await page.CheckComponentAvailability(page.GetComponentByText("dma-db-component", "Add Relational Anomaly", 0));
			await page.CheckComponentAvailability(page.GetComponentByText("dma-automation-ui div", "What to add?Group", 0));
			await page.CheckComponentAvailability(page.GetComponentByText("Add Relational Anomaly Group"));
			await page.CheckComponentAvailability(page.GetComponentByRole("What to add?"));
			await page.CheckComponentAvailability(page.GetComponentByRole("Add single group "));
			await page.CheckComponentAvailability(page.GetComponentByRole("Group name"));
			await page.CheckComponentAvailability(page.GetComponentByRole("Group name", AriaRole.Row).GetByRole(AriaRole.Cell).Nth(1));
			await page.CheckComponentAvailability(page.GetComponentByRole("Element", true));
			await page.CheckComponentAvailability(page.GetComponentByRole("Parameter", true));
			await page.CheckComponentAvailability(page.GetComponentByRole("Display key filter"));
			await page.CheckComponentAvailability(page.GetComponentByPlaceholder("Filter"));
			await page.CheckComponentAvailability(page.Locator(".is-disabled > .dma-input-group > .dma-input-inner-group").First);
			await page.CheckComponentAvailability(page.GetComponentByRole("Add", true));
			await page.CheckComponentAvailability(page.GetComponentByRole("Remove", true).Locator("div"));
			await page.CheckComponentAvailability(page.GetComponentByRole("No parameters selected", true).Locator("dma-automation-grid-component"));
			await page.CheckComponentAvailability(page.GetComponentByRole("Update model on new data?"));
			await page.CheckComponentAvailability(page.GetComponentByRole("Override default anomaly").Locator("label"));
			await page.CheckComponentAvailability(page.GetComponentByRole("Anomaly threshold", true));
			await page.CheckComponentAvailability(page.Locator("dma-automation-ui").GetByRole(AriaRole.Cell, new() { Name = "6" }));
			await page.CheckComponentAvailability(page.GetComponentByRole("Minimum anomaly duration (in"));
			await page.CheckComponentAvailability(page.GetComponentByRole(": 15").Locator("div").Nth(1));
			await page.CheckComponentAvailability(page.GetComponentByRole("Cancel").Locator("dma-button"));
			await page.CheckComponentAvailability(page.GetComponentByRole("Add group").Locator("div"));
		}

		private async Task AddGroup(LowCodeAppPage page)
		{
			await page.FillInGroupName("PlaywrightGroup");
			await page.AddAndValidateParameter("PlaywrightElement","Value1", "1");
			await page.AddAndValidateParameter("PlaywrightElement", "Value2", "1");
			await page.AddAndValidateParameter("PlaywrightElement", "Value3", "1");

			await page.AddGroup();
			var closeButton = page.GetComponentByText("div", "Close", 0);
			await closeButton.ClickAsync();

			await page.ValidateGroupWasAdded("PlaywrightGroup");
		}

		private async Task ValidateEditGroupPanelComponents(LowCodeAppPage page)
		{
			await page.SelectGroup("PlaywrightGroup");
			var editGroupButton = page.GetComponentByTitle("Edit Group");
			await editGroupButton.ClickAsync();

			await page.CheckComponentAvailability(page.GetComponentByText("Edit Group 'PlaywrightGroup'"));
			await page.CheckComponentAvailability(page.GetComponentByRole("PlaywrightGroup").Locator("div").Nth(1));
			await page.ValidateParameter("PlaywrightElement", "Value1", "1");
			await page.ValidateParameter("PlaywrightElement", "Value2", "1");
			await page.ValidateParameter("PlaywrightElement", "Value3", "1");
		}

		private async Task EditGroup(LowCodeAppPage page)
		{
			await page.EditGroupName("PlaywrightGroup", "PlaywrightGroup_Renamed");

			await page.SelectParameter("PlaywrightElement", "Value3", "1");
			await page.RemoveParameterFromGroup();

			await page.UpdateModel();
			await page.OverrideAnomalyThreshold("10");
			await page.OverrideMinimumAnomalyDuration("10");
			await page.ApplyChanges();
		}

		private async Task ValidateSpecifyTrainingRangePanelComponents(LowCodeAppPage page)
		{
			var specifyTrainingRangeButton = page.GetComponentByTitle("Specify Training Range");
			await specifyTrainingRangeButton.ClickAsync();
			var closeButton = page.GetComponentByText("div", "Close", 0);
			await closeButton.ClickAsync();
		}

		private async Task ValidateRemoveGroupPanelComponents(LowCodeAppPage page)
		{
			await page.SelectGroup("PlaywrightGroup_Renamed");

			var removeGroupButton = page.GetComponentByTitle("Remove Group");
			await removeGroupButton.ClickAsync();

			await page.CheckComponentAvailability(page.GetComponentByText("div", "Close", 0));
			await page.CheckComponentAvailability(page.GetComponentByText("Remove Relational Anomaly"));
			await page.CheckComponentAvailability(page.GetComponentByText("Are you sure you want to"));
		}

		private async Task RemoveGroup(LowCodeAppPage page)
		{
			await page.RemoveGroup();
			await page.ValidateGroupWasRemoved("PlaywrightGroup_Renamed");
		}

		private async Task RemoveGroupsFromPreviousRun(LowCodeAppPage page)
		{
			await page.RemoveGroupIfExists("PlaywrightGroup");
			await page.RemoveGroupIfExists("PlaywrightGroup_Renamed");
		}
    }
}
