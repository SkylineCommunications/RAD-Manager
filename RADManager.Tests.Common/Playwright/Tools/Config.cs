namespace Skyline.DataMiner.RADManager.Tests.Common.Playwright
{
	using System;
	using System.IO;
	using DotNetEnv;

	public class Config
	{
		private Config()
		{
		}

		public string UserName => Environment.GetEnvironmentVariable("US");

		public string Password => Environment.GetEnvironmentVariable("PP");

		public string EMail => Environment.GetEnvironmentVariable("EM");

		public string B2cUserName => Environment.GetEnvironmentVariable("B2C_US");

		public string B2cPassword => Environment.GetEnvironmentVariable("B2C_PP");

		public string BaseUrl => Environment.GetEnvironmentVariable("HN");

		public static Config Load()
		{
			LoadEnv();

			return new Config();
		}

		private static void LoadEnv()
		{
			var solutionRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
			Console.WriteLine($"Solution root: {solutionRoot}");
			var localEnvPath = Path.Combine(solutionRoot, ".env");
			var productionEnvPath = Path.Combine("c:\\Skyline DataMiner\\Playwright_Tests", ".env");

			if (File.Exists(localEnvPath))
			{
				Env.Load(localEnvPath);
			}
			else if (File.Exists(productionEnvPath))
			{
				Env.Load(productionEnvPath);
			}
			else
			{
				throw new FileNotFoundException("No .env file found.");
			}
		}
	}
}
