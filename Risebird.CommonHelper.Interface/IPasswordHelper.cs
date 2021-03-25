using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IPasswordHelper
    {
        Task<string> GenerateUserPassword(UserDetails user);
    }
}
