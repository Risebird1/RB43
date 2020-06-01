using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;
using VoiceIt2API;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;

namespace Risebird.Repository
{
    public class VoiceItRepository : IVoiceItRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly string voiceItApiKey;
        private readonly string voiceItApiToken;
        private readonly string voiceItLanguage;
        private IApplicationErrorLogger _applicationErrorLogger;
        public VoiceItRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            voiceItApiKey = _configuration["VoiceIt:ApiKey"];
            voiceItApiToken = _configuration["VoiceIt:ApiToken"];
            voiceItLanguage = _configuration["VoiceIt:Language"];
        }

        public VoiceItResponse CreateUser()
        {
            //var result = string.Empty;
            try
            {
                VoiceIt2 myVoiceIt = new VoiceIt2(voiceItApiKey, voiceItApiToken);
                var response = myVoiceIt.CreateUser();
                var outPut = JsonConvert.DeserializeObject<VoiceItResponse>(response);
                //if (response != null)
                //{
                //    result = "success";
                //}
                return outPut;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
           

        }

        public string EnrollUser(string userId, string fileUrl, string phrase)
        {
            //var result = string.Empty;
            try
            {
                VoiceIt2 myVoiceIt = new VoiceIt2(voiceItApiKey, voiceItApiToken);
                var response = myVoiceIt.CreateVoiceEnrollmentByUrl(userId, voiceItLanguage, phrase, fileUrl);
                var outPut = JsonConvert.DeserializeObject<VoiceItResponse>(response);
                //if (outPut != null)
                //{
                //    return outPut.message;
                //}
                return outPut.message;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed";
            }


        }

        public VoiceItResponse VerifyUser(string userId, string fileUrl, string phrase)
        {
            //var result = string.Empty;
            try
            {
                VoiceIt2 myVoiceIt = new VoiceIt2(voiceItApiKey, voiceItApiToken);
                var response = myVoiceIt.VoiceVerificationByUrl(userId, voiceItLanguage, phrase, fileUrl);
                var outPut = JsonConvert.DeserializeObject<VoiceItResponse>(response);
                return outPut;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }


        }

        public PhraseResponse GetPhrases()
        {
            //var result = string.Empty;
            try
            {
                VoiceIt2 myVoiceIt = new VoiceIt2(voiceItApiKey, voiceItApiToken);
                var response = myVoiceIt.GetPhrases(voiceItLanguage);
                return JsonConvert.DeserializeObject<PhraseResponse>(response);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }


        }


    }
}
