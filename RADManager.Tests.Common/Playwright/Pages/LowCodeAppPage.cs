namespace Skyline.DataMiner.RADManager.Tests.Common.Playwright
{
	using System;
	using System.Threading.Tasks;

	using Microsoft.Playwright;

	public class LowCodeAppPage
	{
		public LowCodeAppPage(IPage page)
		{
			Page = page ?? throw new ArgumentNullException(nameof(page));
		}

		public IPage Page { get; }

		public ILocator Body => Page.Locator("body");

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
			var titleLocator = Locator($"dma-db-component-header div.component-title", new PageLocatorOptions() { HasText = title });

			return Locator("dma-db-component", new PageLocatorOptions() { Has = titleLocator });
		}

		public virtual async Task WaitUntilEverythingIsLoadedAsync()
		{
			var loaderSelectors = new[]
			{
				"dma-loader",
				"dma-loader-bar",
				"div.skeleton",
				"div.skeleton-cell",
				"div.loader-icon",
			};

			var combinedSelector = String.Join(", ", loaderSelectors);
			var loaderLocator = Page.Locator(combinedSelector);

			await Assertions.Expect(loaderLocator).ToHaveCountAsync(0);
		}
	}
}
