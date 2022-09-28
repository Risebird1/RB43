using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IClientService
    {
        List<Client> GetClients(string emailid, string profileName);
        bool AddClient(Client client);
        bool UpdateClient(Client client);
        Client GetClient(int id);
        bool DeleteClient(int id);
        List<ClientRole> GetClientRoles();
        string AddClientJd(ClientJdVM _client);
        IEnumerable<ClientJdVM> FetchClientJd(int clientid);
        string AddCandidateJDScore(int IrfId, List<CandidateCuratedScore> candidateCuratedScore, string candidateProfile);
        IList<CandidateCuratedScore> GetCandidateJDScore(int id);
        IList<Wallet> FetchClientWallet(int clientid);
        string AddWalletPoints(int ClientId, string emailid, int points, string comment);
        string ResetCandidate(int Clientjdid, string candidateEmail, string emailid);
        string DeleteClientJd(int assessmentid);
        Task<int> GetClientRoleByEmailId(string emailId);
        Task<int> GetUniqueClientRoleByEmailId(string emailId, string id);
        Task<string> PublicClientUrlSave(string emailId, string uniqueid);
        Task<string> PublicInterviewerUrlSave(string emailId, string uniqueid);
        Task<string> Fetchlogoforclient(string emailId, string id);
        IEnumerable<IRF> GetRecruiterReportByClient(string emailId, DateTime st, DateTime se);
        //bool UpdateRecruiterStatus(int userid, int clientid, bool status);
        bool UpdateRecruiterStatus(int userid, int clientid, int Irfid, bool status);
        Task<int> GetClientByEmailId(string emailId);
        Task<int> GetClientBillingType(int Clientid);
        Task<IEnumerable<Client>> GetAllClients();
        //Task<ClientCommentsForReport> GetClientCommentsForReport(int interviewId);
        Task<ClientCommentsForReport> GetClientCommentsForReport(int interviewId, string emailId, string profile);
        Task<bool> AddUpdateClientCommentsForReport(ClientCommentsForReport model);
        //Task<ClientFeedbackCommentVM> GetClientCommentsList(int interviewId, string emailId);
        Task<IEnumerable<ClientAudioInstructions>> GetClientAudioForIRF(int interviewId, string emailId, string profile);
        Task<IEnumerable<ClientAudioInstructions>> GetClientInstructionsAudioForReport(int interviewId, string emailId, string profile);
    }
}
