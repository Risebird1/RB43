using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IEncryptionHelper
    {
        Task<string> EncryptDateTime(DateTime dateTime);
        Task<DateTime?> DecryptDateTime(string encryptedData);
    }
}
