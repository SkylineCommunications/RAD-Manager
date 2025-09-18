namespace RADPlaywright.Extensions
{
	using System.Text.RegularExpressions;

	using Microsoft.Playwright;
	using RADPlaywright.Components;

	public static class ILocatorExtensions
	{
		public static ILocatorAssertions Expect(this ILocator locator)
		{
			if (locator is null)
			{
				throw new ArgumentNullException(nameof(locator));
			}

			return Assertions.Expect(locator);
		}

		public static async Task WaitUntilEverythingIsLoadedAsync(this ILocator locator)
		{
			if (locator is null)
			{
				throw new ArgumentNullException(nameof(locator));
			}

			var loaderSelectors = new[]
			{
				"dma-loader",
				"dma-loader-bar",
				"div.skeleton",
				"div.skeleton-cell",
				"div.loader-icon",
			};

			var combinedSelector = String.Join(", ", loaderSelectors);
			var loaderLocator = locator.Locator(combinedSelector);

			await Assertions.Expect(loaderLocator).ToHaveCountAsync(0);
		}

		public static TableComponent AsTableComponent(this ILocator locator)
		{
			if (locator is null)
			{
				throw new ArgumentNullException(nameof(locator));
			}

			return new TableComponent(locator);
		}

		public static DropDownComponent AsDropDownComponent(this ILocator locator)
		{
			if (locator is null)
			{
				throw new ArgumentNullException(nameof(locator));
			}

			return new DropDownComponent(locator);
		}

		public static ILocator GetComponentById(this ILocator locator, int id)
		{
			if (locator is null)
			{
				throw new ArgumentNullException(nameof(locator));
			}

			return locator.Locator($"dma-db-component[id='{id}']");
		}

		public static ILocator GetComponentByTitle(this ILocator locator, string title)
		{
			if (locator is null)
			{
				throw new ArgumentNullException(nameof(locator));
			}

			var page = locator.Page;
			var titleLocator = page.Locator($"dma-db-component-header div.component-title", new() { HasTextRegex = new Regex($"^{Regex.Escape(title)}$") });

			return locator.Locator("dma-db-component", new() { Has = titleLocator });
		}

		public static async Task PressDmaButtonAsync(this ILocator locator, string text)
		{
			if (locator is null)
			{
				throw new ArgumentNullException(nameof(locator));
			}

			await locator
				.Locator("dma-button", new() { HasTextRegex = new Regex($"^{Regex.Escape(text)}$") })
				.ClickAsync();
		}

		public static async Task PressHeaderButtonAsync(this ILocator locator, string text)
		{
			if (locator is null)
			{
				throw new ArgumentNullException(nameof(locator));
			}

			var headerBar = locator.Locator("dma-app-headerbar");
			var button = headerBar.GetByTitle(text, new() { Exact = true });

			await button.ClickAsync();
		}
	}
}
