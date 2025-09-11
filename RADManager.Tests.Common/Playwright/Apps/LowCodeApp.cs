namespace Skyline.DataMiner.RADManager.Tests.Common.Playwright
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Microsoft.Playwright;

	public abstract class LowCodeApp
	{
		private readonly IBrowserContext _browserContext;

		protected LowCodeApp(IBrowserContext browserContext, Config config)
		{
			_browserContext = browserContext ?? throw new ArgumentNullException(nameof(browserContext));
			Config = config ?? throw new ArgumentNullException(nameof(config));
		}

		public abstract Guid ID { get; }

		public abstract String Name { get; }

		protected Config Config { get; }

		public static async Task<IEnumerable<string>> GetSidebarPagesAsync(LowCodeAppPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            var sidebarTabs = await page.Body.GetByTestId("app-sidebar.sidebar-tab").AllAsync();

            var titleTasks = sidebarTabs
            .Select(tab => tab.Locator("i").GetAttributeAsync("title"))
            .ToArray();

            var titles = await Task.WhenAll(titleTasks);

            return titles.Where(title => !String.IsNullOrEmpty(title));
        }

		public async Task<LowCodeAppPage> NavigateToInitialPageAsync()
		{
			var page = await _browserContext.NewPageAsync();
			await GotoAndLoginAsync(page, $"/app/{ID}", new PageGotoOptions { WaitUntil = WaitUntilState.Load });

			return new LowCodeAppPage(page);
		}

		public async Task<LowCodeAppPage> NavigateToPageAsync(string title)
		{
			if (String.IsNullOrWhiteSpace(title))
			{
				throw new ArgumentException($"'{nameof(title)}' cannot be null or whitespace.", nameof(title));
			}

			var page = await _browserContext.NewPageAsync();
			await GotoAndLoginAsync(page, $"https:/adelinasp.skyline.local/app/{ID}/{Uri.EscapeDataString(title)}");

			return new LowCodeAppPage(page);
		}

		public async Task<IReadOnlyCollection<LowCodeAppPage>> GetAllPages()
		{
			var page = await NavigateToInitialPageAsync();

			var subPageTitles = (await GetSidebarPagesAsync(page)).ToList();

			if (subPageTitles.Count == 0)
			{
				return new[] { page };
			}

			var subPageTasks = subPageTitles.Select(x => NavigateToPageAsync(x));
			var subPages = await Task.WhenAll(subPageTasks);

			return subPages;
		}

		private async Task<IResponse> GotoAndLoginAsync(IPage page, string url, PageGotoOptions options = default)
        {
            var result = await page.GotoAsync(url, options);
            await Authentication.LoginAsync(page, Config);

            return result;
        }
	}
}
