namespace RADPlaywright
{
	using System;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using Microsoft.Playwright;
	using Microsoft.Playwright.TestAdapter;
	using RADPlaywright.Tools;
	using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

	public abstract class PlaywrightTestBase
	{
		private static readonly SemaphoreSlim _semaphore = new(1, 1);
		private static IPlaywright? _playwright;
		private static IBrowser? _browser;

		public PlaywrightTestBase(TestContext testContext)
		{
			TestContext = testContext ?? throw new ArgumentNullException(nameof(testContext));

			Config = Config.Load();
		}

		protected TestContext TestContext { get; }

		protected Config Config { get; }

		protected IBrowserContext? Context { get; private set; }

		/// <summary>
		/// Should be called from [AssemblyCleanup] to cleanup Playwright properly.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
				var tracePath = SecurePath.ConstructSecurePath(
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

		protected async Task<IPage> CreatePage()
		{
			if (Context != null)
			{
				return await Context.NewPageAsync();
			}
			else
			{
				throw new InvalidOperationException("Browser context is not initialized.");
			}
		}

		private static async Task<IBrowser> LaunchBrowserAsync()
		{
			var launchOptions = new BrowserTypeLaunchOptions
			{
				Headless = !Debugger.IsAttached,
			};

			try
			{
				if (_playwright != null)
				{
					return await _playwright.Chromium.LaunchAsync(launchOptions);
				}
				else
				{
					throw new InvalidOperationException("Playwright is not initialized.");
				}
			}
			catch (PlaywrightException ex)
				when (ex.Message.Contains("Please run the following command to download new browsers"))
			{
				InstallBrowsers();

				// Retry
				if (_playwright != null)
				{
					return await _playwright.Chromium.LaunchAsync(launchOptions);
				}
				else
				{
					throw new InvalidOperationException("Playwright is not initialized.");
				}
			}
		}

		private static void InstallBrowsers()
		{
			new Program().Run(["install", "--with-deps", PlaywrightSettingsProvider.BrowserName]);
		}
	}
}
