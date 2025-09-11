namespace Skyline.DataMiner.RADManager.Tests.Common.Playwright
{
	using System;
	using System.IO;
	using System.Threading.Tasks;

	using Microsoft.Playwright;

	internal class Authentication
	{
		private const int MaxTries = 3;

		public static string StorageStatePath => "./.playwright/.auth/state.json";

		public static async Task LoginAsync(IPage page, Config config)
		{
			await page.Context.Tracing.GroupAsync("Log in");

			try
			{
				for (int attempt = 1; attempt <= MaxTries; attempt++)
				{
					try
					{
						await DaasLoginAsync(page, config);
						await LocalLoginAsync(page, config);

						await Task.WhenAny(
							page.Locator("dma-home").WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible }),
							page.Locator("dma-app-ui").WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible }));

						Directory.CreateDirectory(Path.GetDirectoryName(StorageStatePath));
						await page.Context.StorageStateAsync(new BrowserContextStorageStateOptions() { Path = StorageStatePath });

						return; // Exit if successful
					}
					catch (Exception)
					{
						if (attempt >= MaxTries)
							throw;
						await page.WaitForTimeoutAsync(1000); // Retry delay
					}
				}
			}
			finally
			{
				await page.Context.Tracing.GroupEndAsync();
			}
		}

		public static async Task DaasLoginAsync(IPage page, Config config)
		{
			var loginNeeded = await Task.WhenAny(
				page.GetByText("Sign in with email").WaitForAsync().ContinueWith(_ => true),
				page.Locator("dma-login-screen").WaitForAsync().ContinueWith(_ => false),
				page.Locator("dma-login").WaitForAsync().ContinueWith(_ => false),
				page.Locator("dma-home").WaitForAsync().ContinueWith(_ => false),
				page.Locator("dma-app-ui").WaitForAsync().ContinueWith(_ => false));

			if (!loginNeeded.Result)
				return;

			await page.GetByPlaceholder("Email Address").FillAsync(config.B2cUserName);
			await page.GetByPlaceholder("Password").FillAsync(config.B2cPassword);
			await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions() { Name = "Sign in" }).ClickAsync();
		}

		public static async Task LocalLoginAsync(IPage page, Config config)
		{
			var loginNeeded = await Task.WhenAny(
				page.Locator("dma-login-screen").WaitForAsync().ContinueWith(_ => true),
				page.Locator("dma-login").WaitForAsync().ContinueWith(_ => true),
				page.Locator("dma-home").WaitForAsync().ContinueWith(_ => false),
				page.Locator("dma-app-ui").WaitForAsync().ContinueWith(_ => false));

			if (!loginNeeded.Result)
				return;

			var userNameTextBox = page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions() { Name = "Domain\\User name" });
			if (await userNameTextBox.IsVisibleAsync())
			{
				await userNameTextBox.FillAsync(config.UserName);
			}

			await page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions() { Name = "Password" }).FillAsync(config.Password);

			var keepMeLoggedIn = page.Locator("dma-switch").Filter(new LocatorFilterOptions() { HasTextString = "Keep me logged in" });

			if (await keepMeLoggedIn.IsVisibleAsync())
			{
				var hasCheckedClass = (await keepMeLoggedIn.GetAttributeAsync("class"))?.Contains("checked") == true;

				if (!hasCheckedClass)
				{
					await keepMeLoggedIn.Locator(".switch").ClickAsync();
				}
			}

			var logonButton = page.Locator("dma-button").Filter(new LocatorFilterOptions() { HasTextString = "Log on" });
			await logonButton.ClickAsync();
		}
	}
}
