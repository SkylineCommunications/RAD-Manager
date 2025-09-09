namespace Skyline.DataMiner.RADManager.Tests.Common.Playwright.IntegrationTest
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Logging;
	using Microsoft.Playwright;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.RADManager.Tests.Common.IntegrationTest;

	public abstract class PlaywrightIntegrationTest : IntegrationTest
	{
		private Lazy<IConnection>? lazyConnection;
		private IConnection? providedConnection;

		protected PlaywrightIntegrationTest(TestContext testContext)
		{
			TestContext = testContext ?? throw new ArgumentNullException(nameof(testContext));

			Config = Config.Load();
			lazyConnection = new Lazy<IConnection>(CreateConnection);
		}

		protected PlaywrightIntegrationTest(IConnection connection)
		{
			Config = Config.Load();
			providedConnection = connection;
		}

		public override IConnection Connection => providedConnection ?? lazyConnection!.Value;

		protected Config Config { get; private set; }

		protected IPlaywright? Playwright { get; private set; }

		protected IBrowser? Browser { get; private set; }

		protected IBrowserContext? Context { get; private set; }

		protected TestContext? TestContext { get; set; }

		public override void Cleanup()
		{
			Task.Run(async () =>
			{
				await CleanupBrowser();
			}).GetAwaiter().GetResult();
		}

		public override void Initialize()
		{
			Task.Run(async () =>
			{
				await InitializeBrowser();
			}).GetAwaiter().GetResult();
		}

		private IConnection CreateConnection()
		{
			var connection = ConnectionSettings.GetConnection(Config.BaseUrl);
			connection.Authenticate(Config.UserName, Config.Password);

			return connection;
		}

		private async Task InitializeBrowser()
		{
			Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
			Playwright.Selectors.SetTestIdAttribute("data-cy");
			try
			{
				Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
				{
					Headless = !Debugger.IsAttached,
				});

				Context = await Browser.NewContextAsync(new BrowserNewContextOptions
				{
					BaseURL = Config.BaseUrl,
					ViewportSize = new ViewportSize
					{
						Width = 1920,
						Height = 1080,
					},
					StorageStatePath = File.Exists(Authentication.StorageStatePath) ? Authentication.StorageStatePath : null,
				});

				await Context.Tracing.StartAsync(new TracingStartOptions
				{
					Title = $"{TestContext?.FullyQualifiedTestClassName}.{TestContext?.TestName}",
					Screenshots = true,
					Snapshots = true,
					Sources = true,
				});
			}
			catch (Exception ex)
			{
				Logger?.LogError(ex, "Failed to launch browser.");
				throw;
			}
		}

		private async Task CleanupBrowser()
        {
            if (Context != null)
            {
                if (TestContext != null && (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed || Debugger.IsAttached))
                {
                    var tracePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "playwright-traces",
                        $"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}.zip");

                    await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
                }
                else
                {
                    await Context.Tracing.StopAsync();
                }

                await Context.CloseAsync();
            }

            if (Browser != null)
            {
                await Browser.CloseAsync();
            }

            Playwright?.Dispose();
        }
	}
}