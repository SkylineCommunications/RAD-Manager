namespace RadDataSources
{
	using System;
	using System.IO;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Utils.RadToolkit;

	public static class ConnectionHelper
	{
		private const string APPLICATION_NAME = "GQI RAD data sources";
		private static readonly object _connectionLock = new object();
		private static Connection _connection = null;
		private static RadHelper _radHelper;

		/// <summary>
		/// Gets the connection to the DataMiner Agent.
		/// </summary>
		public static Connection Connection
		{
			get => _connection;
		}

		public static RadHelper RadHelper
		{
			get => _radHelper;
		}

		public static void InitializeConnection(GQIDMS dms, IGQILogger logger)
		{
			lock (_connectionLock)
			{
				if (_connection?.IsShuttingDown == false)
					return;

				if (dms == null)
					throw new ArgumentNullException(nameof(dms));

				var attributes = ConnectionAttributes.AllowMessageThrottling;
				try
				{
					var connection = ConnectionSettings.GetConnection("localhost", attributes);
					connection.ClientApplicationName = APPLICATION_NAME;
					connection.AuthenticateUsingTicket(RequestCloneTicket(dms));

					var radHelper = new RadHelper(connection, new Logger(s => logger.Error(s)));

					_connection = connection;
					_radHelper = radHelper;
				}
				catch (Exception ex)
				{
					logger.Error(ex, "Failed to setup a connection with the DataMiner Agent: " + ex.Message);
					throw new InvalidOperationException("Failed to setup a connection with the DataMiner Agent: " + ex.Message, ex);
				}
			}
		}

		/// <summary>
		/// Requests a one time ticket that can be used to authenticate another connection.
		/// </summary>
		/// <returns>Ticket.</returns>
		private static string RequestCloneTicket(GQIDMS dms)
		{
			RequestTicketMessage requestInfo = new RequestTicketMessage(TicketType.Authentication, ExportConfig());
			TicketResponseMessage ticketInfo = dms.SendMessage(requestInfo) as TicketResponseMessage;
			if (ticketInfo == null)
				throw new DataMinerException("Did not receive ticket.");

			return ticketInfo.Ticket;
		}

		/// <summary>
		/// Exports the clientside configuration for polling, zipping etc. Does not include
		/// connection uris and the like.
		/// </summary>
		/// <returns>Flags.</returns>
		private static byte[] ExportConfig()
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (BinaryWriter bw = new BinaryWriter(ms))
				{
					bw.Write(1); // version
					bw.Write(1000); // ms PollingInterval
					bw.Write(100); // ms PollingIntervalFast
					bw.Write(1000); // StackOverflowSize
					bw.Write(5000); // ms ConnectionCheckingInterval
					bw.Write(10); // MaxSimultaneousCalls

					ConnectionAttributes attributesToAdd = ConnectionAttributes.AllowMessageThrottling;
					bw.Write((int)attributesToAdd);

					bw.Write("r"); // connection is remoting or IPC (which inherits from remoting)
					bw.Write(1); // version
					bw.Write(30); // s PollingFallbackTime
				}

				return ms.ToArray();
			}
		}
	}
}