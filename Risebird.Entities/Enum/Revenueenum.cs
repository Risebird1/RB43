using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum Revenueenum
    {
        [Description("Invoice Raised")]
        InvoiceRaised = 1,
        [Description("PO Pending")]
        POPending = 2
    }
    public enum Invoicerealized
    {
        [Description("Received")]
        Received = 1,
        [Description("Not Received")]
        NotReceived = 2
    }
}
