namespace RADManager.Tests.Common.Playwright.Pages
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.Playwright;
	using Microsoft.Playwright.MSTest;

	public class RADManagerAppPage : PageTest
	{
		public RADManagerAppPage(IPage page)
		{
			Page = page ?? throw new ArgumentNullException(nameof(page));
		}

		public new IPage Page { get; }

		public ILocator Locator(string selector, PageLocatorOptions options = null)
		{
			return Page.Locator(selector, options);
		}

		public ILocator GetComponentById(int id)
		{
			return Locator($"[id='{id}']");
		}

		public async Task ValidateComponentByTitle(string title)
		{
			var component = Page.GetByTitle(title);
			await Expect(component).ToBeVisibleAsync();
		}

		public async Task ValidateComponentByText(string locatorName, string text, int index)
		{
			var title = Locator(locatorName, new PageLocatorOptions() { HasText = text }).Nth(index);
			if (!await title.IsVisibleAsync())
			{
				// Scroll down the page to bring the element into view
				await Page.Mouse.WheelAsync(0, 10000);
			}

			await Expect(title).ToBeVisibleAsync();
		}

		public async Task CheckNoTryAgainButton()
		{
			var tryAgainButton = Page.Locator("dma-button", new PageLocatorOptions() { HasText = "Try again" }).Nth(0);
			await Expect(tryAgainButton).Not.ToBeVisibleAsync();
		}
	}
}
