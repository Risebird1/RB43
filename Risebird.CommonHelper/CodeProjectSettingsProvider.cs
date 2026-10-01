using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Code project settings: configuration first, then the CodeProjectSettings table that the
    /// one-click setup fills in. Values are cached for the process for a minute, so a setup run
    /// is picked up without a restart.
    /// </summary>
    public class CodeProjectSettingsProvider : ICodeProjectSettingsProvider
    {
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(1);
        private static readonly object Gate = new object();
        private static Dictionary<string, string> _cache;
        private static DateTime _cachedAtUtc;

        private readonly IConfiguration _configuration;
        private readonly ICodeProjectSettingsRepository _repository;
        private readonly IEncryptionHelper _encryption;

        public CodeProjectSettingsProvider(IConfiguration configuration, ICodeProjectSettingsRepository repository, IEncryptionHelper encryption)
        {
            _configuration = configuration;
            _repository = repository;
            _encryption = encryption;
        }

        public string Get(string key)
        {
            var configured = _configuration[key];
            if (!string.IsNullOrWhiteSpace(configured)) return configured;
            return Stored().TryGetValue(key, out var stored) && !string.IsNullOrWhiteSpace(stored) ? stored : null;
        }

        public string FunctionsBaseUrl()
        {
            var url = Get(CodeProjectSettingNames.FunctionsBaseUrl);
            if (!string.IsNullOrWhiteSpace(url)) return url.TrimEnd('/');
            // Running locally with nothing configured: the functions host (func start) is on 7071.
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase) ? CodeProjectSettingNames.LocalFunctionsBaseUrl : null;
        }

        public void Invalidate()
        {
            lock (Gate) { _cache = null; }
        }

        private Dictionary<string, string> Stored()
        {
            lock (Gate)
            {
                if (_cache != null && DateTime.UtcNow - _cachedAtUtc < CacheLifetime) return _cache;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in _repository.GetAll())
            {
                var value = pair.Value.Value;
                if (pair.Value.IsSecret && !string.IsNullOrEmpty(value))
                {
                    try { value = _encryption.DecryptData(value).GetAwaiter().GetResult(); }
                    catch (Exception) { value = null; }
                }
                values[pair.Key] = value;
            }

            lock (Gate)
            {
                _cache = values;
                _cachedAtUtc = DateTime.UtcNow;
                return _cache;
            }
        }
    }
}
