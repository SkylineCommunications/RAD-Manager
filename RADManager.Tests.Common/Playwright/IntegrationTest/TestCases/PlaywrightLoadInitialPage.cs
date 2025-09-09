namespace Skyline.DataMiner.RADManager.Tests.Common.Playwright.IntegrationTest.TestCases
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.Playwright;
	using Skyline.DataMiner.RADManager.Tests.Common.IntegrationTest;

	public class PlaywrightLoadInitialPage : TestCase
	{
		private readonly LowCodeApp app;
		public PlaywrightLoadInitialPage(LowCodeApp app)
		{
			this.app = app ?? throw new ArgumentNullException(nameof(app));
		}

		public override string Name => $"Load Initial page for {app.Name}";

		public override TimeSpan MaxExecutionTime => TimeSpan.FromSeconds(10);

		public override bool Execute()
		{
			Task.Run(async () =>
			{
		       await LoadInitialPageAsync();
			}).GetAwaiter().GetResult();
			return true;
		}

		private async Task LoadInitialPageAsync()
		{
			var poolsPage = await app.NavigateToPageAsync(app.Name);
			await poolsPage.WaitUntilEverythingIsLoadedAsync();
		}
	}
}
