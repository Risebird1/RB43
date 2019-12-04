using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Repository.Interface
{
    public interface IAccountRepository
    {
        IEnumerable<Account> GetAllAccounts();
    }
}
