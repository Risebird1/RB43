using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IMailRepository
    {
        Task<IEnumerable<SendgridEventData>> GetRecipients(string profile, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<MailRecipientActivityDetailsVM>> GetEmailRecipientActivities(string recipientEmail, DateTime? startDate = null, DateTime? endDate = null);
    }
}
