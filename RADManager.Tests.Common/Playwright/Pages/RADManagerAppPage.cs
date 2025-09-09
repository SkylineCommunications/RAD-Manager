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

		public IPage Page { get; }

		/// <summary>
		/// Returns a locator for the specified selector and options.
		/// </summary>
		/// <param name="selector">The selector string.</param>
		/// <param name="options">Optional locator options.</param>
		/// <returns>An ILocator for the selector.</returns>
		public ILocator Locator(string selector, PageLocatorOptions options = null)
		{
			return Page.Locator(selector, options);
		}

		/// <summary>
		/// Returns a locator for a component by its id.
		/// </summary>
		/// <param name="id">The id of the component.</param>
		/// <returns>An ILocator for the component.</returns>
		public ILocator GetComponentById(int id)
		{
			return Locator($"[id='{id}']");
		}

		/// <summary>
		/// Validates that a component with the specified title is visible.
		/// </summary>
		/// <param name="title">The title of the component.</param>
		public async Task ValidateComponentByTitle(string title)
		{
			var component = Page.GetByTitle(title);
			await Expect(component).ToBeVisibleAsync();
		}

		/// <summary>
		/// Validates that a component with the specified locator name and text at the given index is visible.
		/// Scrolls down the page if the component is not visible.
		/// </summary>
		/// <param name="locatorName">The locator name.</param>
		/// <param name="text">The text to match.</param>
		/// <param name="index">The index of the component.</param>
		public async Task ValidateComponentByText(string locatorName, string text, int index)
		{
			var title = Locator(locatorName, new() { HasText = text }).Nth(index);
			if (!await title.IsVisibleAsync())
			{
				// Scroll down the page to bring the element into view
				await Page.Mouse.WheelAsync(0, 10000);
			}
			await Expect(title).ToBeVisibleAsync();
		}

		public async Task CheckNoTryAgainButton()
		{
			var tryAgainButton = Page.Locator("dma-button", new() { HasText = "Try again" }).Nth(0);
			await Expect(tryAgainButton).Not.ToBeVisibleAsync();
		}
	}
}
