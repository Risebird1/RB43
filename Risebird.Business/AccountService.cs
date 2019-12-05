using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class AccountService : IAccountService
    {
        private IAccountRepository _accountRepository;
        private IProjectRepository _projectRepository;

        private IApplicationErrorLogger _applicationErrorLogger;

        public AccountService(IAccountRepository accountRepository, IApplicationErrorLogger applicationErrorLogger, IProjectRepository projectRepository)
        {
            _accountRepository = accountRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _projectRepository = projectRepository;
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

        public Account GetAccount()
        {
            try
            {
                var accountModel = new Account();
                var projectList = _projectRepository.GetAllProjects();
                var objList = new List<AccountProjectMapping>();
                foreach (var item in projectList)
                {
                    var obj = new AccountProjectMapping();
                    obj.ProjectId = item.id;
                    obj.ProjectName = item.Name;
                    obj.IsAdded = false;
                    //obj.id = 0;
                    //obj.ProjectId = 0;
                    obj.Client = item.Client;
                    objList.Add(obj);
                }
                accountModel.AccountProjectMappings = (IList<AccountProjectMapping>)(objList);
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
                var account = _accountRepository.GetAccountById(id);
                var accountCluster = _accountRepository.GetAccountCluster(id);
                var allProjects = _projectRepository.GetAllProjects();
                if (accountCluster != null && accountCluster.Count() != 0)
                {
                    foreach (var item in accountCluster)
                    {
                        //item.PrimarySkills = allIRFs.Where(x => x.id == item.IRFId).FirstOrDefault().JDTiltle.Split("|")[0];
                        //item.Client = allIRFs.Where(x => x.id == item.IRFId).FirstOrDefault().ClientCompName;
                        allProjects = allProjects.Where(x => x.id != item.ProjectId);
                    }
                }


                var objList = new List<AccountProjectMapping>();
                objList.AddRange(accountCluster);

                //if (projectCluster != null && projectCluster.Count() != 0)
                //{
                //    foreach (var item in projectCluster)
                //    {
                //        var obj = new ProjectIRFMapping();
                //        obj.IRFId = item.IRFId;
                //        obj.IRFName = item.IRFName;
                //        obj.IsAdded = item.IsAdded;
                //        //obj.id = 0;
                //        obj.PrimarySkills = item.PrimarySkills;
                //        //obj.ProjectId = 0;
                //        obj.Client = item.Client;
                //        objList.Add(obj);
                //    }

                //}

                foreach (var item in allProjects)
                {

                    var obj = new AccountProjectMapping();
                    obj.ProjectId = item.id;
                    obj.ProjectName = item.Name;
                    obj.IsAdded = false;
                    //obj.id = 0;
                    //obj.PrimarySkills = item.JDTiltle.Split("|")[0];
                    //obj.ProjectId = 0;
                    obj.Client = item.Client;
                    objList.Add(obj);
                }
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



    }
}
