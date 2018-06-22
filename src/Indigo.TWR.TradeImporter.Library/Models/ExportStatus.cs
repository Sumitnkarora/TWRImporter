using System;
using System.Collections.Generic;
using System.Text;

namespace Indigo.TWR.TradeImporter.Library.Models
{
    /// <summary>
    /// Status of an exported item.
    /// </summary>
    public enum ExportStatus : byte
    {
        /// <summary>
        /// The item is a newly added item.
        /// </summary>
        NewItem=0,
        /// <summary>
        /// The item is ready to be sent to TW to be created.
        /// </summary>
        ReadyForCreation=1,
        /// <summary>
        /// The item has been sent to TW to be created.
        /// </summary>
        SentForCreation=2,
        /// <summary>
        /// The item has a status of "Error" and cannot be created.
        /// </summary>
        FailedToCreate=3,
        /// <summary>
        /// The item was exported to TW successfully.
        /// </summary>
        ExportSuccessful=4,
        /// <summary>
        /// The item is ready to be sent to TW to be updated.
        /// </summary>
        ReadyForUpdate=5,
        /// <summary>
        /// The item has been sent to TW to be updated.
        /// </summary>
        SentForUpdate=6,
        /// <summary>
        /// The item has a status of "Error" and cannot be updated.
        /// </summary>
        FailedToUpdate=7
    }
}
