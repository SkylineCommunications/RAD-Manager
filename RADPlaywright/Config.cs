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

			Credentials = new Authentication.Credentials(configuration["PLAYWRIGHT_USERNAME"], configuration["PLAYWRIGHT_PASSWORD"], configuration["EMAIL"]);

			BaseUrl = configuration["PLAYWRIGHT_URL"] ?? "https://analyticscl56-skyline.on.dataminer.services/";
		}

		public Authentication.Credentials Credentials { get; }

		public string BaseUrl { get; }

		public static Config Load()
		{
			var builder = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.AddUserSecrets(Assembly.GetExecutingAssembly());

			return new Config(builder.Build());
		}
	}
}
