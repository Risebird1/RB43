using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IClientService
    {
        Task<List<Client>> GetClients(string emailid, string profileName);
        Task<bool> AddClient(Client client);
        Task<bool> UpdateClient(Client client);
        Task<Client> GetClient(int id);
        Task<bool> DeleteClient(int id);
        Task<List<ClientRole>> GetClientRoles();
        Task<string> AddClientJd(ClientJdVM _client);
        Task<IEnumerable<ClientJdVM>> FetchClientJd(int clientid);
        string AddCandidateJDScore(int IrfId, List<CandidateCuratedScore> candidateCuratedScore, string candidateProfile);
        IList<CandidateCuratedScore> GetCandidateJDScore(int id);
        IList<Wallet> FetchClientWallet(int clientid);
        Task<string> AddWalletPoints(int ClientId, string emailid, int points, string comment);
        string ResetCandidate(int Clientjdid, string candidateEmail, string emailid);
        string DeleteClientJd(int assessmentid);
        Task<int> GetClientRoleByEmailId(string emailId);
        Task<int> GetUniqueClientRoleByEmailId(string emailId, string id);
        Task<string> PublicClientUrlSave(string emailId, string uniqueid);
        Task<string> PublicInterviewerUrlSave(string emailId, string uniqueid);
        Task<string> Fetchlogoforclient(string emailId, string id);
        Task<IEnumerable<IRF>> GetRecruiterReportByClient(string emailId, DateTime st, DateTime se);
        //bool UpdateRecruiterStatus(int userid, int clientid, bool status);
        Task<bool> UpdateRecruiterStatus(int userid, int clientid, int Irfid, bool status);
        Task<int> GetClientByEmailId(string emailId);
        Task<int> GetClientBillingType(int Clientid);
        Task<IEnumerable<Client>> GetAllClients();
        //Task<ClientCommentsForReport> GetClientCommentsForReport(int interviewId);
        Task<ClientCommentsForReport> GetClientCommentsForReport(int interviewId, string emailId, string profile);
        Task<bool> AddUpdateClientCommentsForReport(ClientCommentsForReport model);
        //Task<ClientFeedbackCommentVM> GetClientCommentsList(int interviewId, string emailId);
        Task<IEnumerable<ClientAudioInstructions>> GetClientAudioForIRF(int interviewId, string emailId, string profile);
        Task<IEnumerable<ClientAudioInstructions>> GetClientInstructionsAudioForReport(int interviewId, string emailId, string profile);
        Task<List<Clientaudiofeedback>> Getclientaudiofeedbackdetails();
        Task<string> GetClientName(int id);
        Task<string> GetClientLogoByIRFId(int irfId);
        Task<IEnumerable<StatsVM>> GetStaffingAgencyStats(int? irfId = null);
        Task<IEnumerable<StatsVM>> GetClientHRStats(int? irfId = null);
        Task<IEnumerable<StatsDetailsVM>> GetClientRoleWiseStatsDetails(int interviewerUserId, string type, string clientRole, int? irfId = null, DateTime ? startDate = null, DateTime? endDate = null);
        Task<Tuple<int, string>> GetClientRoleAndCompanyNameByEmailId(string emailId);
        Task<IEnumerable<Meeting>> GetDateRangeWiseClientMeetings(DateTime startDate, DateTime endDate, string emailId, int clientId);
        Task<ClientRoleWiseStatsWrapper> GetDateRangeWiseMeetingStatusForClientRoles(DateTime startDate, DateTime endDate, string emailId, int clientId);
        Task<IEnumerable<StatsDerived>> GetDateRangeWiseMeetingStatusForClientByIRF(DateTime startDate, DateTime endDate, string emailId, int clientId, string clientUserEmailId, string role = null);
    }
}
