using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Risebird.Business
{
    public class AccountService : IAccountService
    {
        private IAccountRepository _accountRepository;
        private IApplicationErrorLogger _applicationErrorLogger;

        public AccountService(IAccountRepository accountRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _accountRepository = accountRepository;
            _applicationErrorLogger = applicationErrorLogger;
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

    }
}
