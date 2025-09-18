namespace RADPlaywright
{
	using System.Text.RegularExpressions;
	using Microsoft.Playwright;
	using RADPlaywright.Extensions;
	using RADPlaywright.Tools;

	public class LowCodeAppPage
	{
		public LowCodeAppPage(IPage page)
		{
			Page = page ?? throw new ArgumentNullException(nameof(page));
		}

		public IPage Page { get; }

		public ILocator Body => Page.Locator("body");

		public async Task LoginAsync(Authentication.Credentials credentials)
		{
			if (credentials is null)
			{
				throw new ArgumentNullException(nameof(credentials));
			}

			await Authentication.LoginAsync(Page, credentials);
		}

		public ILocator Locator(string selector, PageLocatorOptions options = null)
		{
			return Page.Locator(selector, options);
		}

		public ILocator GetComponents()
		{
			return Locator("dma-db-component");
		}

		public ILocator GetComponentById(int id)
		{
			return Locator($"dma-db-component[id='{id}']");
		}

		public ILocator GetComponentByTitle(string title)
		{
			var titleLocator = Locator($"dma-db-component-header div.component-title", new() { HasTextRegex = new Regex($"^{Regex.Escape(title)}$") });

			return Locator("dma-db-component", new() { Has = titleLocator });
		}

		public virtual async Task WaitUntilEverythingIsLoadedAsync()
		{
			await Page.WaitUntilEverythingIsLoadedAsync();
		}

		public async Task<IEnumerable<string>> GetSidebarPagesAsync()
		{
			var sidebarTabs = await Page.GetByTestId("app-sidebar.sidebar-tab").AllAsync();

			var titleTasks = sidebarTabs
				.Select(tab => tab.Locator("i").GetAttributeAsync("title"))
				.ToArray();

			var titles = await Task.WhenAll(titleTasks);

			return titles.Where(title => !String.IsNullOrEmpty(title));
		}
	}
}
