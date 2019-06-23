using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Repository.Interface
{
    public interface IUserRepository
    {
        IEnumerable<Menu> GetMenuBasedOnUserEmailld(string emailId);
    }
}
