using System;

namespace Risebird.Entities.ViewModel
{
    public enum AgreementTypeEnum
    {
        MSA = 1,
        SOW = 2,
        NDA = 3,
        DPA = 4
    }

    public enum AgreementStatusEnum
    {
        Active = 1,
        Terminated = 2
    }

    public class ClientAgreementVM
    {
        public int id { get; set; }
        public int ClientId { get; set; }
        public int AgreementType { get; set; }
        public string AgreementTypeDesc { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpireDate { get; set; }

        /// <summary>Manually stored status: 1=Active, 2=Terminated. "Expired" is derived from ExpireDate — see EffectiveStatusDesc.</summary>
        public int Status { get; set; }
        public string AgreementFileUrl { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }

        /// <summary>True when ExpireDate is set and before today.</summary>
        public bool IsExpired =>
            ExpireDate.HasValue && ExpireDate.Value.Date < DateTime.Today;

        /// <summary>True when ExpireDate falls within the next 30 days (inclusive) and not already expired.</summary>
        public bool IsExpiringSoon =>
            ExpireDate.HasValue && !IsExpired && ExpireDate.Value.Date <= DateTime.Today.AddDays(30);

        /// <summary>Days remaining until expiry. Negative once expired. Null when there is no ExpireDate.</summary>
        public int? DaysRemaining =>
            ExpireDate.HasValue ? (int)(ExpireDate.Value.Date - DateTime.Today).TotalDays : null;

        /// <summary>Terminated (manual) takes priority, then auto-derived Expired/Expiring Soon, else Active.</summary>
        public string EffectiveStatusDesc =>
            Status == (int)AgreementStatusEnum.Terminated ? "Terminated"
            : IsExpired ? "Expired"
            : IsExpiringSoon ? "Expiring Soon"
            : "Active";

        /// <summary>
        /// Pairs with the "agr-badge" base class (not Bootstrap's "badge") — the site's global
        /// .badge rule is a fixed 22x22px notification-count circle and clips normal label text.
        /// </summary>
        public string EffectiveStatusCss =>
            Status == (int)AgreementStatusEnum.Terminated ? "agr-badge-secondary"
            : IsExpired ? "agr-badge-danger"
            : IsExpiringSoon ? "agr-badge-warning"
            : "agr-badge-success";
    }
}
