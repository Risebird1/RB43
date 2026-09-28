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
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@InterviewerProfileId", interviewerProfileId);
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

        public async Task<bool> SaveInterviewerAgreement(int interviewerProfileId, string agreementVersion, string signerName, string fatherName, string address, string signatureUrl)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@InterviewerProfileId", interviewerProfileId);
                parameters.Add("@AgreementVersion", agreementVersion);
                parameters.Add("@SignerName", signerName);
                parameters.Add("@FatherName", fatherName);
                parameters.Add("@Address", address);
                parameters.Add("@SignatureUrl", signatureUrl);
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
    }
}
