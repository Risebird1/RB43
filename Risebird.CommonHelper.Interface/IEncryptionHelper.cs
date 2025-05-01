using System;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IEncryptionHelper
    {
        Task<string> EncryptDateTime(DateTime dateTime);
        Task<DateTime?> DecryptDateTime(string encryptedData);
        Task<string> EncryptData(string data);
        Task<string> DecryptData(string encryptedData);
        Task<string> EncryptInteger(int id);
        Task<int?> DecryptInteger(string encryptedId);
        Task<string> EncryptIntegerValue(int id, string type);
        Task<int> DecryptIntegerValue(string encryptedId, string type);
        Task<string> EncryptStringValue(string val);
        Task<string> DecryptStringValue(string encryptedId);
    }
}
