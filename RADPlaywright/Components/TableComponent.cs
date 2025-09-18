namespace RADPlaywright.Components
{
	using Microsoft.Playwright;
	using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

	public class TableComponent(ILocator tableLocator) : ComponentBase(tableLocator)
	{
		public ILocator GetHeaderRowLocator()
		{
			var header = Locator.GetByTestId("virtualised-table.header");
			var headerRow = header.Locator("tr.header");

			return headerRow;
		}

		public ILocator GetDataRowsLocator()
		{
			var body = Locator.GetByTestId("virtualised-table.body");
			var rows = body.Locator("tr:not(.header, .buffer)");

			return rows;
		}

		public async Task<string[]> GetHeaderAsync()
		{
			var headerRow = GetHeaderRowLocator();

			return (await headerRow.Locator("th:not(.buffer)").AllTextContentsAsync()).ToArray();
		}

		public async IAsyncEnumerable<string[]> GetRowsDataAsync()
		{
			var rows = GetDataRowsLocator();

			foreach (var row in await rows.AllAsync())
			{
				yield return (await row.Locator("td:not(.buffer)").AllTextContentsAsync()).ToArray();
			}
		}

		public async Task SearchAsync(string text)
		{
			var componentHeader = Locator.Locator("dma-db-component-header");

			var searchIcon = componentHeader.GetByTestId("search");
			await searchIcon.ClickAsync();

			var searchInput = componentHeader.Locator("dwa-search-box").GetByTestId("select.input");
			await searchInput.FillAsync(text);
		}

		public async Task ClearSearchAsync()
		{
			var componentHeader = Locator.Locator("dma-db-component-header");

			var searchIcon = componentHeader.GetByTitle("Clear and close search");
			await searchIcon.ClickAsync();
		}
	}
}
