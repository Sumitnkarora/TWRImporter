using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using System.Data;
using Indigo.TWR.TradeImporter.Library.Models;

namespace Indigo.TWR.TradeImporter.Library
{
    /// <summary>
    /// Class to import status messages from an Azure queue containing Teamwork response messages.
    /// The status of each item is set in the database.
    /// </summary>
    public class AzureImporter
    {
        private const string DefaultRegionId = "US";

        private SqlConnection sqlConnection;

        /// <summary>
        /// Configuration from appsettings.json.
        /// </summary>
        public IConfiguration Config { get; set; }
        /// <summary>
        /// Serilog Logger.
        /// </summary>
        public ILogger Log { get; set; }

        private string ConnectionString
        {
            get
            {
                return Config.GetConnectionString("TradeDbUs");
            }
        }

        /// <summary>
        /// Creates a new AzureImporter. Reads configuration from appsettings.json.
        /// </summary>
        public AzureImporter()
        {
            sqlConnection = null;
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json");
            Config = builder.Build();
            Log = new LoggerConfiguration()
                .ReadFrom.Configuration(Config)
                .CreateLogger();
        }

        /// <summary>
        /// Imports messages from an Azure queue into TradeDataExport table in TradeDB_US.
        /// </summary>
        public void Run()
        {
            Log.Information("Running import process...");

            try
            {
                OpenDbConnection();

                // Connect to Azure queue
                MessageReceiver messageReceiver = new MessageReceiver(Config["TWR.ServiceBus"], Config["QueueName"])
                {
                    OperationTimeout = new TimeSpan(0, 0, int.Parse(Config["ReceiveTimeout"])) //seconds to wait until we stop receiving messages
                };
                ReceiveQueueMessagesAsync(messageReceiver).GetAwaiter().GetResult();
            }
            catch (Exception x)
            {
                Log.Error(x, "Generic Error in Import");
            }
            finally
            {
                CloseDbConnection();
            }
        }

        private void OpenDbConnection()
        {
            // Connect to database
            sqlConnection = new SqlConnection(ConnectionString);
            sqlConnection.Open();
        }

        private void CloseDbConnection()
        {
            if (sqlConnection != null)
            {
                if (sqlConnection.State == ConnectionState.Open)
                {
                    sqlConnection.Close();
                }
            }
        }

        private async Task ReceiveQueueMessagesAsync(MessageReceiver messageReceiver)
        {
            int messageCount = 0;
            while (true)
            {
                var msg = await messageReceiver.ReceiveAsync();
                if (msg == null)
                {
                    Log.Information("No more messages received.");
                    break;
                }
                messageCount++;
                try
                {
                    await ReceiveMessageAsync(msg.Body);
                }
                catch (Exception x)
                {
                    Log.Error(x, "Error Processing Message");
                }
                await messageReceiver.CompleteAsync(msg.SystemProperties.LockToken);
            }
            await messageReceiver.CloseAsync();
            Log.Information($"Received {messageCount} message(s).");
        }

        /// <summary>
        /// Parses the body of an Azure queue message. Call this when the message is received.
        /// The message body is a JSON response from Teamwork API.
        /// </summary>
        /// <param name="messageBody">Body of the message (UTF-8 encoding recommended)</param>
        /// <returns>List of <c>ExportItem</c> from the message JSON</returns>
        public async Task<List<ExportItem>> ParseMessageAsync(byte[] messageBody)
        {
            List<ExportItem> itemsList = new List<ExportItem>();
            JObject dataObject = null;
            MemoryStream messageStream = new MemoryStream(messageBody);
            using (TextReader textReader = new StreamReader(messageStream))
            {
                dataObject = JObject.Parse(await textReader.ReadToEndAsync());
            }
            foreach (JObject line in dataObject["Lines"])
            {
                ExportItem item = new ExportItem();
                item.ISBN13 = line["StyleNo"].ToString();
                item.Status = line["Status"].ToString();
                if (line["Error"].Type != JTokenType.Null)
                {
                    item.ErrorMessage = line["Error"].ToString();
                }
                else
                {
                    item.ErrorMessage = null;
                }
                itemsList.Add(item);
            }
            return itemsList;
        }

        private async Task ReceiveMessageAsync(byte[] messageBody)
        {
            List<ExportItem> itemsList = await ParseMessageAsync(messageBody);
            foreach (ExportItem item in itemsList)
            {
                byte itemStatus = 0;
                // Call a SP to get item status
                itemStatus = (byte)await sqlConnection.ExecuteScalarAsync(sql: "usp_TradeDataExport_GetStatus",
                    param: new { item.ISBN13, RegionId = DefaultRegionId },
                    commandType: CommandType.StoredProcedure);
                byte newStatus = GetNewStatus(item, itemStatus);
                if (newStatus != itemStatus)
                {
                    // Call a SP to set item status
                    int rowsAffected = await sqlConnection.ExecuteAsync(sql: "usp_TradeDataExport_UpdateStatus",
                        param: new { item.ISBN13, RegionId = DefaultRegionId,
                            ExportStatus = newStatus, item.ErrorMessage },
                        commandType: CommandType.StoredProcedure);
                    if (rowsAffected > 0)
                    {
                        Log.Debug($"Updated status to {(ExportStatus)newStatus} for item {item.ISBN13}.");
                        if (!string.IsNullOrEmpty(item.ErrorMessage))
                        {
                            Log.Debug($"Teamwork Item Error Message = {item.ErrorMessage}; Item ID = {item.ISBN13}.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the new status of an exported item, i.e. whether import to Teamwork was successful or failed.
        /// </summary>
        /// <param name="item"><c>ExportItem</c>: The item to be updated</param>
        /// <param name="itemStatus"><c>ExportStatus</c>: The current status of the item</param>
        /// <returns><c>ExportStatus</c>: The new status of the item</returns>
        public static byte GetNewStatus(ExportItem item, byte itemStatus)
        {
            byte newStatus = itemStatus;
            switch (item.Status)
            {
                case "Successful":
                    {
                        if (itemStatus == (byte)ExportStatus.SentForCreation ||
                            itemStatus == (byte)ExportStatus.SentForUpdate)
                        {
                            newStatus = (byte)ExportStatus.ExportSuccessful;
                        }
                        break;
                    }
                case "Error":
                    {
                        if (itemStatus == (byte)ExportStatus.SentForCreation)
                        {
                            newStatus = (byte)ExportStatus.FailedToCreate;
                        }
                        else if (itemStatus == (byte)ExportStatus.SentForUpdate)
                        {
                            newStatus = (byte)ExportStatus.FailedToUpdate;
                        }
                        break;
                    }
            }

            return newStatus;
        }
    }
}
