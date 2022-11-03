using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IMailService
    {
        Task<IEnumerable<SendgridEventData>> GetRecipients(string profile, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<MailRecipientActivityDetailsVM>> GetEmailRecipientActivities(string recipientType, DateTime? startDate = null, DateTime? endDate = null);
    }
}
