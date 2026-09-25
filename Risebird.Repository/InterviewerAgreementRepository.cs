using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class InterviewerAgreementRepository : IInterviewerAgreementRepository
    {
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public InterviewerAgreementRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<InterviewerAgreementVM> GetInterviewerAgreement(int interviewerProfileId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@InterviewerProfileId", interviewerProfileId);
            return await GetAgreement(parameters);
        }

        public async Task<InterviewerAgreementVM> GetInterviewerAgreementByZohoRequestId(string zohoRequestId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ZohoRequestId", zohoRequestId);
            return await GetAgreement(parameters);
        }

        // New request: usp_SaveInterviewerAgreement with @InterviewerProfileId.
        public Task<bool> SaveInterviewerAgreementRequest(int interviewerProfileId, ZohoSignRequestResult request, string agreementVersion)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@InterviewerProfileId", interviewerProfileId);
            parameters.Add("@ZohoRequestId", request.RequestId);
            parameters.Add("@ZohoActionId", request.ActionId);
            parameters.Add("@AgreementStatus", InterviewerAgreementStatus.Sent);
            parameters.Add("@AgreementVersion", agreementVersion);
            return SaveAgreement(parameters);
        }

        // Status update: usp_SaveInterviewerAgreement keyed by @ZohoRequestId (no @InterviewerProfileId).
        public Task<bool> UpdateInterviewerAgreementStatus(string zohoRequestId, string agreementStatus, bool isAgreementSigned,
            DateTime? signedOn, DateTime? completedOn, string documentUrl, string declineReason,
            string signerName = null, string fatherName = null, string address = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ZohoRequestId", zohoRequestId);
            parameters.Add("@AgreementStatus", agreementStatus);
            parameters.Add("@IsAgreementSigned", isAgreementSigned);
            parameters.Add("@SignedOn", signedOn);
            parameters.Add("@CompletedOn", completedOn);
            parameters.Add("@DocumentUrl", documentUrl);
            parameters.Add("@DeclineReason", declineReason);
            parameters.Add("@SignerName", signerName);
            parameters.Add("@FatherName", fatherName);
            parameters.Add("@Address", address);
            return SaveAgreement(parameters);
        }

        private async Task<bool> SaveAgreement(DynamicParameters parameters)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<int>(con, "usp_SaveInterviewerAgreement", parameters,
                        commandType: CommandType.StoredProcedure);
                    return rows.FirstOrDefault() > 0;
                }
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        private async Task<InterviewerAgreementVM> GetAgreement(DynamicParameters parameters)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<InterviewerAgreementVM>(con, "usp_GetInterviewerAgreement", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
