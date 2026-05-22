namespace RADPlaywright.Apps
{
	using System;

	using Microsoft.Playwright;

	public class LowCodeApp
	{
		private readonly IBrowserContext _browserContext;

		public LowCodeApp(IBrowserContext browserContext, Guid id)
		{
			_browserContext = browserContext ?? throw new ArgumentNullException(nameof(browserContext));

			ID = id;
		}

		public Guid ID { get; }

		public async Task<LowCodeAppPage> NavigateToInitialPageAsync()
		{
			var page = await _browserContext.NewPageAsync();
			await page.GotoAsync($"/app/{ID}", new() { WaitUntil = WaitUntilState.Load });

			return new LowCodeAppPage(page);
		}

		public async Task<LowCodeAppPage> NavigateToPageAsync()
		{
			var page = await _browserContext.NewPageAsync();
			await page.GotoAsync($"/app/{ID}");

			return new LowCodeAppPage(page);
		}
	}
}
