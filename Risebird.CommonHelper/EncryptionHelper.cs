using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
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
        private readonly string genericKey;
        private readonly string baseEncodedKey;
        private readonly byte[] ExecutiveReportKey;
        private readonly byte[] _byteKey;


        public EncryptionHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            key = _configuration["EncryptionDetails:Key"];
            genericKey = _configuration["EncryptionDetails:GenericKey"];
            baseEncodedKey = _configuration["EncryptionDetails:BaseEncodedKey"];
            ExecutiveReportKey = Encoding.UTF8.GetBytes(key.ToString().PadRight(32).Substring(0, 32));
            _byteKey = Encoding.UTF8.GetBytes("ThisIsA32ByteLongKeyForAES256".PadRight(32).Substring(0, 32));

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

        public async Task<string> EncryptData(string data)
        {
            try
            {
                byte[] encryptedBytes;
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(genericKey);
                    aesAlg.GenerateIV();

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                // Serialize datetime to JSON and write to the crypto stream
                                string serializedData = JsonConvert.SerializeObject(data);
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

        public async Task<string> DecryptData(string encryptedData)
        {
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(genericKey);
                    aesAlg.IV = encryptedBytes.Take(16).ToArray(); // IV length is 16 bytes

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(encryptedBytes.Skip(16).ToArray()))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                string serializedData = srDecrypt.ReadToEnd();
                                return JsonConvert.DeserializeObject<string>(serializedData);
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

        //public async Task<string> EncryptInteger(int id)
        //{
        //    try
        //    {
        //        byte[] encryptedBytes;
        //        using (Aes aesAlg = Aes.Create())
        //        {
        //            aesAlg.Key = Encoding.UTF8.GetBytes(key);
        //            aesAlg.GenerateIV();

        //            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        //            using (MemoryStream msEncrypt = new MemoryStream())
        //            {
        //                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        //                {
        //                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
        //                    {
        //                        // Serialize integer to string and write to the crypto stream
        //                        string serializedData = id.ToString();
        //                        swEncrypt.Write(serializedData);
        //                    }
        //                }

        //                encryptedBytes = aesAlg.IV.Concat(msEncrypt.ToArray()).ToArray();
        //            }

        //            // Combine IV and encrypted data
        //            return Convert.ToBase64String(encryptedBytes);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return string.Empty;
        //    }
        //}
        //public async Task<string> EncryptInteger(int id)
        //{
        //    try
        //    {
        //        byte[] encryptedBytes;
        //        using (Aes aesAlg = Aes.Create())
        //        {
        //            //aesAlg.Key = Encoding.UTF8.GetBytes(key);
        //            aesAlg.Key = Convert.FromBase64String(baseEncodedKey);
        //            aesAlg.GenerateIV(); // Generate a new IV

        //            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        //            using (MemoryStream msEncrypt = new MemoryStream())
        //            {
        //                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        //                {
        //                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
        //                    {
        //                        // Serialize the integer to a string and write to the crypto stream
        //                        swEncrypt.Write(id.ToString());
        //                    }
        //                }

        //                // Combine the IV and encrypted bytes
        //                encryptedBytes = aesAlg.IV.Concat(msEncrypt.ToArray()).ToArray();
        //            }

        //            // Return the Base64-encoded IV + cipher text
        //            return Convert.ToBase64String(encryptedBytes);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return string.Empty;
        //    }
        //}


        //public async Task<int?> DecryptInteger(string encryptedId)
        //{
        //    try
        //    {
        //        byte[] fullCipher = Convert.FromBase64String(encryptedId);

        //        using (Aes aesAlg = Aes.Create())
        //        {
        //            aesAlg.Key = Encoding.UTF8.GetBytes(key);

        //            // Extract the IV from the encrypted bytes
        //            byte[] iv = fullCipher.Take(16).ToArray();
        //            aesAlg.IV = iv;

        //            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        //            // Extract the cipher text (excluding the IV part)
        //            byte[] cipherText = fullCipher.Skip(16).ToArray();

        //            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
        //            {
        //                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
        //                {
        //                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
        //                    {
        //                        // Read the decrypted data
        //                        string decryptedData = await srDecrypt.ReadToEndAsync();

        //                        // Convert back to an integer
        //                        return int.Parse(decryptedData);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return null;
        //    }
        //}

        //public async Task<int?> DecryptInteger(string encryptedId)
        //{
        //    try
        //    {
        //        // Convert the encrypted string from Base64
        //        byte[] fullCipher = Convert.FromBase64String(encryptedId);

        //        using (Aes aesAlg = Aes.Create())
        //        {
        //            //aesAlg.Key = Encoding.UTF8.GetBytes(key);
        //            aesAlg.Key = Convert.FromBase64String(baseEncodedKey);
        //            // Extract the first 16 bytes (AES block size) for the IV
        //            byte[] iv = fullCipher.Take(16).ToArray();
        //            aesAlg.IV = iv;

        //            // The rest is the encrypted data
        //            byte[] cipherText = fullCipher.Skip(16).ToArray();

        //            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        //            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
        //            {
        //                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
        //                {
        //                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
        //                    {
        //                        // Read the decrypted data as string
        //                        string decryptedData = await srDecrypt.ReadToEndAsync();

        //                        // Convert the decrypted string back to an integer
        //                        return int.Parse(decryptedData);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return null;
        //    }
        //}

        public async Task<string> EncryptInteger(int id)
        {
            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = _byteKey;
                    aesAlg.GenerateIV(); // Generate a new IV for each encryption

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        // Prepend the IV
                        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            // Write the integer to the crypto stream
                            await swEncrypt.WriteAsync(id.ToString());
                        }

                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return string.Empty;
            }
        }
        public async Task<int?> DecryptInteger(string encryptedId)
        {
            try
            {
                byte[] fullCipher = Convert.FromBase64String(encryptedId);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = _byteKey;

                    // Extract the IV (the first 16 bytes)
                    byte[] iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aesAlg.IV = iv;

                    // Extract the cipher text (remaining bytes after IV)
                    byte[] cipherText = new byte[fullCipher.Length - iv.Length];
                    Array.Copy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        // Read the decrypted integer from the stream
                        string decryptedData = await srDecrypt.ReadToEndAsync();
                        return int.Parse(decryptedData);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        #region SecurityEncryptionandDecryption
        public async Task<string> EncryptIntegerValue(int id, string type)
        {
            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    if (type == "ExecutiveReport")
                    {
                        aesAlg.Key = ExecutiveReportKey;
                    }
                    else
                    {
                        aesAlg.Key = _byteKey;
                    }
                    aesAlg.GenerateIV(); // Generate a new IV for each encryption

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        // Prepend the IV
                        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            // Write the integer to the crypto stream
                            await swEncrypt.WriteAsync(id.ToString());
                        }
                        var _encriptvalue = Convert.ToBase64String(msEncrypt.ToArray());
                        _encriptvalue = _encriptvalue.Replace("+", "_");
                        return _encriptvalue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return string.Empty;
            }
        }
        public async Task<int> DecryptIntegerValue(string encryptedId, string type)
        {
            try
            {
                encryptedId = encryptedId.Replace("_", "+");
                byte[] fullCipher = Convert.FromBase64String(encryptedId);

                using (Aes aesAlg = Aes.Create())
                {
                    if (type == "ExecutiveReport")
                    {
                        aesAlg.Key = ExecutiveReportKey;
                    }
                    else
                    {
                        aesAlg.Key = _byteKey;
                    }

                    // Extract the IV (the first 16 bytes)
                    byte[] iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aesAlg.IV = iv;

                    // Extract the cipher text (remaining bytes after IV)
                    byte[] cipherText = new byte[fullCipher.Length - iv.Length];
                    Array.Copy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        // Read the decrypted integer from the stream
                        string decryptedData = await srDecrypt.ReadToEndAsync();
                        return int.Parse(decryptedData);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        #endregion
    }
}
