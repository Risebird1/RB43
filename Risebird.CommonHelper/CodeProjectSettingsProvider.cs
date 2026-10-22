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
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                // A developer machine talks to its own functions host (func start, port 7071). The URL
                // the setup stores in the database is the deployed app's, which may run older code;
                // only an explicit configuration value sends a local web app there.
                var configured = _configuration[CodeProjectSettingNames.FunctionsBaseUrl];
                return string.IsNullOrWhiteSpace(configured) ? CodeProjectSettingNames.LocalFunctionsBaseUrl : configured.TrimEnd('/');
            }
            var url = Get(CodeProjectSettingNames.FunctionsBaseUrl);
            return string.IsNullOrWhiteSpace(url) ? null : url.TrimEnd('/');
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
