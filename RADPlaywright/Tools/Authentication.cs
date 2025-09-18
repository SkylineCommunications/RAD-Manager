namespace RADPlaywright.Tools
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.Playwright;

	public class Authentication
	{
		private const int MaxTries = 3;

		public static string StorageStatePath => "./.playwright/.auth/state.json";

		public static async Task LoginAsync(IPage page, Credentials credentials)
		{
			if (page is null)
			{
				throw new ArgumentNullException(nameof(page));
			}

			if (credentials is null)
			{
				throw new ArgumentNullException(nameof(credentials));
			}

			await page.Context.Tracing.GroupAsync("Log in");

			try
			{
				for (int attempt = 1; attempt <= MaxTries; attempt++)
				{
					try
					{
						await DaasLoginAsync(page, credentials);
						await LocalLoginAsync(page, credentials);

						await Task.WhenAny(
							page.Locator("dma-home").WaitForAsync(new() { State = WaitForSelectorState.Visible }),
							page.Locator("dma-app-ui").WaitForAsync(new() { State = WaitForSelectorState.Visible }));

						Directory.CreateDirectory(Path.GetDirectoryName(StorageStatePath));
						await page.Context.StorageStateAsync(new() { Path = StorageStatePath });

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

		public static async Task DaasLoginAsync(IPage page, Credentials credentials)
		{
			if (page is null)
			{
				throw new ArgumentNullException(nameof(page));
			}

			if (credentials is null)
			{
				throw new ArgumentNullException(nameof(credentials));
			}

			var loginNeeded = await Task.WhenAny(
				page.GetByText("Sign in with email").WaitForAsync().ContinueWith(_ => true),
				page.Locator("dma-login-screen").WaitForAsync().ContinueWith(_ => false),
				page.Locator("dma-login").WaitForAsync().ContinueWith(_ => false),
				page.Locator("dma-home").WaitForAsync().ContinueWith(_ => false),
				page.Locator("dma-app-ui").WaitForAsync().ContinueWith(_ => false));

			if (!loginNeeded.Result)
				return;

			await page.GetByPlaceholder("Email Address").FillAsync(credentials.Username);
			await page.GetByPlaceholder("Password").FillAsync(credentials.Password);
			await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
		}

		public static async Task LocalLoginAsync(IPage page, Credentials credentials)
		{
			if (page is null)
			{
				throw new ArgumentNullException(nameof(page));
			}

			if (credentials is null)
			{
				throw new ArgumentNullException(nameof(credentials));
			}

			var loginNeeded = await Task.WhenAny(
				page.Locator("dma-login-screen").WaitForAsync().ContinueWith(_ => true),
				page.Locator("dma-login").WaitForAsync().ContinueWith(_ => true),
				page.Locator("dma-home").WaitForAsync().ContinueWith(_ => false),
				page.Locator("dma-app-ui").WaitForAsync().ContinueWith(_ => false));

			if (!loginNeeded.Result)
				return;

			var userNameTextBox = page.GetByRole(AriaRole.Textbox, new() { Name = "Username" });
			if (await userNameTextBox.IsVisibleAsync())
			{
				await userNameTextBox.FillAsync(credentials.Username);
			}

			await page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(credentials.Password);

			var keepMeLoggedIn = page.Locator("dma-switch").Filter(new() { HasTextString = "Keep me logged in" });

			if (await keepMeLoggedIn.IsVisibleAsync())
			{
				var hasCheckedClass = (await keepMeLoggedIn.GetAttributeAsync("class"))?.Contains("checked") == true;

				if (!hasCheckedClass)
				{
					await keepMeLoggedIn.Locator(".switch").ClickAsync();
				}
			}

			var logonButton = page.GetByRole(AriaRole.Button, new() { Name = "Sign in"});
			await logonButton.ClickAsync();
		}

		public class Credentials
		{
			public Credentials(string username, string password)
			{
				Username = username;
				Password = password;
			}

			public string Username { get; }

			public string Password { get; }
		}
	}
}
