namespace RADPlaywright.Components
{
	using System;

	using Microsoft.Playwright;

	public abstract class ComponentBase
	{
		protected ComponentBase(ILocator locator)
		{
			Locator = locator ?? throw new ArgumentNullException(nameof(locator));
		}

		public ILocator Locator { get; }

		public ILocatorAssertions Expect()
		{
			return Assertions.Expect(Locator);
		}
	}
}
