namespace Indigo.TWR.TradeImporter.Library.Models
{
    /// <summary>
    /// Item exported to Teamwork.
    /// </summary>
    public class ExportItem
    {
        /// <summary>
        /// Item identifier (ISBN-13)
        /// </summary>
        public string ISBN13 { get; set; }
        /// <summary>
        /// Status from API response, "Successful" or "Error"
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Error message if status is "Error"
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}