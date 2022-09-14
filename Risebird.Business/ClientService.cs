using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public ClientService(IClientRepository clientRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _clientRepository = clientRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public List<Client> GetClients(string emailid, string profileName)
        {
            try
            {
                return _clientRepository.GetClients(emailid, profileName).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool AddClient(Client client)
        {
            try
            {
                return _clientRepository.AddClient(client).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public Client GetClient(int id)
        {
            try
            {
                var client = _clientRepository.GetClient(id);

                if (client == null)
                    return null;
                else
                    return client.Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateClient(Client client)
        {
            try
            {
                return _clientRepository.UpdateClient(client).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool DeleteClient(int id)
        {
            try
            {
                return _clientRepository.DeleteClient(id).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public List<ClientRole> GetClientRoles()
        {
            try
            {
                var clientRoles = _clientRepository.GetClientRoles();
                if (clientRoles != null)
                    return clientRoles.Result.ToList();
                else
                    return null;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string AddClientJd(ClientJdVM _client)
        {
            try
            {
                return _clientRepository.AddClientJd(_client).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public IEnumerable<ClientJdVM> FetchClientJd(int clientid)
        {
            try
            {
                return _clientRepository.FetchClientJd(clientid);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string AddCandidateJDScore(int IrfId, List<CandidateCuratedScore> candidateCuratedScore, string candidateProfile)
        {
            try
            {
                return _clientRepository.AddCandidateJDScore(IrfId, candidateCuratedScore, candidateProfile);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public IList<CandidateCuratedScore> GetCandidateJDScore(int id)
        {
            try
            {
                var result = (IList<CandidateCuratedScore>)_clientRepository.GetCandidateJDScore(id);
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IList<Wallet> FetchClientWallet(int clientid)
        {
            try
            {
                var result = (IList<Wallet>)_clientRepository.FetchClientWallet(clientid);
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string AddWalletPoints(int ClientId, string emailid, int points, string comment)
        {
            try
            {
                var result = _clientRepository.AddWalletPoints(ClientId, emailid, points, comment).Result;
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "failed";
            }
        }
        public string ResetCandidate(int Clientjdid, string candidateEmail, string emailid)
        {
            try
            {
                return _clientRepository.ResetCandidate(Clientjdid, candidateEmail, emailid);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public string DeleteClientJd(int assessmentid)
        {
            try
            {
                return _clientRepository.DeleteClientJd(assessmentid);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "failed";
            }
        }
        public async Task<int> GetClientRoleByEmailId(string emailId)
        {
            try
            {
                return await _clientRepository.GetClientRoleByEmailId(emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public async Task<int> GetUniqueClientRoleByEmailId(string emailId, string id)
        {
            try
            {
                return await _clientRepository.GetUniqueClientRoleByEmailId(emailId, id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public async Task<string> PublicClientUrlSave(string emailId, string uniqueid)
        {
            try
            {
                return await _clientRepository.PublicClientUrlSave(emailId, uniqueid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<string> PublicInterviewerUrlSave(string emailId, string uniqueid)
        {
            try
            {
                return await _clientRepository.PublicInterviewerUrlSave(emailId, uniqueid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<string> Fetchlogoforclient(string emailId, string id)
        {
            try
            {
                return await _clientRepository.Fetchlogoforclient(emailId, id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public IEnumerable<IRF> GetRecruiterReportByClient(string emailId, DateTime st, DateTime se)
        {
            try
            {
                var irfList = _clientRepository.GetRecruiterReportByClient(emailId, st, se).Result.ToList();
                return irfList;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateRecruiterStatus(int userid, int clientid, int Irfid, bool status)
        {
            try
            {
                return _clientRepository.UpdateRecruiterStatus(userid, clientid, Irfid, status);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<int> GetClientByEmailId(string emailId)
        {
            try
            {
                return await _clientRepository.GetClientByEmailId(emailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }
        public async Task<int> GetClientBillingType(int Clientid)
        {
            try
            {
                return await _clientRepository.GetClientBillingType(Clientid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return 0;
            }
        }

        public async Task<IEnumerable<Client>> GetAllClients()
        {
            try
            {
                return await _clientRepository.GetAllClients();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        //public async Task<ClientCommentsForReport> GetClientCommentsForReport(int interviewId)
        //{
        //    try
        //    {

        //        return await _clientRepository.GetClientCommentsForReport(interviewId);
        //    }
        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return null;
        //    }
        //}

        public async Task<bool> AddUpdateClientCommentsForReport(ClientCommentsForReport model)
        {
            try
            {
                return await _clientRepository.AddUpdateClientCommentsForReport(model);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        //public async Task<ClientFeedbackCommentVM> GetClientCommentsList(int interviewId, string emailId)
        //{
        //    try
        //    {
        //        var clientCommentsList = await _clientRepository.GetClientCommentsForReport(interviewId);
        //        var model = new ClientFeedbackCommentVM();
        //        if(clientCommentsList != null)
        //        {
        //            model.ClientComments = clientCommentsList.Where(x => !string.IsNullOrEmpty(x.CreatedBy) && x.CreatedBy.ToLower() == emailId.ToLower())?.FirstOrDefault();
        //            model.ClientCommentsList = clientCommentsList.Where(x => string.IsNullOrEmpty(x.CreatedBy) || x.CreatedBy.ToLower() != emailId.ToLower());
        //        }
        //        return model;
        //    }
        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return null;
        //    }
        //}

        public async Task<ClientCommentsForReport> GetClientCommentsForReport(int interviewId, string emailId, string profile)
        {
            try
            {
                var model = new ClientCommentsForReport();
                var clientCommentsList = await _clientRepository.GetClientCommentsForReport(interviewId);

                if (clientCommentsList != null)
                {
                    if (profile == ProfileEnum.Client.GetDescription())
                    {
                        model = clientCommentsList.Where(x => !string.IsNullOrEmpty(x.CreatedBy) && x.CreatedBy.ToLower() == emailId.ToLower())?.FirstOrDefault();
                        if(model != null)
                        {
                            model.ClientCommentsList = clientCommentsList?.Where(x => string.IsNullOrEmpty(x.CreatedBy) || x.CreatedBy.ToLower() != emailId.ToLower());
                        }
                    }
                    //if (profile != ProfileEnum.Interviewer.GetDescription())
                    //{
                    //    model = clientCommentsList.Where(x => !string.IsNullOrEmpty(x.CreatedBy) && x.CreatedBy.ToLower() == emailId.ToLower())?.FirstOrDefault();
                    //    model.ClientCommentsList = clientCommentsList.Where(x => string.IsNullOrEmpty(x.CreatedBy) || x.CreatedBy.ToLower() != emailId.ToLower());
                    //}
                    else
                    {
                        model.ClientCommentsList = clientCommentsList;
                    }
                }
                return model;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<ClientAudioInstructions>> GetClientAudioForIRF(int interviewId, string emailId, string profile)
        {
            try
            {
                var clientAudioList = await _clientRepository.GetClientAudioForIRF(interviewId);
                return clientAudioList;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

    }
}
