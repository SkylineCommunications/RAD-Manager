namespace PlaywrightTests.TestCases
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.Playwright;
	using RADManager.Tests.Common.Playwright.Apps;
	using RADManager.Tests.Common.Playwright.Pages;
	using Skyline.DataMiner.RADManager.Tests.Common.IntegrationTest;

	public class RAD_UserInterface_Validation : TestCase
	{
		private readonly RADManagerApp app;

		public RAD_UserInterface_Validation(RADManagerApp app)
		{
			this.app = app ?? throw new ArgumentNullException(nameof(app));
		}

		public override string Name => "Validates the main user interface";

		public override TimeSpan MaxExecutionTime => TimeSpan.FromSeconds(10);

		public IPage Page { get; set; }

		private RADManagerAppPage AppPage => new RADManagerAppPage(Page);

		public override bool Execute()
		{
			Task.Run(async () =>
			{
				await LoadInitialPageAsync();
				await TestSteps();
			}).GetAwaiter().GetResult();
			return true;
		}

		private async Task TestSteps()
		{
			// Validate that the title "RAD MANAGER" is visible on the page
			await AppPage.ValidateComponentByText("div", "RAD MANAGER", 3);

			// Validate that the "DataMiner Docs" link is visible on the page
			await AppPage.ValidateComponentByText("div", "DataMiner Docs", 0);

			// Validate that the "Add Group" button is visible on the page
			await AppPage.ValidateComponentByTitle("Add Group");

			// Validate that the "Edit Group" button is visible on the page
			await AppPage.ValidateComponentByTitle("Edit Group");

			// Validate that the "Remove Group" button is visible on the page
			await AppPage.ValidateComponentByTitle("Remove Group");

			// Validate that the "Specify Training Range" button is visible on the page
			await AppPage.ValidateComponentByTitle("Specify Training Range");

			// Validate that "Your Relational Anomaly Groups" text is visible on the page
			await AppPage.ValidateComponentByText("#db-component-1", "Your Relational Anomaly Groups", 0);

			// Validate that there is no GQI error message on the page by checking that the "Try again" button is not visible
			await AppPage.CheckNoTryAgainButton();

			// Validate that "Parameters in the selected group" text is visible on the page
			await AppPage.ValidateComponentByText("#db-component-3", "Group Information", 0);

			// Validate that "Time range to show on trend graphs" text is visible on the page
			await AppPage.ValidateComponentByText("#db-component-8", "Time range to show on trend graphs", 0);

			// Validate that "Trend graph of selected parameters" text is visible on the page
			await AppPage.ValidateComponentByText("#db-component-4", "Trend graph of selected parameters", 0);

			// Validate that "Inspect the anomaly score of your group" text is visible on the page
			await AppPage.ValidateComponentByText("#db-component-7", "Inspect the anomaly score of your group", 0);
		}

		private async Task LoadInitialPageAsync()
		{
			var poolsPage = await app.NavigateToPageAsync(app.Name);
			await poolsPage.WaitUntilEverythingIsLoadedAsync();
			Page = poolsPage.Page;
		}
	}
}
