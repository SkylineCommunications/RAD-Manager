namespace RAD_Manager
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.Http;
	using System.Net.Http.Headers;
	using System.Text;
	using System.Threading.Tasks;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.GRPCConnection;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	public class GqiUtils
	{
		public static bool IsGqiDxmEnabled()
		{
			var httpClient = new HttpClient();
			var connectionTicket = GetTicket(Engine.SLNetRaw);
			var baseUrl = CreateBaseUrl(Engine.SLNetRaw);

			var connectionId = ConnectToWebApiAsync(httpClient, connectionTicket, baseUrl).Result;
			var gqiFeatureInfo = GetGenericInterfaceFeatureInfoAsync(httpClient, connectionId, baseUrl).Result;

			return gqiFeatureInfo?.IsUsingDxM ?? false;
		}

		/// <summary>
		/// Retrieves a connection ticket for the DataMiner system.
		/// </summary>
		/// <param name="conn">The connection to the DataMiner SLNet system.</param>
		/// <returns>A string representing the connection ticket.</returns>
		/// <exception cref="InvalidOperationException">Thrown when unable to retrieve a ticket after multiple attempts.</exception>
		private static string GetTicket(IConnection conn)
		{
			string ticket;
			Connection fullConn = (Connection)conn;

			// Only supported from DataMiner 10.4.9, see RN https://intranet.skyline.be/DataMiner/Lists/Release%20Notes/DispForm2.aspx?ID=40038
			try
			{
				var request = new RequestTicketMessage(TicketType.Authentication, Array.Empty<byte>());
				var response = conn.HandleSingleResponseMessage(request) as TicketResponseMessage;
				ticket = response.Ticket;
			}
			catch (Exception ex1)
			{
				try
				{
					// Tries to request a ticket as Administrator
					ticket = fullConn.RequestLogonAsTicket("Administrator", String.Empty);
				}
				catch (Exception ex2)
				{
					throw new InvalidOperationException($"Retrieving web ticket failed with two exceptions: Exception 1: {ex1} \r\n\r\n Exception 2: {ex2}");
				}
			}

			return ticket;
		}

		/// <summary>
		/// Creates the base URL for the DataMiner system using the SLNet connection.
		/// </summary>
		/// <param name="connection">The connection to the DataMiner SLNet system.</param>
		/// <returns>The base URL of the DataMiner system.</returns>
		/// <exception cref="DataMinerException">Thrown when general information about the DataMiner system cannot be retrieved.</exception>
		private static string CreateBaseUrl(IConnection connection)
		{
			string protocol = "http";
			string host;

			if (connection is GRPCConnection grpcc)
			{
				host = grpcc.ConnectionString;
			}
			else
			{
				host = "localhost"; // default to localhost

				try
				{
					var request = new GetInfoMessage(InfoType.LocalGeneralInfoMessage);
					if (connection.HandleSingleResponseMessage(request) is GeneralInfoEventMessage response)
					{
						if (response.HTTPS)
							protocol = "https";
						if (!String.IsNullOrEmpty(response.CertificateAddressName))
							host = response.CertificateAddressName;
					}
				}
				catch (Exception e)
				{
					throw new DataMinerException("Failed to retrieve general info: " + e.Message, e);
				}
			}

			if (!host.StartsWith("http") || !host.StartsWith("https"))
			{
				return $"{protocol}://{host}/";
			}
			else
			{
				return host;
			}
		}

		/// <summary>
		/// Establishes a connection with the web API using a connection ticket.
		/// </summary>
		private static async Task<string> ConnectToWebApiAsync(HttpClient httpClient, string connectionTicket, string baseUrl)
		{
			var message = new ConnectAppAndInfoUsingTicketMessage(connectionTicket);

			string jsonContent = JsonConvert.SerializeObject(message);
			StringContent stringContent = new StringContent(jsonContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json").MediaType);
			var httpResponse = await httpClient.PostAsync($"{baseUrl}API/v1/json.asmx/ConnectAppAndInfoUsingTicket", stringContent);
			httpResponse.EnsureSuccessStatusCode();

			string jsonString = await httpResponse.Content.ReadAsStringAsync();
			var jsonObject = SecureNewtonsoftDeserialization.DeserializeObject<ConnectAppAndInfoUsingTicketResponse>(jsonString);

			return jsonObject.D.Connection;
		}

		private static async Task<GenericInterfaceFeatureInfo> GetGenericInterfaceFeatureInfoAsync(HttpClient httpClient, string connectionId, string baseUrl)
		{
			var message = new GetGenericInterfaceFeatureInfoMessage
			{
				Connection = connectionId,
				FeatureNames = new List<string> { "GenericInterface" },
			};

			string jsonContent = JsonConvert.SerializeObject(message);
			StringContent stringContent = new StringContent(jsonContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json").MediaType);
			var httpResponse = await httpClient.PostAsync($"{baseUrl}API/v1/Internal.asmx/GetFeatureInfo", stringContent);
			httpResponse.EnsureSuccessStatusCode();

			string jsonString = await httpResponse.Content.ReadAsStringAsync();
			var jsonObject = SecureNewtonsoftDeserialization.DeserializeObject<GetGenericInterfaceFeatureInfoResponse>(jsonString);

			return jsonObject.D.FirstOrDefault();
		}
	}

	public class GetGenericInterfaceFeatureInfoMessage
	{
		[JsonProperty("connection")]
		public string Connection { get; set; }

		[JsonProperty("featureNames")]
		public List<string> FeatureNames { get; set; }
	}

	public class GetGenericInterfaceFeatureInfoResponse
	{
		[JsonProperty("d")]
		public List<GenericInterfaceFeatureInfo> D { get; set; }
	}

	public class FeatureInfo
	{
		[JsonProperty("ErrorMessage")]
		public string ErrorMessage { get; set; }

		[JsonProperty("Name")]
		public string Name { get; set; }

		[JsonProperty("IsEnabled")]
		public bool IsEnabled { get; set; }
	}

	public class GenericInterfaceFeatureInfo : FeatureInfo
	{
		[JsonProperty("IsUsingDxM")]
		public bool IsUsingDxM { get; set; }
	}

	/// <summary>
	/// Represents a message used to connect an application and its information using a connection ticket.
	/// </summary>
	public class ConnectAppAndInfoUsingTicketMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectAppAndInfoUsingTicketMessage"/> class with the specified connection ticket.
		/// </summary>
		/// <param name="ticket">The connection ticket for establishing the connection.</param>
		public ConnectAppAndInfoUsingTicketMessage(string ticket)
		{
			ConnectionTicket = ticket;
			ClientAppName = String.Empty;
			ClientAppVersion = String.Empty;
			ClientComputerName = String.Empty;
		}

		/// <summary>
		/// Gets or sets the connection ticket used for authentication.
		/// </summary>
		[JsonProperty("connectionTicket")]
		public string ConnectionTicket { get; set; }

		/// <summary>
		/// Gets or sets the name of the client application.
		/// </summary>
		[JsonProperty("clientAppName")]
		public string ClientAppName { get; set; }

		/// <summary>
		/// Gets or sets the version of the client application.
		/// </summary>
		[JsonProperty("clientAppVersion")]
		public string ClientAppVersion { get; set; }

		/// <summary>
		/// Gets or sets the name of the client computer.
		/// </summary>
		[JsonProperty("clientComputerName")]
		public string ClientComputerName { get; set; }
	}

	public class ConnectAppAndInfoUsingTicketResponse
	{
		[JsonProperty("d")]
		public DmaConnectAndInfo D { get; set; }
	}

	public class DmaConnectAndInfo
	{
		[JsonProperty("connection")]
		public string Connection { get; set; }
	}
}
