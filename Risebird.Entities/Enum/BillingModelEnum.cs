using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum BillingModelEnum
    {
        [Display(Name = "M1 - Prepaid")]
        [Description("M1 - Prepaid")]
        M1 = 1,

        [Display(Name = "M2 - Postpaid + Pre-Delivery PO")]
        [Description("M2 - Postpaid + Pre-Delivery PO")]
        M2 = 2,

        [Display(Name = "M3 - Postpaid + Post-Delivery PO")]
        [Description("M3 - Postpaid + Post-Delivery PO")]
        M3 = 3,

        [Display(Name = "M4 - Postpaid Direct")]
        [Description("M4 - Postpaid Direct")]
        M4 = 4,
    }
}
