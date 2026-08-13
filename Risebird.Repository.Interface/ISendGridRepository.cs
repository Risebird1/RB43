using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISendGridRepository
    {
        Task<bool> SaveSendGridEventDetails(SendgridEventData model);
        Task<bool> SaveSentMailDetails(SentMailDetails model, bool isStaging = false);

        /// <summary>
        /// Returns the subset of <paramref name="emails"/> that belong to a UserList account
        /// which is inactive (UserList.IsActive = 0). Used to hard-suppress outgoing mail so an
        /// inactive interviewer/user is never emailed, regardless of the calling code path.
        /// </summary>
        Task<HashSet<string>> GetInactiveRecipientEmailsAsync(IEnumerable<string> emails);
    }
}
