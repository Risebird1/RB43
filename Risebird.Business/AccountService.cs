using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectService _projectService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AccountService(IAccountRepository accountRepository, IApplicationErrorLogger applicationErrorLogger, IProjectRepository projectRepository, IProjectService projectService)
        {
            _accountRepository = accountRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _projectRepository = projectRepository;
            _projectService = projectService;
        }

        public IEnumerable<Account> GetAllAccounts()
        {
            try
            {
                return _accountRepository.GetAllAccounts();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<Account> GetAccounts(string emailId, string profile)
        {
            try
            {
                return _accountRepository.GetAccounts(emailId, profile).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public Account GetAccount()
        {
            try
            {
                var accountModel = new Account();
                var projectList = _projectRepository.GetProjects().Result.ToList();
                var objList = new List<AccountProjectMapping>();
                foreach (var item in projectList)
                {
                    var obj = new AccountProjectMapping();
                    obj.ProjectId = item.id;
                    obj.ProjectName = item.Name;
                    obj.IsAdded = false;
                    obj.Client = item.Client;
                    objList.Add(obj);
                }
                accountModel.AccountProjectMappings = (IList<AccountProjectMapping>)(objList);
                accountModel.ClientList = new SelectList(_projectService.GetClients(), "id", "Name");

                return accountModel;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> AddAccount(Account accountDetails)
        {
            try
            {
                accountDetails.AccountProjectMappings = accountDetails.AccountProjectMappings.Where(x => x.IsAdded == true).ToList();
                accountDetails.CreatedOn = DateTime.Now;
                return _accountRepository.AddAccount(accountDetails).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public Account GetAccountById(int id)
        {
            try
            {
                string projectList = string.Empty;
                var account = _accountRepository.GetAccountById(id);
                var accountCluster = _accountRepository.GetAccountCluster(id);
                if (accountCluster != null && accountCluster.Count() != 0)
                {
                    projectList = string.Join(",", from item in accountCluster select item.ProjectId);
                }
                var remainingProjects = _accountRepository.GetProjectByClientId(account.ClientId, projectList);

                var objList = new List<AccountProjectMapping>();
                objList.AddRange(accountCluster);
                objList.AddRange(remainingProjects);

                account.AccountProjectMappings = (IList<AccountProjectMapping>)(objList);
                return account;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateAccount(Account model)
        {
            try
            {
                model.AccountProjectMappings = model.AccountProjectMappings.Where(x => x.IsAdded == true).ToList();
                return _accountRepository.UpdateAccount(model).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> ArchiveAccount(int id)
        {
            try
            {
                return _accountRepository.ArchiveAccount(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> UnArchiveAccount(int id)
        {
            try
            {
                return _accountRepository.UnArchiveAccount(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public IEnumerable<AccountProjectMapping> GetProjectsbyClientId(int clientId)
        {
            try
            {
                return _accountRepository.GetProjectsbyClientId(clientId);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<AccountProjectMapping> GetProjectbyClientId(int clientId, string projectList = null)
        {
            try
            {
                return _accountRepository.GetProjectByClientId(clientId, projectList);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<AccountProjectMapping> GetAccountCluster(int accountId)
        {
            try
            {
                return _accountRepository.GetAccountCluster(accountId);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string Signout(string EmailId, string sessionId)
        {
            try
            {
                return _accountRepository.Signout(EmailId, sessionId);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public InterviewerProfileVM FetchUserProfile(string EmailId)
        {
            try
            {
                return _accountRepository.FetchUserProfile(EmailId);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> UpdateUserProfile(InterviewerProfileVM userProfile)
        {
            try
            {
                return _accountRepository.UpdateUserProfile(userProfile).Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }



    }
}
