using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ISurveyRepository
    {
        Task<List<ClientAudioFeedbackDetails>> FetchClientAudioFeedbackDetails();
        Task<List<ClientAudioFeedbackDetails>> FetchBroadcastMessages();
        Task<SurveyCandidateInterviewer> AddOrEditCandidateRating(SurveyVM model);
        Task<IEnumerable<SurveyVM>> GetCandidateRating(string emailid, string profileName);
        Task<IEnumerable<SurveyVM>> GetCandidateRatingByIRFId(string IRFId);
    }
}
