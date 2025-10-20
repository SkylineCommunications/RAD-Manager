namespace RADPlaywright.Extensions
{
	using Microsoft.Playwright;

	public static class IPageExtensions
	{
		public static IPageAssertions Expect(this IPage page)
		{
			if (page is null)
			{
				throw new ArgumentNullException(nameof(page));
			}

			return Assertions.Expect(page);
		}

		public static LowCodeAppPage AsLowCodeAppPage(this IPage page)
		{
			if (page is null)
			{
				throw new ArgumentNullException(nameof(page));
			}

			return new LowCodeAppPage(page);
		}

		public static async Task WaitUntilEverythingIsLoadedAsync(this IPage page)
		{
			await page.Locator(":root").WaitUntilEverythingIsLoadedAsync();
		}
	}
}
