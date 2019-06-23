using System.Collections.Generic;
using Risebird.Entities.Data;

namespace Risebird.Business.Interface
{
    public interface IUserService
   {
       List<Menu> GetMenuBasedOnUserEmailld(string emailId);
   }
}
