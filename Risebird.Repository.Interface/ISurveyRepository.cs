using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISurveyRepository
    {
        Task<IEnumerable<ClientAudioFeedbackDetails>> FetchClientAudioFeedbackDetails();
        Task<IEnumerable<ClientAudioFeedbackDetails>> FetchBroadcastMessages();

    }
}
