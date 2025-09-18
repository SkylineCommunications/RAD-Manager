namespace RADPlaywright
{
	using System;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using Microsoft.Playwright;
	using Microsoft.Playwright.TestAdapter;
	using RADPlaywright.Tools;

	public abstract class PlaywrightTestBase
	{
		private static readonly SemaphoreSlim _semaphore = new(1, 1);

		private static IPlaywright _playwright;
		private static IBrowser _browser;

		protected TestContext TestContext { get; }

		protected Config Config { get; }

		protected IBrowserContext Context { get; private set; }

		protected async Task<IPage> CreatePage()
		{
			return await Context.NewPageAsync();
		}

		public PlaywrightTestBase(TestContext testContext)
		{
			TestContext = testContext ?? throw new ArgumentNullException(nameof(testContext));

			Config = Config.Load();
		}

		[TestInitialize]
		public async Task TestInitialize()
		{
			await _semaphore.WaitAsync(TestContext.CancellationTokenSource.Token);

			try
			{
				if (_playwright == null)
				{
					_playwright = await Playwright.CreateAsync();
					_playwright.Selectors.SetTestIdAttribute("data-cy");
				}

				if (_browser == null)
				{
					_browser = await LaunchBrowserAsync();
				}

				Context = await _browser.NewContextAsync(new()
				{
					BaseURL = Config.BaseUrl,
					ViewportSize = new ViewportSize
					{
						Width = 1920,
						Height = 1080,
					},
					StorageStatePath = File.Exists(Authentication.StorageStatePath)
						? Authentication.StorageStatePath
						: null,
				});

				await Context.Tracing.StartAsync(new()
				{
					Title = $"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}",
					Screenshots = true,
					Snapshots = true,
					Sources = true,
				});
			}
			finally
			{
				_semaphore.Release();
			}
		}

		[TestCleanup]
		public async Task TestCleanup()
		{
			if (Context == null)
			{
				return;
			}

			if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed || Debugger.IsAttached)
			{
				var tracePath = Path.Combine(
					Directory.GetCurrentDirectory(),
					"playwright-traces",
					$"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}.zip");

				await Context.Tracing.StopAsync(new() { Path = tracePath });
			}
			else
			{
				await Context.Tracing.StopAsync();
			}

			await Context.CloseAsync();
			Context = null;
		}

		/// <summary>
		/// Should be called from [AssemblyCleanup] to cleanup Playwright properly.
		/// </summary>
		public static async Task CleanupAsync()
		{
			if (_browser != null)
			{
				await _browser.CloseAsync();
				_browser = null;
			}

			_playwright?.Dispose();
			_playwright = null;
		}

		private static async Task<IBrowser> LaunchBrowserAsync()
		{
			var launchOptions = new BrowserTypeLaunchOptions
			{
				Headless = !Debugger.IsAttached,
			};

			try
			{
				return await _playwright.Chromium.LaunchAsync(launchOptions);
			}
			catch (PlaywrightException ex)
				when (ex.Message.Contains("Please run the following command to download new browsers"))
			{
				InstallBrowsers();

				// Retry
				return await _playwright.Chromium.LaunchAsync(launchOptions);
			}
		}

		private static void InstallBrowsers()
		{
			new Program().Run(["install", "--with-deps", PlaywrightSettingsProvider.BrowserName]);
		}
	}
}
