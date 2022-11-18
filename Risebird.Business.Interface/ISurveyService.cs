using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ISurveyService
    {
        Task<List<ClientAudioFeedbackDetails>> FetchClientAudioFeedbackDetails();
        Task<List<ClientAudioFeedbackDetails>> FetchBroadcastMessages();


    }
}
