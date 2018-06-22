using Indigo.TWR.TradeImporter.Library;
using Indigo.TWR.TradeImporter.Library.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Indigo.TWR.TradeImporter.Test
{
    public class AzureImporterTest
    {
        private const string ISBN13 = "9780023381737";

        private const string Status_Successful = "Successful";
        private const string Status_Failure = "Error";

        [Fact]
        public async Task Test_ParseSuccessfulMessage()
        {
            AzureImporter importer = new AzureImporter();
            List<ExportItem> itemsList = await importer.ParseMessageAsync(File.ReadAllBytes("TestData\\TestMessage.json"));
            Assert.NotEmpty(itemsList);
            Assert.Equal(ISBN13, itemsList[0].ISBN13);
            Assert.Equal(Status_Successful, itemsList[0].Status);
            Assert.Null(itemsList[0].ErrorMessage);
        }

        [Fact]
        public async Task Test_ParseFailureMessage()
        {
            AzureImporter importer = new AzureImporter();
            List<ExportItem> itemsList = await importer.ParseMessageAsync(File.ReadAllBytes("TestData\\TestMessage_Error.json"));
            Assert.NotEmpty(itemsList);
            Assert.Equal(ISBN13, itemsList[0].ISBN13);
            Assert.Equal(Status_Failure, itemsList[0].Status);
            Assert.NotNull(itemsList[0].ErrorMessage);
        }

        [Fact]
        public async Task Test_ParseInvalidMessage()
        {
            AzureImporter importer = new AzureImporter();
            Exception x = await Assert.ThrowsAnyAsync<Exception>(async () =>
                await importer.ParseMessageAsync(File.ReadAllBytes("TestData\\TestMessage_Invalid.json")));
            Assert.Equal("JsonReaderException", x.GetType().Name);
        }

        [Fact]
        public void Test_GetNewStatus_Successful()
        {
            ExportItem testSuccessfulItem = new ExportItem { Status = Status_Successful };
            byte newStatus = AzureImporter.GetNewStatus(testSuccessfulItem, (byte)ExportStatus.SentForUpdate);
            Assert.Equal((byte)ExportStatus.ExportSuccessful, newStatus);
            newStatus = AzureImporter.GetNewStatus(testSuccessfulItem, (byte)ExportStatus.NewItem);
            Assert.Equal((byte)ExportStatus.NewItem, newStatus);
        }

        [Fact]
        public void Test_GetNewStatus_Failure()
        {
            ExportItem testFailureItem = new ExportItem { Status = Status_Failure };
            byte newStatus = AzureImporter.GetNewStatus(testFailureItem, (byte)ExportStatus.SentForUpdate);
            Assert.Equal((byte)ExportStatus.FailedToUpdate, newStatus);
            newStatus = AzureImporter.GetNewStatus(testFailureItem, (byte)ExportStatus.SentForCreation);
            Assert.Equal((byte)ExportStatus.FailedToCreate, newStatus);
            newStatus = AzureImporter.GetNewStatus(testFailureItem, (byte)ExportStatus.NewItem);
            Assert.Equal((byte)ExportStatus.NewItem, newStatus);
        }

        [Fact]
        public void Test_GetNewStatus_Invalid()
        {
            ExportItem testInvalidItem = new ExportItem { Status = "Invalid" };
            byte newStatus = AzureImporter.GetNewStatus(testInvalidItem, (byte)ExportStatus.SentForCreation);
            Assert.Equal((byte)ExportStatus.SentForCreation, newStatus);
        }
    }
}
