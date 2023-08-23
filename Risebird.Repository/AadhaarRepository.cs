using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Risebird.Repository
{
    public class AadhaarRepository : IAadhaarRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly string employmentconnectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AadhaarRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            employmentconnectionString = _configuration["ConnectionString:EmploymentVerificationConnection"];
        }
        public async Task<bool> SaveAadhaarDetails(AadhaarData model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Name", model.Name);
                    parameters.Add("@Address", model.Address);
                    parameters.Add("@email", model.email);
                    parameters.Add("@reference_id", model.reference_id);
                    parameters.Add("@gender", model.gender);
                    parameters.Add("@dob", model.dob);
                    parameters.Add("@doc_face", model.doc_face);
                    parameters.Add("@verified_using", model.verified_using);
                    parameters.Add("@matchPercentage", model.matchPercentage);
                    parameters.Add("@passport_image", model.passport_image);
                    await SqlMapper.QueryAsync(con, "usp_SaveAadhaarDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<AadhaarData> FetchAadhaarDetails(string MCQId, string candidateEmail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@MCQId", MCQId);
                    parameters.Add("@candidateEmail", candidateEmail);
                    var result = await SqlMapper.QueryAsync<AadhaarData>(con, "usp_GetAadhaarDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<AadhaarDetails> FetchCandidateEmailidByUniqueId(string uniqueid)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@uniqueid", uniqueid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var uniques = await SqlMapper.QueryAsync<AadhaarDetails>(
                        con, "Select EmailId,AadhaarVerified From UserList where uniqueid=@uniqueid", parameters, commandType: System.Data.CommandType.Text);
                    return uniques.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> FetchUniqueIdByEmailid(string emailid)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@emailid", emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var uniques = await SqlMapper.QueryAsync<string>(
                        con, "Select uniqueid From UserList where EmailId=@emailid ", parameters, commandType: System.Data.CommandType.Text);
                    return uniques.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public async Task<AadhaarData> FetchInterviewAadhaarDetails(string candidateEmail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@candidateEmail", candidateEmail);
                    var result = await SqlMapper.QueryAsync<AadhaarData>(con, "usp_GetInterviewAadhaarDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<AadhaarData>> GetAadharDetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<AadhaarData>(con, "usp_GetAadhardetailsforInterview",
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> SaveCandidateVerificationDetails(candidateDetailsresult model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@name", model.name);
                    parameters.Add("@address", model.address);
                    parameters.Add("@email", model.email);
                    parameters.Add("@passportNumber", model.passportNumber);
                    parameters.Add("@gender", model.sex);
                    parameters.Add("@birthDate", model.birthDate);
                    parameters.Add("@nationality", model.nationality);
                    parameters.Add("@matchPercentage", model.matchPercentage);
                    parameters.Add("@doc_face", model.doc_face);
                    parameters.Add("@passport_image", model.passport_image);
                    await SqlMapper.QueryAsync(con, "usp_SaveCandidateverificationDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<AadhaarData> FetchInterviewCandidateDetails(string candidateEmail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@candidateEmail", candidateEmail);
                    var result = await SqlMapper.QueryAsync<AadhaarData>(con, "usp_GetInterviewCandidateDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task SaveFaceMatchScorePercentage(string email, int score)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(employmentconnectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@email", email);
                    parameters.Add("@score", score);

                    await SqlMapper.QueryAsync(con, "usp_SaveFaceMatchScore", parameters,
                         commandType: CommandType.StoredProcedure);
                    return;
                }
            }
            catch (SqlException ex)
            {
                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return;
        }
        public async Task SaveEmploymentDetails(SignzyEmployeeResponse model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(employmentconnectionString))
                {
                    //Save employment details
                    var _joiningdate = model.employmentSummary?.recent_employer_data.date_of_joining;
                    DateTime? joiningdate = !string.IsNullOrWhiteSpace(_joiningdate) ? Convert.ToDateTime(_joiningdate) : null;
                    var _exitdate = model.employmentSummary?.recent_employer_data.date_of_exit;
                    DateTime? exitdate = !string.IsNullOrWhiteSpace(_exitdate) ? Convert.ToDateTime(_exitdate) : null;
                    var _dob = model.uanDetails1.FirstOrDefault()?.date_of_birth;
                    DateTime? dob = !string.IsNullOrWhiteSpace(_dob) ? Convert.ToDateTime(_dob) : null;
                    var Uannumbers = string.Empty;
                    if (model.uanNumbers != null)
                    {
                        Uannumbers = string.Join(",", model.uanNumbers);
                    }
                    var parameters = new DynamicParameters();
                    parameters.Add("@CandidateEmail", model.CandidateEmail);
                    parameters.Add("@MobileNumber", model.MobileNumber);
                    parameters.Add("@Name", model.uanDetails1.FirstOrDefault()?.name);
                    parameters.Add("@CompanyName", model.employmentSummary?.recent_employer_data.establishment_name);
                    parameters.Add("@UAN", model.employmentSummary?.recent_employer_data.matching_uan);
                    parameters.Add("@JoiningDate", joiningdate);
                    parameters.Add("@ExitDate", exitdate);
                    parameters.Add("@TotalUAN", model.employmentSummary?.uan_count);
                    parameters.Add("@Isemployed", model.employmentSummary?.is_employed);
                    parameters.Add("@UANNumbers", Uannumbers);
                    parameters.Add("@DOB", dob);
                    parameters.Add("@message", model.message);

                    var result = await SqlMapper.QueryAsync<int>(con, "usp_SaveEmploymentDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    var employmentid = result.FirstOrDefault();
                    //Save UAN Details
                    DataTable _uanDetails = new DataTable();
                    _uanDetails.Columns.Add("EmploymentId", typeof(int));
                    _uanDetails.Columns.Add("UAN", typeof(string));
                    _uanDetails.Columns.Add("Name", typeof(string));
                    _uanDetails.Columns.Add("Gender", typeof(string));
                    _uanDetails.Columns.Add("DOB", typeof(DateTime));
                    _uanDetails.Columns.Add("CompanyName", typeof(string));
                    _uanDetails.Columns.Add("JoiningDate", typeof(DateTime));
                    foreach (var item in model.uanDetails1)
                    {
                        var _dobitem = item.date_of_birth;
                        DateTime? dobitem = !string.IsNullOrWhiteSpace(_dobitem) ? Convert.ToDateTime(_dobitem) : null;
                        var _joinitem = item.date_of_joining;
                        DateTime? joinitem = !string.IsNullOrWhiteSpace(_joinitem) ? Convert.ToDateTime(_joinitem) : null;
                        _uanDetails.Rows.Add(employmentid, item.uanNumber, item.name, item.gender, dobitem, item.establishment_name, joinitem);
                    }
                    if (model.uanDetails1.Count() > 0)
                    {
                        SqlCommand cmd = new SqlCommand("usp_SaveUANDetails", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@UANDetailBulkData", SqlDbType.Structured));
                        cmd.Parameters["@UANDetailBulkData"].Value = _uanDetails;
                        con.Open();
                        if (cmd.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
                    }
                    return;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return;
        }
        public async Task<IEnumerable<EmploymentDetailsDM>> GetEmployementDetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(employmentconnectionString))
                {
                    var result = await SqlMapper.QueryAsync<EmploymentDetailsDM>(con, "usp_GetEmploymentDetails",
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<int> CheckEmployementDetails(string mobilenumber)
        {
            try
            {
                var query = "Select Count(*) from EmploymentDetails where MobileNumber=@MobileNumber";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@MobileNumber", mobilenumber);
                using (SqlConnection con = new SqlConnection(employmentconnectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }
        public async Task<EmploymentDetailsDM> GetEmployementDetailsByMobileNumber(string mobilenumber)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(employmentconnectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@mobilenumber", mobilenumber);
                    var result = await SqlMapper.QueryAsync<EmploymentDetailsDM>(con, "usp_GetEmploymentDetailsByMobilenumber", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result?.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> CheckCompanyStatus(string CompanyName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@CompanyName", CompanyName);
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_checkBlackListCompany", parameters,
                         commandType: CommandType.StoredProcedure);
                    if (result.FirstOrDefault() == 0)
                    {
                        return true;
                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
    }
}
