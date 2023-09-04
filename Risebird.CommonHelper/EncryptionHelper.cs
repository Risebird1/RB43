using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class EncryptionHelper : IEncryptionHelper
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly string key;


        public EncryptionHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            key = _configuration["EncryptionDetails:Key"];

        }

        public async Task<string> EncryptDateTime(DateTime dateTime)
        {
            try
            {
                byte[] encryptedBytes;
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);
                    aesAlg.GenerateIV();

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                // Serialize datetime to JSON and write to the crypto stream
                                string serializedData = JsonConvert.SerializeObject(dateTime);
                                swEncrypt.Write(serializedData);
                            }
                        }

                        encryptedBytes = aesAlg.IV.Concat(msEncrypt.ToArray()).ToArray();

                    }

                    // Combine IV and encrypted data
                    //byte[] encryptedBytes = aesAlg.IV.Concat(msEncrypt.ToArray()).ToArray();
                    if (encryptedBytes != null)
                    {
                        return Convert.ToBase64String(encryptedBytes);

                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return string.Empty;
            }
        }

        public async Task<DateTime?> DecryptDateTime(string encryptedData)
        {
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);
                    aesAlg.IV = encryptedBytes.Take(16).ToArray(); // IV length is 16 bytes

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(encryptedBytes.Skip(16).ToArray()))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                string serializedData = srDecrypt.ReadToEnd();
                                return JsonConvert.DeserializeObject<DateTime>(serializedData);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
