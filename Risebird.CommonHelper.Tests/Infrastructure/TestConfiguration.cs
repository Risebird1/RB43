using Microsoft.Extensions.Configuration;

namespace Risebird.CommonHelper.Tests.Infrastructure
{
    /// <summary>
    /// In-memory <see cref="IConfiguration"/> builders so helper classes that read
    /// configuration in their constructor can be exercised without appsettings.json.
    /// </summary>
    internal static class TestConfiguration
    {
        /// <summary>AES keys must be exactly 16/24/32 UTF-8 bytes — these are 32.</summary>
        public const string Key = "ThisIsA32ByteLongKeyForAES256!!!";
        public const string GenericKey = "GenericKey32BytesLongForAES256!!";
        public const string BaseEncodedKey = "BaseEncodedKey32BytesLongAES256!";

        public static IConfiguration From(params (string Key, string Value)[] values)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var (key, value) in values)
                dictionary[key] = value;

            return new ConfigurationBuilder()
                .AddInMemoryCollection(dictionary)
                .Build();
        }

        public static IConfiguration Encryption(
            string key = Key,
            string genericKey = GenericKey,
            string baseEncodedKey = BaseEncodedKey) =>
            From(
                ("EncryptionDetails:Key", key),
                ("EncryptionDetails:GenericKey", genericKey),
                ("EncryptionDetails:BaseEncodedKey", baseEncodedKey));

        public static IConfiguration Empty() => From();
    }
}
