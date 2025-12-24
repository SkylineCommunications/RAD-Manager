namespace RADPlaywright
{
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using Microsoft.Playwright;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
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

		public ILocator Locator(string selector, PageLocatorOptions? options = null)
		{
			return Page.Locator(selector, options);
		}

		public ILocator GetComponentById(string id)
		{
			return Locator(id);
		}

		public ILocator GetComponentById(int id)
		{
			return Locator($"dma-db-component[id='{id}']");
		}

		public ILocator GetComponentByTitle(string title)
		{
			return Page.GetByTitle(title);
		}

		public ILocator GetComponentByText(string locatorName, string text, int index)
		{
			return Locator(locatorName, new PageLocatorOptions() { HasText = text }).Nth(index);
		}

		public ILocator GetComponentByText(string text)
		{
			return Page.GetByText(text);
		}

		public ILocator GetComponentByRole(string text, AriaRole role = AriaRole.Cell)
		{
			return Page.GetByRole(role, new() { Name = text });
		}

		public ILocator GetComponentByPlaceholder(string placeholder, PageGetByPlaceholderOptions? options = null)
		{
			return Page.GetByPlaceholder(placeholder, options);
		}

		public ILocator GetComponentByRole(string text, bool? exact, AriaRole role = AriaRole.Cell)
		{
			return Page.GetByRole(role, new() { Name = text, Exact = exact});
		}

		public virtual async Task WaitUntilEverythingIsLoadedAsync()
		{
			await Page.WaitUntilEverythingIsLoadedAsync();
		}

		public async Task<IEnumerable<string?>> GetSidebarPagesAsync()
		{
			var sidebarTabs = await Page.GetByTestId("app-sidebar.sidebar-tab").AllAsync();

			var titleTasks = sidebarTabs
				.Select(tab => tab.Locator("i").GetAttributeAsync("title"))
				.ToArray();

			var titles = await Task.WhenAll(titleTasks);

			return titles.Where(title => !String.IsNullOrEmpty(title));
		}

		public async Task WaitOnDataMinerDocsPage()
		{
			var dataMinerDocsPage = await Page.RunAndWaitForPopupAsync(async () =>
			{
				await Page.GetByTitle("DataMiner Docs").ClickAsync();
			});

			await dataMinerDocsPage.GetByRole(AriaRole.Heading, new() { Name = "Working with the RAD Manager" }).WaitForAsync();
			Assert.IsTrue(await dataMinerDocsPage.GetByRole(AriaRole.Heading, new() { Name = "Working with the RAD Manager" }).IsVisibleAsync());
		}

		public async Task TypeFromKeyboard(string text)
		{
			await Page.Keyboard.TypeAsync(text);
			await Page.Keyboard.PressAsync("Enter");
		}

		public async Task FillInGroupName(string groupName)
		{
			var addGroupPanel = Page.Locator("dma-db-component").Filter(new() { HasText = "Add Relational Anomaly" });
			var groupNameTextBox = addGroupPanel.GetByRole(AriaRole.Row, new() { Name = "Group name" }).GetByRole(AriaRole.Cell).Nth(1).GetByRole(AriaRole.Textbox, new() { Name = " " });
			Assert.IsTrue(await groupNameTextBox.IsEditableAsync());
			await groupNameTextBox.FillAsync(groupName);
		}

		public async Task SelectOptionInDropDown(string option, int dropDownIndex)
		{
			var addGroupPanel = Page.Locator("dma-db-component").Filter(new() { HasText = "Add Relational Anomaly" });

			var elementDropDown = addGroupPanel.GetByRole(AriaRole.Cell, new() { Name = " " }).Locator("i").Nth(dropDownIndex);
			await elementDropDown.ClickAsync();
			await TypeFromKeyboard(option);
		}

		public async Task FillInDisplayKey(string option)
		{
			var addGroupPanel = Page.Locator("dma-db-component").Filter(new() { HasText = "Add Relational Anomaly" });
			await Page.Locator("td:nth-child(3) > .grid-component > .ng-star-inserted > .dma-input-group > .dma-input-inner-group").ClickAsync();
			await Page.Keyboard.PressAsync("ControlOrMeta+a");
			await TypeFromKeyboard(option);
		}

		public async Task ValidateParameter(string elementName, string parameter, string displayKey)
		{
			await CheckComponentAvailability(Page.Locator("div").Filter(new() { HasTextRegex = new Regex($"^{elementName}\\/{parameter}\\/{displayKey}$") }).Nth(1));
		}

		public async Task SelectParameter(string elementName, string parameter, string displayKey)
		{
			var p = Page.Locator("div").Filter(new() { HasTextRegex = new Regex($"^{elementName}\\/{parameter}\\/{displayKey}$") }).Nth(1);
			await p.ScrollIntoViewIfNeededAsync();
			await p.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			await p.ClickAsync();
		}

		public async Task RemoveParameterFromGroup()
		{
			await GetComponentByTitle("Remove the instance(s)").ClickAsync();
		}

		public async Task AddAndValidateParameter(string elementName, string parameter, string displayKey)
		{
			await SelectOptionInDropDown(elementName, 1);
			await SelectOptionInDropDown(parameter, 2);
			await FillInDisplayKey(displayKey);
			await GetComponentByRole("Add", true).ClickAsync();
			await ValidateParameter(elementName, parameter, displayKey);
		}

		public async Task CheckComponentAvailability(ILocator component)
		{
			await component.ScrollIntoViewIfNeededAsync();
			await component.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
			Assert.IsTrue(await component.IsVisibleAsync());
		}

		public async Task AddGroup()
		{
			await GetComponentByTitle("Add the relational anomaly group specified above to the RAD configuration").ClickAsync();
		}

		public async Task ValidateGroupWasAdded(string groupName)
		{
			var relationalAnomalyGroups = GetComponentById(1).First;
			await relationalAnomalyGroups.ClickAsync();
			var timeout = TimeSpan.FromMinutes(1);
			var start = DateTime.UtcNow;
			while (! await relationalAnomalyGroups.GetByText(groupName).IsVisibleAsync())
			{
				// Scroll down by a fixed amount (e.g., 500 pixels)
				await Page.Mouse.WheelAsync(1,10000);

				// Optionally, wait a bit for lazy-loaded content
				await Task.Delay(200);

				if (DateTime.UtcNow - start > timeout)
					throw new TimeoutException($"{groupName} not found within timeout.");
			}

			relationalAnomalyGroups.ScrollIntoViewIfNeededAsync().Wait();
			Assert.IsTrue(await relationalAnomalyGroups.GetByText(groupName).IsVisibleAsync());
		}

		public async Task SelectGroup(string groupName)
		{
			var relationalAnomalyGroups = GetComponentById(1).First;
			await relationalAnomalyGroups.ClickAsync();
			var timeout = TimeSpan.FromMinutes(1);
			var start = DateTime.UtcNow;
			while (!await relationalAnomalyGroups.GetByText(groupName).IsVisibleAsync())
			{
				// Scroll down by a fixed amount (e.g., 500 pixels)
				await Page.Mouse.WheelAsync(1, 10000);

				// Optionally, wait a bit for lazy-loaded content
				await Task.Delay(200);

				if (DateTime.UtcNow - start > timeout)
					throw new TimeoutException($"{groupName} not found within timeout.");
			}

			relationalAnomalyGroups.ScrollIntoViewIfNeededAsync().Wait();
			await relationalAnomalyGroups.GetByText(groupName).ClickAsync();
		}

		public async Task UpdateModel()
		{
			await Locator("div").Filter(new() { HasTextRegex = new Regex("^Update model on new data\\?$") }).Locator("div").ClickAsync();
		}

		public async Task OverrideAnomalyThreshold(string threshold)
		{
			await Locator("div").Filter(new() { HasTextRegex = new Regex("^Override default anomaly threshold\\?$") }).Locator("div").ClickAsync();
			await Locator("dma-automation-ui").GetByRole(AriaRole.Cell, new() { Name = "6" }).ClickAsync();
			await Page.Keyboard.PressAsync("ControlOrMeta+a");
			await TypeFromKeyboard(threshold);
		}

		public async Task OverrideMinimumAnomalyDuration(string minutes)
		{
			await Locator("div").Filter(new() { HasTextRegex = new Regex("^Override default minimum anomaly duration\\?$") }).Locator("div").ClickAsync();
			await GetComponentByRole("mm", true, AriaRole.Textbox).ClickAsync();
			await GetComponentByRole("mm", true, AriaRole.Textbox).PressAsync("ArrowRight");
			await GetComponentByRole("mm", true, AriaRole.Textbox).FillAsync(minutes);
		}

		public async Task ApplyChanges()
		{
			await GetComponentByTitle("Edit the selected relational").ClickAsync();
		}

		public async Task EditGroupName(string groupName, string newGoupName)
		{
			var editPanel = Locator("dma-db-component").Filter(new() { HasText = $"Edit Group '{groupName}'" });
			var groupNameTextBox = editPanel.GetByRole(AriaRole.Row, new() { Name = "Group name" }).GetByRole(AriaRole.Cell).Nth(1).GetByRole(AriaRole.Textbox, new() { Name = " " });
			Assert.IsTrue(await groupNameTextBox.IsEditableAsync());
			await Page.Keyboard.PressAsync("ControlOrMeta+a");
			await groupNameTextBox.FillAsync(newGoupName);
		}

		public async Task RemoveGroup()
		{
			await GetComponentByRole("Yes").Locator("dma-button").ClickAsync();
		}

		public async Task ValidateGroupWasRemoved(string groupName)
		{
			var relationalAnomalyGroups = GetComponentById(1).First;
			await relationalAnomalyGroups.ClickAsync();
			var timeout = TimeSpan.FromSeconds(30);
			var start = DateTime.UtcNow;
			while (await relationalAnomalyGroups.GetByText(groupName).IsVisibleAsync())
			{
				// Scroll down by a fixed amount (e.g., 500 pixels)
				await Page.Mouse.WheelAsync(1, 10000);

				// Optionally, wait a bit for lazy-loaded content
				await Task.Delay(200);

				if (DateTime.UtcNow - start > timeout)
					throw new TimeoutException($"{groupName} not deleted within timeout.");
			}

			Assert.IsFalse(await relationalAnomalyGroups.GetByText(groupName).IsVisibleAsync());
		}

		public async Task RemoveGroupIfExists(string groupName)
		{
			var relationalAnomalyGroups = GetComponentById(1).First;
			await relationalAnomalyGroups.ClickAsync();
			var timeout = TimeSpan.FromSeconds(10);
			var start = DateTime.UtcNow;
			while (await relationalAnomalyGroups.GetByText(groupName).IsVisibleAsync())
			{
				// Scroll down by a fixed amount (e.g., 500 pixels)
				await Page.Mouse.WheelAsync(1, 10000);

				// Optionally, wait a bit for lazy-loaded content
				await Task.Delay(200);

				if ((DateTime.UtcNow - start > timeout) && relationalAnomalyGroups.GetByText(groupName).IsVisibleAsync().Result)
				{
					await relationalAnomalyGroups.GetByText(groupName).ClickAsync();
					var removeGroupButton = GetComponentByTitle("Remove Group");
					await removeGroupButton.ClickAsync();
					await RemoveGroup();
					return;
				}
			}
		}
	}
}
