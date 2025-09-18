namespace RADPlaywright
{
	using System.Reflection;

	using Microsoft.Extensions.Configuration;
	using RADPlaywright.Tools;

	/*
	// Use the following commands to configure the credentials:
	// dotnet user-secrets set "PLAYWRIGHT_USERNAME" "your_username"
	// dotnet user-secrets set "PLAYWRIGHT_PASSWORD" "your_password"
	*/
	public class Config
	{
		private Config(IConfiguration configuration)
		{
			if (configuration is null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}

			Credentials = new Authentication.Credentials(configuration["PLAYWRIGHT_USERNAME"], configuration["PLAYWRIGHT_PASSWORD"]);

			BaseUrl = configuration["PLAYWRIGHT_URL"] ?? "https://slc-h56-g01.skyline.local/";
		}

		public Authentication.Credentials Credentials { get; }

		public string BaseUrl { get; }

		public static Config Load()
		{
			var builder = new ConfigurationBuilder()
				.AddUserSecrets(Assembly.GetExecutingAssembly())
				.AddEnvironmentVariables();

			return new Config(builder.Build());
		}
	}
}
