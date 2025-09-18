namespace RADPlaywright.Components
{
	using Microsoft.Playwright;

	public class DropDownComponent(ILocator dropdownLocator) : ComponentBase(dropdownLocator)
	{
		public async Task SelectOptionAsync(string option)
		{
			var page = Locator.Page;

			await Locator.GetByTestId("select.toggle").ClickAsync();

			var selectOverlay = page.GetByTestId("select.overlay");
			await selectOverlay.Locator("dma-loader").WaitForAsync(new() { State = WaitForSelectorState.Hidden });

			var selectOption = selectOverlay.GetByTestId("select.option").Filter(new() { HasText = option });

			if (!(await selectOption.IsVisibleAsync()))
			{
				var selectInput = selectOverlay.GetByTestId("select.input");
				await selectInput.FillAsync(option);
			}

			await selectOption.ClickAsync();
		}
	}
}
