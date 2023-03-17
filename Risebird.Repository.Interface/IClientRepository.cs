using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Risebird.Repository.Interface
{
    public interface IClientRepository
    {
        Task<IEnumerable<Client>> GetClients(string emailid, string profileName);
        Task<bool> AddClient(Client client);
        Task<bool> UpdateClient(Client entity);
        Task<Client> GetClient(int id);
        Task<bool> DeleteClient(int id);
        Task<IEnumerable<ClientRole>> GetClientRoles();
        Task<string> AddClientJd(ClientJdVM _client);
        Task<IEnumerable<ClientJdVM>> FetchClientJd(int clientid);
        string AddCandidateJDScore(int IrfId, List<CandidateCuratedScore> candidateCuratedScore, string candidateProfile);
        IEnumerable<CandidateCuratedScore> GetCandidateJDScore(int id);
        IEnumerable<Wallet> FetchClientWallet(int clientid);
        Task<string> AddWalletPoints(int ClientId, string emailid, int points, string comment);
        string ResetCandidate(int Clientjdid, string candidateEmail, string emailid);
        string DeleteClientJd(int assessmentid);
        Task<int> GetClientRoleByEmailId(string emailId);
        Task<int> GetUniqueClientRoleByEmailId(string emailId, string id);
        Task<string> PublicClientUrlSave(string emailId, string uniqueid);
        Task<string> PublicInterviewerUrlSave(string emailId, string uniqueid);
        Task<string> Fetchlogoforclient(string emailId, string id);
        Task<IEnumerable<IRF>> GetRecruiterReportByClient(string emailID, DateTime st, DateTime se);
        Task<int> GetClientByEmailId(string emailId);
        Task<bool> UpdateRecruiterStatus(int userid, int clientid, int Irfid, bool status);
        Task<int> GetClientBillingType(int Clientid);
        Task<IEnumerable<Client>> GetAllClients();
        Task<IEnumerable<ClientCommentsForReport>> GetClientCommentsForReport(int interviewId);
        Task<bool> AddUpdateClientCommentsForReport(ClientCommentsForReport model);
        Task<IEnumerable<ClientAudioInstructions>> GetClientAudioForIRF(int interviewId);
        Task<IEnumerable<ClientAudioInstructions>> GetClientInstructionsAudioForReport(int interviewId, string emailId = null, string profile = null);
        Task<List<Clientaudiofeedback>> Getclientaudiofeedbackdetails();
        Task<string> GetClientName(int id);
        Task<string> GetClientLogoByIRFId(int irfId);
        Task<IEnumerable<StatsVM>> GetStaffingAgencyStats(int? irfId = null);
        Task<IEnumerable<StatsVM>> GetClientHRStats(int? irfId = null);
        Task<IEnumerable<StatsDetailsVM>> GetClientRoleWiseStatsDetails(int clientUserId, string type, string clientRole, int? irfId = null);
    }
}
