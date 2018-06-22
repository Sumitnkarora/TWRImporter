using Microsoft.Azure.ServiceBus;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Indigo.TWR.TradeImporter.AzureSender
{
    class Program
    {
        const string PathToTestFiles = @"C:\Users\DTohatan\Documents\TWRIntegration\TestData";
        const int NumberOfMessages = 1;
        const string WhichMessageToSend = "TestMessage";
        const string ServiceBusConnectionString = "Endpoint=sb://twridgservicebusdev.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=1en5RVflQoujEIOt34wq06H5bkCHRKlpaTw82+N7yoA=";
        const string QueueName = "books-import-queue";
        static IQueueClient queueClient;
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Azure Sender.");
            Console.WriteLine("Sending test messages to queue...");
            QueueAccessAsync().GetAwaiter().GetResult();
        }

        private static async Task QueueAccessAsync()
        {
            queueClient = new QueueClient(ServiceBusConnectionString, QueueName);
            await SendMessagesAsync();
            Console.WriteLine("Press a key to exit.");
            Console.ReadKey();
            await queueClient.CloseAsync();
        }

        private static async Task SendMessagesAsync()
        {
            try
            {
                for (var i = 0; i < NumberOfMessages; i++)
                {
                    // Create a new message to send to queue.
                    string messageBody = File.ReadAllText($"{PathToTestFiles}\\{WhichMessageToSend}.json");
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));
                    Console.WriteLine($"Sending message {i}...");
                    await queueClient.SendAsync(message);
                }
            }
            catch (Exception x)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {x.Message}");
            }
        }
    }
}
