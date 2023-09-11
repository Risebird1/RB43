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
using System.Threading.Tasks;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class InterviewerRepository : IInterviewerRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        private ICodeInviteRequest _codeinviteRequest;

        public InterviewerRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, ICodeInviteRequest codeinviteRequest)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            _codeinviteRequest = codeinviteRequest;
        }



        #region Interviewer Profile Repository function

        public async Task<IEnumerable<InterviewerProfileVM>> GetInterviewers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<InterviewerProfileVM>(con, "[dbo].[usp_GetInterviewerDetails]", null,//usp_GetInterviewerDetails  usp_GetInterviewerDataRN
                        commandType: StoredProcedure);
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

        //public async Task<bool> AddOrEditInterviewerProfile(InterviewerProfileVM interviewerProfile, string loggedInUser, out string assessmentGroupLi)
        //{
        //    try
        //    {
        //        int ProfileId;

        //        DynamicParameters parameters = SetSPParametersForInterviewProfile(interviewerProfile);
        //        parameters.Add("@loggedInUser", loggedInUser);
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {

        //            await SqlMapper.QueryAsync<int>(con, "usp_AddOrEditInterviewerProfile", parameters,
        //               commandType: CommandType.StoredProcedure);

        //            ProfileId = parameters.Get<int>("@InterviewerProfileId");

        //        }

        //        //To Add Skills
        //        foreach (var item in interviewerProfile.InterviewerSkill)
        //        {
        //            DynamicParameters Params = new DynamicParameters();
        //            Params.Add("@InterviewerProfileId", ProfileId);
        //            Params.Add("@SkillId", item.SkillId);
        //            Params.Add("@Category", item.Category);
        //            Params.Add("@Ip", item.Ip);
        //            Params.Add("@Level", item.Level);
        //            Params.Add("@SkillExperience", item.SkillExperience);
        //            Params.Add("@Nature", item.Nature);

        //            using (SqlConnection con = new SqlConnection(connectionString))
        //            {
        //                await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerSkills", Params,
        //               commandType: CommandType.StoredProcedure);

        //            }
        //        }

        //        foreach (var item in interviewerProfile.InterviewerCertificateDetails)
        //        {
        //            DynamicParameters Params = new DynamicParameters();
        //            Params.Add("@InterviewerProfileId", ProfileId);
        //            Params.Add("@CertificateID", item.CertificateID);
        //            Params.Add("@CertificateName", item.CertificateName);
        //            Params.Add("@Version", item.Version);
        //            Params.Add("@ExpiryDate", item.Expirydate);

        //            using (SqlConnection con = new SqlConnection(connectionString))
        //            {
        //                await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerCertificate", Params,
        //               commandType: CommandType.StoredProcedure);

        //            }
        //        }

        //        //foreach (var item in interviewerProfile.InterviewerProfessionalDetails)
        //        //{
        //        //    DynamicParameters Params = new DynamicParameters();
        //        //    Params.Add("@InterviewerProfileId", ProfileId);
        //        //    Params.Add("@CompanyName", item.CompanyName);
        //        //    Params.Add("@Type", item.Type);
        //        //    Params.Add("@CompanyStage", item.CompanyStage);
        //        //    Params.Add("@Size", item.Size);
        //        //    Params.Add("@JobFunction", item.JobFunction);
        //        //    Params.Add("@Industry", item.Industry);
        //        //    Params.Add("@Headquarters", item.Headquarters);
        //        //    Params.Add("@Role", item.Role);
        //        //    Params.Add("@Domain", item.Domain);
        //        //    Params.Add("@SeniorityLevel", item.SeniorityLevel);
        //        //    Params.Add("@Experience", item.Experience);
        //        //    Params.Add("@WorkNature", item.WorkNature);
        //        //    Params.Add("@PrimaryResponsibility", item.PrimaryResponsibility);
        //        //    Params.Add("@CompanyLinkedinPage", item.CompanyLinkedinPage);
        //        //    Params.Add("@CompanyWebsite", item.CompanyWebsite);

        //        //    using (SqlConnection con = new SqlConnection(connectionString))
        //        //    {
        //        //        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerProfessional", Params,
        //        //       commandType: CommandType.StoredProcedure);

        //        //    }
        //        //}

        //        foreach (var item in interviewerProfile.InterviewerEducationDetails)
        //        {
        //            DynamicParameters Params = new DynamicParameters();
        //            Params.Add("@InterviewerProfileId", ProfileId);
        //            Params.Add("@EducationType", item.EducationType);
        //            Params.Add("@Degree", item.Degree);
        //            Params.Add("@EducationMode", item.EducationMode);
        //            Params.Add("@CollegeType", item.CollegeType);
        //            Params.Add("@UniversityName", item.UniversityName);
        //            Params.Add("@PassOutYear", item.PassOutYear);
        //            Params.Add("@AnyGap", item.AnyGap);

        //            using (SqlConnection con = new SqlConnection(connectionString))
        //            {
        //                await SqlMapper.QueryAsync<int>(con, "[dbo].[usp_AddInterviewerEducation]", Params,
        //               commandType: CommandType.StoredProcedure);

        //            }
        //        }

        //        foreach (var item in interviewerProfile.InterviewerProjectsDetails)
        //        {
        //            DynamicParameters Params = new DynamicParameters();
        //            Params.Add("@InterviewerProfileId", ProfileId);
        //            Params.Add("@TechnologyEnvironment", item.TechnologyEnvironment);
        //            Params.Add("@IsPrimarySkillUsed", item.IsPrimarySkillUsed);
        //            Params.Add("@Description", item.Description);
        //            Params.Add("@IsCommercialProject", item.IsCommercialProject);
        //            Params.Add("@NatureOfProject", item.NatureOfProject);

        //            using (SqlConnection con = new SqlConnection(connectionString))
        //            {
        //                await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerProjects", Params,
        //               commandType: CommandType.StoredProcedure);

        //            }
        //        }

        //        foreach (var item in interviewerProfile.InterviewerAvailabilityDetails)
        //        {
        //            DynamicParameters Params = new DynamicParameters();
        //            Params.Add("@InterviewerProfileId", ProfileId);
        //            Params.Add("@Weekday", item.Weekday);
        //            Params.Add("@Weekend", item.Weekend);
        //            Params.Add("@IsAvailableOnPublicHolidays", item.IsAvailableOnPublicHolidays);

        //            using (SqlConnection con = new SqlConnection(connectionString))
        //            {
        //                await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerAvailability", Params,
        //               commandType: CommandType.StoredProcedure);

        //            }
        //        }

        //        //add interviewer account details
        //        DynamicParameters paramsAccount = new DynamicParameters();
        //        paramsAccount.Add("@InterviewerProfileId", ProfileId);
        //        paramsAccount.Add("@AccountName", interviewerProfile.InterviewerAccDetails.AccountName);
        //        paramsAccount.Add("@AccountNumber", interviewerProfile.InterviewerAccDetails.AccountNumber);
        //        paramsAccount.Add("@IFSCCode", interviewerProfile.InterviewerAccDetails.IFSCCode);
        //        paramsAccount.Add("@UPIId", interviewerProfile.InterviewerAccDetails.UPIId);
        //        paramsAccount.Add("@PAN", interviewerProfile.InterviewerAccDetails.PAN);

        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerAccDetails", paramsAccount,
        //           commandType: CommandType.StoredProcedure);

        //        }

        //        return true;

        //    }

        //    catch (SqlException e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return false;
        //    }

        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return false;
        //    }
        //}

        public bool AddOrEditInterviewerProfile(InterviewerProfileVM interviewerProfile, string loggedInUser, out string assessmentGroupList)
        {
            assessmentGroupList = string.Empty;
            try
            {
                int ProfileId;

                DynamicParameters parameters = SetSPParametersForInterviewProfile(interviewerProfile);
                parameters.Add("@loggedInUser", loggedInUser);
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    _ = SqlMapper.Query<int>(con, "usp_AddOrEditInterviewerProfile", parameters,
                       commandType: CommandType.StoredProcedure);

                    //ProfileId = parameters.Get<int?>("@InterviewerProfileId");
                    int? profileOut = (int?)parameters.Get<int?>("@InterviewerProfileId");
                    ProfileId = profileOut.HasValue ? profileOut.Value : 0;
                    assessmentGroupList = parameters.Get<string>("@assessmentGroupList");
                    if (!string.IsNullOrEmpty(assessmentGroupList))
                    {
                        return false;
                    }

                }

                //To Add Skills
                foreach (var item in interviewerProfile.InterviewerSkill)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@SkillId", item.SkillId);
                    Params.Add("@Category", item.Category);
                    Params.Add("@Ip", item.Ip);
                    Params.Add("@Level", item.Level);
                    Params.Add("@SkillExperience", item.SkillExperience);
                    Params.Add("@Nature", item.Nature);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        _ = SqlMapper.Query<int>(con, "usp_AddInterviewerSkills", Params,
                        commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerCertificateDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@CertificateID", item.CertificateID);
                    Params.Add("@CertificateName", item.CertificateName);
                    Params.Add("@Version", item.Version);
                    Params.Add("@ExpiryDate", item.Expirydate);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        _ = SqlMapper.Query<int>(con, "usp_AddInterviewerCertificate", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                //foreach (var item in interviewerProfile.InterviewerProfessionalDetails)
                //{
                //    DynamicParameters Params = new DynamicParameters();
                //    Params.Add("@InterviewerProfileId", ProfileId);
                //    Params.Add("@CompanyName", item.CompanyName);
                //    Params.Add("@Type", item.Type);
                //    Params.Add("@CompanyStage", item.CompanyStage);
                //    Params.Add("@Size", item.Size);
                //    Params.Add("@JobFunction", item.JobFunction);
                //    Params.Add("@Industry", item.Industry);
                //    Params.Add("@Headquarters", item.Headquarters);
                //    Params.Add("@Role", item.Role);
                //    Params.Add("@Domain", item.Domain);
                //    Params.Add("@SeniorityLevel", item.SeniorityLevel);
                //    Params.Add("@Experience", item.Experience);
                //    Params.Add("@WorkNature", item.WorkNature);
                //    Params.Add("@PrimaryResponsibility", item.PrimaryResponsibility);
                //    Params.Add("@CompanyLinkedinPage", item.CompanyLinkedinPage);
                //    Params.Add("@CompanyWebsite", item.CompanyWebsite);

                //    using (SqlConnection con = new SqlConnection(connectionString))
                //    {
                //        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerProfessional", Params,
                //       commandType: CommandType.StoredProcedure);

                //    }
                //}

                foreach (var item in interviewerProfile.InterviewerEducationDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@EducationType", item.EducationType);
                    Params.Add("@Degree", item.Degree);
                    Params.Add("@EducationMode", item.EducationMode);
                    Params.Add("@CollegeType", item.CollegeType);
                    Params.Add("@UniversityName", item.UniversityName);
                    Params.Add("@PassOutYear", item.PassOutYear);
                    Params.Add("@AnyGap", item.AnyGap);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        _ = SqlMapper.Query<int>(con, "[dbo].[usp_AddInterviewerEducation]", Params,
                        commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerProjectsDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@TechnologyEnvironment", item.TechnologyEnvironment);
                    Params.Add("@IsPrimarySkillUsed", item.IsPrimarySkillUsed);
                    Params.Add("@Description", item.Description);
                    Params.Add("@IsCommercialProject", item.IsCommercialProject);
                    Params.Add("@NatureOfProject", item.NatureOfProject);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        _ = SqlMapper.Query<int>(con, "usp_AddInterviewerProjects", Params,
                        commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerAvailabilityDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@Weekday", item.Weekday);
                    Params.Add("@Weekend", item.Weekend);
                    Params.Add("@IsAvailableOnPublicHolidays", item.IsAvailableOnPublicHolidays);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        _ = SqlMapper.Query<int>(con, "usp_AddInterviewerAvailability", Params,
                        commandType: CommandType.StoredProcedure);

                    }
                }

                //add interviewer account details
                DynamicParameters paramsAccount = new DynamicParameters();
                paramsAccount.Add("@InterviewerProfileId", ProfileId);
                paramsAccount.Add("@AccountName", interviewerProfile.InterviewerAccDetails.AccountName);
                paramsAccount.Add("@AccountNumber", interviewerProfile.InterviewerAccDetails.AccountNumber);
                paramsAccount.Add("@IFSCCode", interviewerProfile.InterviewerAccDetails.IFSCCode);
                paramsAccount.Add("@UPIId", interviewerProfile.InterviewerAccDetails.UPIId);
                paramsAccount.Add("@PAN", interviewerProfile.InterviewerAccDetails.PAN);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    _ = SqlMapper.Query<int>(con, "usp_AddInterviewerAccDetails", paramsAccount,
                    commandType: CommandType.StoredProcedure);

                }

                return true;

            }

            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        private DynamicParameters SetSPParametersForInterviewProfile(InterviewerProfileVM entity)
        {
            var parameters = new DynamicParameters();
            // For Add case, UserId will be null and for edit case, it will have some value
            parameters.Add("@UserId", entity.UserId);
            parameters.Add("@FirstName", entity.FirstName);
            parameters.Add("@LastName", entity.LastName);
            parameters.Add("@NickName", entity.NickName);
            parameters.Add("@EmailId", entity.EmailId);
            parameters.Add("@AltEmailId", entity.AltEmailId);
            entity.DOB = entity.DOB < new DateTime(1753, 1, 1) ? new DateTime(1753, 1, 1) : entity.DOB;
            parameters.Add("@DOB", entity.DOB);
            parameters.Add("@Gender", entity.Gender);
            parameters.Add("@AnnualSalary", entity.AnnualSalary);
            parameters.Add("@Country", entity.Country);
            parameters.Add("@City", entity.City);
            parameters.Add("@MobileNumber", entity.MobileNumber);
            parameters.Add("@AltMobileNumber", entity.AltMobileNumber);
            parameters.Add("@CurrentRole", entity.CurrentRole);
            parameters.Add("@Experience", entity.Experience);
            parameters.Add("@PreferredDomain", entity.PreferredDomain);
            parameters.Add("@GitHubLink", entity.GitHubLink);
            parameters.Add("@ResumeLink", entity.ResumeLink);
            parameters.Add("@CodedResumeLink", entity.CodedResumeLink);
            parameters.Add("@StackOverflowLink", entity.StackOverflowLink);
            parameters.Add("@LinkedinLink", entity.LinkedinLink);
            parameters.Add("@Blog", entity.Blog);
            parameters.Add("@IsActive", entity.IsActive);
            parameters.Add("@IsApproved", entity.IsApproved);
            parameters.Add("@IsLeadInterviewer", entity.IsLeadInterviewer);
            if (entity.AssessmentGroupArray != null)
            {
                entity.AssessmentGroupId = string.Join(',', entity.AssessmentGroupArray);
            }
            parameters.Add("@AssessmentGroupId", entity.AssessmentGroupId);
            parameters.Add("@AssessmentGroupDifference", entity.AssessmentGroupDifference);
            parameters.Add("@RemovedAssessmentGroups", entity.RemovedAssessmentGroups ?? null);
            parameters.Add("@UserPassword", entity.UserPassword);
            parameters.Add("@WhatsappNumber", entity.WhatsappNumber);
            parameters.Add("@Isinstaavailable", entity.Isinstaavailable);
            parameters.Add("@InterviewerProfileId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            //parameters.Add("@IsServiceDomain", entity.IsServiceDomain);
            //parameters.Add("@IsProductDomain", entity.IsProductDomain);
            parameters.Add("@IndiaServiceRate", entity.IndiaServiceRate);
            parameters.Add("@IndiaProductRate", entity.IndiaProductRate);
            parameters.Add("@OutsideIndiaServiceRate", entity.OutsideIndiaServiceRate);
            parameters.Add("@OutsideIndiaProductRate", entity.OutsideIndiaProductRate);
            parameters.Add("@isVerified", entity.isVerified);
            parameters.Add("@CompanyName", entity.CompanyName);
            parameters.Add("@IsEmployed", entity.IsEmployed);
            //parameters.Add("@assessmentGroupList", dbType: DbType.String, direction: ParameterDirection.Output);
            parameters.Add("@assessmentGroupList", dbType: DbType.String, direction: ParameterDirection.Output, size: 5215585);

            return parameters;
        }
        private DynamicParameters SetSPParametersForSkills(InterviewerProfileVM entity)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@UserId", entity.UserId);
            parameters.Add("@FirstName", entity.FirstName);
            parameters.Add("@LastName", entity.LastName);
            parameters.Add("@NickName", entity.NickName);
            parameters.Add("@EmailId", entity.EmailId);
            parameters.Add("@AltEmailId", entity.AltEmailId);
            parameters.Add("@DOB", entity.DOB);
            parameters.Add("@Gender", entity.Gender);
            parameters.Add("@AnnualSalary", entity.AnnualSalary);
            parameters.Add("@Country", entity.Country);
            parameters.Add("@City", entity.City);
            parameters.Add("@MobileNumber", entity.MobileNumber);
            parameters.Add("@AltMobileNumber", entity.AltMobileNumber);
            parameters.Add("@CurrentRole", entity.CurrentRole);
            parameters.Add("@Experience", entity.Experience);
            parameters.Add("@PreferredDomain", entity.PreferredDomain);
            parameters.Add("@GitHubLink", entity.GitHubLink);
            parameters.Add("@ResumeLink", entity.ResumeLink);
            parameters.Add("@CodedResumeLink", entity.CodedResumeLink);
            parameters.Add("@StackOverflowLink", entity.StackOverflowLink);
            parameters.Add("@LinkedinLink", entity.LinkedinLink);
            parameters.Add("@Blog", entity.Blog);
            parameters.Add("@IsActive", entity.IsActive);
            parameters.Add("@IsApproved", entity.IsApproved);
            parameters.Add("@IsLeadInterviewer", entity.IsLeadInterviewer);
            parameters.Add("@InterviewerProfileId", SqlDbType.Int, direction: ParameterDirection.Output);

            return parameters;
        }

        public async Task<bool> DeleteInterviewerProfile(int UserId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@UserId", UserId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "[dbo].[usp_DeleteInterviewerProfile]", parameters,
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


        public async Task<bool> AddEmployerProfile(InterviewerProfessional item)
        {
            try
            {
                DynamicParameters Params = SetSPParametersForEmployer(item);


                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "[dbo].[usp_AddInterviewerProfessional]", Params,
                   commandType: CommandType.StoredProcedure);

                }
                return true;

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

        public async Task<bool> EditEmployerProfile(InterviewerProfessional item)
        {
            try
            {
                DynamicParameters parameters = SetSPParametersForEmployer(item);
                parameters.Add("@Id", item.id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_EditInterviewerProfessional", parameters,
                   commandType: CommandType.StoredProcedure);

                }
                return true;

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

        public async Task<bool> DeleteEmployerProfile(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "[dbo].[usp_DeleteInterviewerEmployer]", parameters,
                   commandType: CommandType.StoredProcedure);

                }
                return true;

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
        private DynamicParameters SetSPParametersForEmployer(InterviewerProfessional item)
        {
            DynamicParameters Params = new DynamicParameters();
            Params.Add("@InterviewerProfileId", item.InterviewerProfileId);
            Params.Add("@CompanyName", item.CompanyName);
            Params.Add("@Type", item.Type);
            Params.Add("@CompanyStage", item.CompanyStage);
            Params.Add("@Size", item.Size);
            Params.Add("@JobFunctionId", item.JobFunctionId);
            Params.Add("@IndustryId", item.IndustryId);
            Params.Add("@Headquarters", item.Headquarters);
            Params.Add("@Role", item.Role);
            Params.Add("@Domain", item.Domain);
            Params.Add("@SeniorityLevel", item.SeniorityLevel);
            Params.Add("@ExperienceId", item.ExperienceId);
            Params.Add("@WorkNature", item.WorkNature);
            Params.Add("@PrimaryResponsibility", item.PrimaryResponsibility);
            Params.Add("@CompanyLinkedinPage", item.CompanyLinkedinPage);
            Params.Add("@CompanyWebsite", item.CompanyWebsite);
            return Params;
        }
        //Get NickNames
        public IEnumerable<string> GetNickNames(int UserId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@UserId", UserId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<string>(
                        con, "select NickName from [dbo].[InterviewerProfile] where UserId != @UserId", parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        //Get NickNames
        public IEnumerable<string> GetEmailIds(int UserId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@UserId", UserId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<string>(
                        con, "select Upper(EmailId) from [dbo].[UserList] where Id != @UserId", parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<LevelTech> GetLevelTech()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<LevelTech>(con, "Select Id, Name, Description from LevelTech",
                        null, commandType: Text);
                }
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public List<ExperienceRange> GetExperienceRange()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<ExperienceRange>(con, "Select Id, Description from ExperienceRanges",
                        null, commandType: Text).ToList();
                }
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<JobFunction> GetJobFunction()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<JobFunction>(con, "Select Id,  Description from JobFunction",
                        null, commandType: Text).ToList();
                }
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<Industry> GetIndustry()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Industry>(con, "Select Id, Description from IndustryCodes",
                        null, commandType: Text).ToList();
                }
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<InterviewerProfileVM>> GetInterviewerProfileById(int userid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@UserId", userid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var ProfileList = await SqlMapper.QueryAsync<InterviewerProfileVM>(con, "usp_GetInterviewerProfileByUserId",
                        parameters, commandType: CommandType.StoredProcedure);
                    return ProfileList;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<InterviewerProfileVM>> GetInterviewerProfileByEmailId(string emailId, string profileName)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                parameters.Add("@Profile", profileName);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var ProfileList = await SqlMapper.QueryAsync<InterviewerProfileVM>(con, "usp_GetInterviewerProfileByEmailId",
                        parameters, commandType: CommandType.StoredProcedure);
                    return ProfileList;
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<InterviewerProfessional> GetEmployerById(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    IEnumerable<InterviewerProfessional> result = await SqlMapper.QueryAsync<InterviewerProfessional>(con, "[dbo].[usp_GetEmployerById]",
                        parameters, commandType: CommandType.StoredProcedure);
                    return result.ToList().FirstOrDefault();
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<TransInterviewerSkill>> GetSkills(int interviewerId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@interviewerId", interviewerId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //Id, InterviewerProfileId,  SkillId, Level, SkillName
                    return await SqlMapper.QueryAsync<TransInterviewerSkill>(
                        con, "select trs.*,sk.Name as SkillName from [dbo].[TransInterviewerSkill] trs  join Skill sk on sk.id = trs.SkillId where trs.InterviewerProfileId = @interviewerId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        //certi
        public async Task<IEnumerable<TransInterviewerCertificate>> GetCertificateList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //Id, InterviewerProfileId,  CertificateID, CertificateName
                    return await SqlMapper.QueryAsync<TransInterviewerCertificate>(
                        con, "select * from [dbo].[TransInterviewerCertificate] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        //Education
        public async Task<IEnumerable<TransInterviewerEducation>> GetEducationList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //Id, InterviewerProfileId,  Graduation, CollegeType, Specialization, Score
                    return await SqlMapper.QueryAsync<TransInterviewerEducation>(
                        con, "select * from [dbo].[TransInterviewerEducation] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        // Profess
        public async Task<IEnumerable<InterviewerProfessional>> GetProfessionalList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<InterviewerProfessional>(
                        con, "select Id, InterviewerProfileId,  CompanyName, CompanyType, Domain, StartDate, EndDate from [dbo].[TransInterviewerProfessional] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        //Project
        public async Task<IEnumerable<TransInterviewerProjects>> GetProjectsList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //Id, InterviewerProfileId,  ProjectName, ClientName, StartDate, EndDate, Role, ProjectDescriptions
                    return await SqlMapper.QueryAsync<TransInterviewerProjects>(
                        con, "select * from [dbo].[TransInterviewerProjects] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        //Availability
        public async Task<IEnumerable<TransInterviewerAvailability>> GetAvailabilityList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // Id, InterviewerProfileId,  DayTimeSlot, DaySlot, StartTime, EndTime
                    return await SqlMapper.QueryAsync<TransInterviewerAvailability>(
                        con, "select * from [dbo].[TransInterviewerAvailability] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        //AccDetails
        public async Task<TransInterviewerAccDetails> GetAccDetailsList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //Id, InterviewerProfileId,  AccountHolderName, AccountNumber, IFSCCode, Location, UPIId
                    return (await SqlMapper.QueryAsync<TransInterviewerAccDetails>(
                        con, "select * from [dbo].[TransInterviewerAccDetails] where InterviewerProfileId = @ProfileId",
                        parameters, commandType: Text)).FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<List<InterviewerProfessional>> GetEmployerListById(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@IProfileId", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    IEnumerable<InterviewerProfessional> result = await SqlMapper.QueryAsync<InterviewerProfessional>(con, "[dbo].[usp_GetEmployerListById]",
                        parameters, commandType: CommandType.StoredProcedure);
                    return result.ToList();
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        #region Interviewer My Profile Repository function
        public async Task<bool> AddInterviewerMyProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {

                // In this case, Only passing the required parameters to fil in the tables
                var parameters = new DynamicParameters();

                parameters.Add("@UserId", interviewerProfile.UserId);
                parameters.Add("@FirstName", interviewerProfile.FirstName);
                parameters.Add("@LastName", interviewerProfile.LastName);
                parameters.Add("@EmailId", interviewerProfile.EmailId);
                parameters.Add("@AltEmailId", interviewerProfile.AltEmailId);
                parameters.Add("@MobileNumber", interviewerProfile.MobileNumber);
                parameters.Add("@AltMobileNumber", interviewerProfile.AltMobileNumber);
                parameters.Add("@ResumeLink", interviewerProfile.ResumeLink);
                parameters.Add("@InterviewerProfileId", interviewerProfile.id);
                parameters.Add("@Isinstaavailable", interviewerProfile.Isinstaavailable);
                parameters.Add("@CodedResumeLink", interviewerProfile.CodedResumeLink);
                parameters.Add("@Isavailable", interviewerProfile.Isavailable);
                parameters.Add("@VideoConfirmation", interviewerProfile.VideoConfirmation);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_MyProfile", parameters,
                       commandType: CommandType.StoredProcedure);

                }

                foreach (var item in interviewerProfile.InterviewerAvailabilityDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", interviewerProfile.id);
                    Params.Add("@Weekday", item.Weekday);
                    Params.Add("@Weekend", item.Weekend);
                    Params.Add("@IsAvailableOnPublicHolidays", item.IsAvailableOnPublicHolidays);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerAvailability", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                //add interviewer account details
                DynamicParameters paramsAccount = new DynamicParameters();
                paramsAccount.Add("@InterviewerProfileId", interviewerProfile.id);
                paramsAccount.Add("@AccountName", interviewerProfile.InterviewerAccDetails.AccountName);
                paramsAccount.Add("@AccountNumber", interviewerProfile.InterviewerAccDetails.AccountNumber);
                paramsAccount.Add("@IFSCCode", interviewerProfile.InterviewerAccDetails.IFSCCode);
                paramsAccount.Add("@UPIId", interviewerProfile.InterviewerAccDetails.UPIId);
                paramsAccount.Add("@PAN", interviewerProfile.InterviewerAccDetails.PAN);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerAccDetails", paramsAccount,
                   commandType: CommandType.StoredProcedure);

                }
                string queryupdate = "Update Userlist Set FirstName= @FirstName,LastName= @LastName,MobileNumber=@MobileNumber,WhatsappNumber=@WhatsappNumber,ImageURL=IsNull(@ImageURL,ImageURL) where Emailid=@Emailid";
                var parametersupdate = new DynamicParameters();

                parametersupdate.Add("@FirstName", interviewerProfile.FirstName);
                parametersupdate.Add("@LastName", interviewerProfile.LastName);
                parametersupdate.Add("@Emailid", interviewerProfile.EmailId);
                parametersupdate.Add("@MobileNumber", interviewerProfile.MobileNumber);
                parametersupdate.Add("@WhatsappNumber", interviewerProfile.WhatsappNumber);
                parametersupdate.Add("@ImageURL", interviewerProfile.ImageURL);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query(queryupdate, parametersupdate);
                }
                return true;
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        private DynamicParameters SetSPParametersForInterviewMyProfile(InterviewerProfileVM entity)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@UserId", entity.UserId);
            parameters.Add("@FirstName", entity.FirstName);
            parameters.Add("@LastName", entity.LastName);
            parameters.Add("@EmailId", entity.EmailId);
            parameters.Add("@AltEmailId", entity.AltEmailId);
            parameters.Add("@City", entity.City);
            parameters.Add("@MobileNumber", entity.MobileNumber);
            parameters.Add("@AltMobileNumber", entity.AltMobileNumber);
            parameters.Add("@ResumeLink", entity.ResumeLink);
            parameters.Add("@InterviewerProfileId", SqlDbType.Int, direction: ParameterDirection.Output);

            return parameters;
        }
        #endregion

        #region unapproved interviewer
        //GetInterviewerInvites
        public IEnumerable<InterviewerInvites> GetInterviewerInvites(string emailId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = SqlMapper.Query<InterviewerInvites>(con, "usp_GetUnapprovedInterviewerInvites", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<Meeting>> GetUnapprovedInterviewerScheduledInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_GetUnapprovedInterviewerScheduledInterviews", parameters,
                         commandType: CommandType.StoredProcedure);
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

        #endregion

        #region lead interviewer
        public async Task<IEnumerable<Meeting>> GetOpenInterviewerAssessment(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_OpenInterviewerAssessment", parameters,
                         commandType: CommandType.StoredProcedure);
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

        public async Task<IEnumerable<Meeting>> GetAssignedInterviewerAssessment(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_MyInterviewerAssessment", parameters,
                         commandType: CommandType.StoredProcedure);
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

        public string AssignYourself(int meetingid, string interviewerMail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "update [dbo].[UnapprovedInterviewerMeetings] set interviewer=@value where id = @meetingid", new { value = interviewerMail, meetingid = meetingid }, commandType: Text);
                    return "Interview assigned successfully.";
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
        }

        public async Task<bool> UnassignYourself(int meetingid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", meetingid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "usp_UnAssignLeadInterviewer", parameters, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public MeetingVM GetMeetingById(int meetingid)
        {
            try
            {
                var query = "select interviewDate, candidateEmail,ISCId as IRFId from UnapprovedInterviewerMeetings where id = @meetingid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", meetingid);
                dynamicParameters.Add("@value", 2);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<MeetingVM>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        #endregion
        public IRFDetails GetInterviewerSkillChannelDetails(int id)
        {
            try
            {
                var query = "Select InterviewerSkillId as ISCId, JDTiltle, sk.[Name] as PrimarySkill from InterviewerSkillsChannel isc join Skill sk on sk.id=isc.PrimarySkill where isc.id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<IRFDetails>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<MeetingVM> GetInterviewerMeetingById(int id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@interviewId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryFirstAsync<MeetingVM>(con, "usp_GetInterviewerMeetingById", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return null;
        }
        public IEnumerable<SkillComment> GetTechSkillComments(int id)
        {
            try
            {
                var query = "Select * from TransTechSkillComments where InterviewerSkillId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<SkillComment>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<InterviewReport> GetInterviewerReportById(int id, int irfId = 0)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewId", id);//@IrfId
                dynamicParameters.Add("@IrfId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<InterviewReport>(con, "usp_GetInterviewerReportById", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public bool GetInterviewerSkillChannelbyId(int irfId)
        {
            try
            {
                var query = "Select * from InterviewerSkillsChannel where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<IRF>(query, dynamicParameters);
                    if (result.Count() > 0)
                    {
                        return true;
                    }
                    else
                        return false;
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<TransSkill> GetSkillsbyIRFid(int id)
        {
            try
            {
                var query = "Select * from TransInterviewerSkillsTechSkills where InterviewerSkillId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSkill>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<InterviewReport> GetInterviewerRecommendationbyinterviewid(int id)
        {
            try
            {
                var query = "Select * from InterviewerRecommendation where Interviewid= @Interviewid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Interviewid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<InterviewReport>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> AddInterviewerReport(InterviewReport reportDetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //check if the data is edited
                    InterviewComments interviewComments = new InterviewComments();
                    DynamicParameters parameters = new DynamicParameters();
                    DynamicParameters parametersRecomm = new DynamicParameters();
                    DynamicParameters paramx = new DynamicParameters();
                    DynamicParameters paramPersonality = new DynamicParameters();

                    paramx.Add("@InterviewId", reportDetails.InterviewId);
                    paramx.Add("@SkillQuality", reportDetails.SkillQuality);
                    paramx.Add("@OverallScore", reportDetails.OverallScore);
                    //paramx.Add("@Rating", reportDetails.Rating);
                    paramx.Add("@TechSkillComments", reportDetails.TechSkillComments);
                    paramx.Add("@CertificationQuality", reportDetails.CertificationQuality);
                    paramx.Add("@EducationQuality", reportDetails.EducationQuality);
                    paramx.Add("@EmployersQuality", reportDetails.EmployersQuality);
                    paramx.Add("@CareerProgression", reportDetails.CareerProgression);
                    paramx.Add("@ProjectsQuality", reportDetails.ProjectsQuality);
                    paramx.Add("@BehavioralScore", reportDetails.BehavioralScore);
                    paramx.Add("@InterviwerCommunication", reportDetails.InterviwerCommunication);
                    paramx.Add("@InterviewExperience", reportDetails.Interviewexperience);
                    paramx.Add("@OnlinePresence", reportDetails.OnlinePresence);
                    paramx.Add("@ProfileScore", reportDetails.ProfileScore);

                    parameters.Add("@interviewId", reportDetails.InterviewId);
                    parameters.Add("@interviewStatus", reportDetails.InterviewStatus);
                    parameters.Add("@MeetingStatus", reportDetails.MeetingStatus);
                    parameters.Add("@interviewerComment", reportDetails.InterviewerComment);
                    parameters.Add("@audiolink", reportDetails.AudioLink);
                    parameters.Add("@videolink", reportDetails.VideoLink);
                    parameters.Add("@ThumbnailImage", reportDetails.ThumbnailImage);
                    parameters.Add("@FeedbackAudioLink", reportDetails.FeedbackAudioLink);
                    parameters.Add("@DecisionMaking", reportDetails.DecisionMaking);
                    parameters.Add("@Articulation", reportDetails.Articulation);
                    parameters.Add("@ReadymadeAssessment", reportDetails.ReadymadeAssessment);
                    parameters.Add("@ReadinessOnlineInterviews", reportDetails.ReadinessOnlineInterviews);
                    parameters.Add("@Teamwork", reportDetails.Teamwork);
                    parameters.Add("@RelationshipManagement", reportDetails.RelationshipManagement);
                    parameters.Add("@ConflictResolution", reportDetails.ConflictResolution);
                    parameters.Add("@Adaptability", reportDetails.Adaptability);
                    parameters.Add("@ModeInterviews", reportDetails.ModeInterviews);
                    parameters.Add("@WorkUnderPressure", reportDetails.WorkUnderPressure);
                    parameters.Add("@TimeManagement", reportDetails.TimeManagement);
                    parameters.Add("@Interviewscompleted", reportDetails.Interviewscompleted);
                    parameters.Add("@Creativity", reportDetails.Creativity);
                    parameters.Add("@OverallInterviewingAbilityScore", reportDetails.OverallInterviewingAbilityScore);
                    parameters.Add("@InterviewerProfileId", reportDetails.InterviewerProfileId);

                    parametersRecomm.Add("@interviewId", reportDetails.InterviewId);
                    parametersRecomm.Add("@CompanySize", reportDetails.CompanySize);
                    //parametersRecomm.Add("@JobFunction", reportDetails.JobFunction);
                    //parametersRecomm.Add("@Industry", reportDetails.Industry);
                    parametersRecomm.Add("@Country", reportDetails.Country);
                    parametersRecomm.Add("@CompanyType", reportDetails.CompanyType);
                    parametersRecomm.Add("@CompanyStage", reportDetails.CompanyStage);
                    parametersRecomm.Add("@SkillCategory", reportDetails.SkillCategory);
                    parametersRecomm.Add("@Yearsofexperience", reportDetails.Yearsofexperience);
                    parametersRecomm.Add("@Senioritylevel", reportDetails.Senioritylevel);
                    parametersRecomm.Add("@SkillNature", reportDetails.SkillNature);
                    parametersRecomm.Add("@Domain", reportDetails.Domain);
                    //parametersRecomm.Add("@SkillIP", reportDetails.SkillIP);
                    parametersRecomm.Add("@PerHourRateIndia", reportDetails.PerHourRateIndia);
                    parametersRecomm.Add("@PerHourRateOutside", reportDetails.PerHourRateOutside);
                    parametersRecomm.Add("@ProfileType", reportDetails.ProfileType);
                    parametersRecomm.Add("@CompentencyLevel", reportDetails.CompentencyLevel);
                    if (reportDetails.Domain == "Other")
                    {
                        parametersRecomm.Add("@DomainName", reportDetails.DomainName);
                    }
                    else
                    {
                        parametersRecomm.Add("@DomainName", "");
                    }
                    paramPersonality.Add("@InterviewId", reportDetails.InterviewId);
                    paramPersonality.Add("@OverallPersonalityScore", reportDetails.OverallPersonalityScore);
                    paramPersonality.Add("@Openness", reportDetails.Openness);
                    paramPersonality.Add("@Conscientiousness", reportDetails.Conscientiousness);
                    paramPersonality.Add("@Extroversion", reportDetails.Extroversion);
                    paramPersonality.Add("@Agreeableness", reportDetails.Agreeableness);
                    paramPersonality.Add("@Neuroticism", reportDetails.Neuroticism);
                    DynamicParameters parametersPoints = new DynamicParameters();
                    if (reportDetails.MeetingStatus == 4)
                    {
                        parametersPoints.Add("@Points", 100);
                    }
                    else if (reportDetails.MeetingStatus == 1 || reportDetails.MeetingStatus == 3)
                    {
                        parametersPoints.Add("@Points", 33);
                    }
                    else
                    {
                        parametersPoints.Add("@Points", 0);
                    }
                    parametersPoints.Add("@interviewId", reportDetails.InterviewId);
                    parametersPoints.Add("@UserEmail", reportDetails.UserDetails);
                    parametersPoints.Add("@Type", "Interview");

                    if (reportDetails.IsEdited)
                    {
                        var result = SqlMapper.Query<int>(con, "usp_UpdateInterviewerReport", paramx,
                                            commandType: CommandType.StoredProcedure);

                    }
                    else
                    {
                        //add to interviewReport table
                        string sqlInsertReport = "INSERT INTO InterviewerReport (InterviewerSkillId, InterviewId, CandidateName,OverallScore, TechSkillComments,SkillQuality,CertificationQuality, EducationQuality, EmployersQuality,CareerProgression,ProjectsQuality,OnlinePresence,BehavioralScore,InterviwerCommunication,InterviewExperience, ProfileScore) Values   (@InterviewerSkillId, @InterviewId, @CandidateName,@OverallScore, @TechSkillComments, @SkillQuality,   @CertificationQuality, @EducationQuality, @EmployersQuality,@CareerProgression,@ProjectsQuality,@OnlinePresence,@BehavioralScore,@InterviwerCommunication,@InterviewExperience, @ProfileScore);";
                        var affectedRows = con.ExecuteAsync(sqlInsertReport, new
                        {
                            InterviewerSkillId = reportDetails.ISCId,
                            InterviewId = reportDetails.InterviewId,
                            CandidateName = reportDetails.CandidateName,
                            EducationQuality = reportDetails.EducationQuality,
                            OverallScore = reportDetails.OverallScore,
                            //Rating = reportDetails.Rating,
                            TechSkillComments = reportDetails.TechSkillComments,
                            SkillQuality = reportDetails.SkillQuality,
                            CertificationQuality = reportDetails.CertificationQuality,
                            EmployersQuality = reportDetails.EmployersQuality,
                            CareerProgression = reportDetails.CareerProgression,
                            ProjectsQuality = reportDetails.ProjectsQuality,
                            OnlinePresence = reportDetails.OnlinePresence,
                            BehavioralScore = reportDetails.BehavioralScore,
                            InterviwerCommunication = reportDetails.InterviwerCommunication,
                            InterviewExperience = reportDetails.Interviewexperience,
                            ProfileScore = reportDetails.ProfileScore
                        }).Result;

                    }

                    DataTable techReportDt = new DataTable();
                    techReportDt.Columns.Add("InterviewId", typeof(int));
                    techReportDt.Columns.Add("RequiredLevelId", typeof(int));
                    techReportDt.Columns.Add("SkillId", typeof(int));
                    techReportDt.Columns.Add("ActualLevelId", typeof(int));
                    techReportDt.Columns.Add("WeightageRequired", typeof(int));
                    techReportDt.Columns.Add("WeightageActual", typeof(int));
                    techReportDt.Columns.Add("TechComments", typeof(string));


                    foreach (var item in reportDetails.TechSkillReportCollection)
                    {
                        if (string.IsNullOrWhiteSpace(item.TechComments))
                        {
                            item.TechComments = "";
                        }
                        techReportDt.Rows.Add(reportDetails.InterviewId, item.RequiredLevelId, item.SkillId, item.ActualLevelId, item.WeightageRequired, item.WeightageActual, item.TechComments);
                    }
                    SqlCommand cmdTechReport = new SqlCommand("usp_InterviewerTechSkillReportBulkInsert", con);
                    cmdTechReport.CommandType = CommandType.StoredProcedure;
                    cmdTechReport.Parameters.Add(new SqlParameter("@TechSkillReportBulkData", SqlDbType.Structured));
                    cmdTechReport.Parameters["@TechSkillReportBulkData"].Value = techReportDt;
                    con.Open();
                    if (cmdTechReport.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }


                    DataTable questionsDt = new DataTable();
                    questionsDt.Columns.Add("InterviewId", typeof(int));
                    questionsDt.Columns.Add("Question", typeof(string));
                    questionsDt.Columns.Add("RatingId", typeof(int));
                    questionsDt.Columns.Add("Timing", typeof(int));

                    foreach (var item in reportDetails.InterviewerQuestionsCollection)
                    {
                        questionsDt.Rows.Add(reportDetails.InterviewId, item.Question, item.RatingId, item.Timing);
                    }

                    SqlCommand cmdQuestions = new SqlCommand("usp_InterviewerQuestionsDiscussedBulkInsert", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdQuestions.Parameters.Add(new SqlParameter("@InterviewerQuestionsDiscussedBulkData", SqlDbType.Structured));
                    cmdQuestions.Parameters["@InterviewerQuestionsDiscussedBulkData"].Value = questionsDt;
                    con.Open();
                    if (cmdQuestions.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }

                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewerReportComments", parameters,
                            commandType: CommandType.StoredProcedure);
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewerReportRecommendation", parametersRecomm,
                           commandType: CommandType.StoredProcedure);

                    await SqlMapper.QueryAsync(con, "usp_AddUpdateInterviewerPersonalityScore", paramPersonality,
                           commandType: CommandType.StoredProcedure);

                    SqlMapper.Query(con, "usp_InterviewerAddPoints", parametersPoints,
                            commandType: CommandType.StoredProcedure);

                    return true;
                }

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<AssignmentVM> GetInterviewerAssessment(string emailid)
        {
            try
            {
                int userid = 0;
                var query = "Select id from UserList where Emailid = @Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    userid = con.Query<int>(query, dynamicParameters).FirstOrDefault();
                }
                var finalquery = "Select ia.id,ia.Overview,ia.Senioritylevel,ia.Difficultylevel,ia.TestedSkill,ag.AssessmentGroup,ia.isApproved as IsApproved,@emailid as Createdby from InterviewerAssessments ia join AssessmentGroup ag on ia.Assessmentgroupid=ag.id Where ia.createdby = @createdby";
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@createdby", userid);
                finaldynamicParameters.Add("@emailid", emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<AssignmentVM>(finalquery, finaldynamicParameters);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string AddAssessment(AssignmentVM _assessment)
        {
            try
            {
                int userid = 0;
                int assessmentid = 0;
                var query = "Select id from UserList where Emailid = @Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", _assessment.Createdby);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    userid = con.Query<int>(query, dynamicParameters).FirstOrDefault();
                }
                //Add Assessment
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@Assessmentgroupid", Convert.ToInt32(_assessment.AssessmentGroup));
                finaldynamicParameters.Add("@Overview", _assessment.Overview);
                finaldynamicParameters.Add("@Senioritylevel", _assessment.Senioritylevel);
                finaldynamicParameters.Add("@YearsofExprience", _assessment.YearsofExprience);
                finaldynamicParameters.Add("@AssessmentType", _assessment.AssessmentType);
                finaldynamicParameters.Add("@TestedSkill", _assessment.TestedSkill);
                finaldynamicParameters.Add("@Evaluation", _assessment.Evaluation);
                finaldynamicParameters.Add("@Environment", _assessment.Environment);
                finaldynamicParameters.Add("@Difficultylevel", _assessment.Difficultylevel);
                finaldynamicParameters.Add("@EmployerType", _assessment.EmployerType);
                finaldynamicParameters.Add("@EmployerStage", _assessment.EmployerStage);
                finaldynamicParameters.Add("@EmployerSize", _assessment.EmployerSize);
                finaldynamicParameters.Add("@createdby", userid);
                finaldynamicParameters.Add("@Delivery", _assessment.Delivery);
                finaldynamicParameters.Add("@Assessmentid", _assessment.id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    assessmentid = SqlMapper.Query<int>(con, "usp_AddAssessment", finaldynamicParameters,
                                          commandType: CommandType.StoredProcedure).FirstOrDefault();

                }
                //Add question set
                DataTable questionsDt = new DataTable();
                questionsDt.Columns.Add("id", typeof(int));
                questionsDt.Columns.Add("Interviewid", typeof(int));
                questionsDt.Columns.Add("Question", typeof(string));
                questionsDt.Columns.Add("Priority", typeof(int));
                questionsDt.Columns.Add("VideoTitle", typeof(string));
                questionsDt.Columns.Add("VideoLink", typeof(string));
                questionsDt.Columns.Add("ThumbnailLink", typeof(string));

                foreach (var item in _assessment.Questionslist)
                {
                    if (!string.IsNullOrEmpty(item.QuestionCovered))
                    {
                        questionsDt.Rows.Add(assessmentid, 0, item.QuestionCovered, item.Level, item.Time, item.VideoLink, item.Skills);
                    }
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmdQuestions = new SqlCommand("usp_AssessmentQuestionInsert", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdQuestions.Parameters.Add(new SqlParameter("@QuestionsDiscussedBulkData", SqlDbType.Structured));
                    cmdQuestions.Parameters["@QuestionsDiscussedBulkData"].Value = questionsDt;
                    con.Open();
                    if (cmdQuestions.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public IEnumerable<AssignmentVM> FetchAssessmentGroups()
        {
            try
            {
                var query = "Select id,AssessmentGroup,Isinstaavailable,ISNULL(IsRelevant,0) as IsRelevant from AssessmentGroup";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<AssignmentVM>(query, null);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string ChangeStatusAssessment(int assessmentid, int status)
        {
            try
            {
                var query = "Update InterviewerAssessments set isApproved=@isapproved where id=@assessmentid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@isapproved", status);
                dynamicParameters.Add("@assessmentid", assessmentid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query(query, dynamicParameters);
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public string UpdateAssessment(AssignmentVM _assessment)
        {
            try
            {
                int userid = 0;
                int assessmentid = 0;
                var query = "Select id from UserList where Emailid = @Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", _assessment.Createdby);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    userid = con.Query<int>(query, dynamicParameters).FirstOrDefault();
                }
                //Add Assessment
                var finaldynamicParameters = new DynamicParameters();
                finaldynamicParameters.Add("@Assessmentgroupid", Convert.ToInt32(_assessment.AssessmentGroup));
                finaldynamicParameters.Add("@Overview", _assessment.Overview);
                finaldynamicParameters.Add("@Senioritylevel", _assessment.Senioritylevel);
                finaldynamicParameters.Add("@YearsofExprience", _assessment.YearsofExprience);
                finaldynamicParameters.Add("@AssessmentType", _assessment.AssessmentType);
                finaldynamicParameters.Add("@TestedSkill", _assessment.TestedSkill);
                finaldynamicParameters.Add("@Evaluation", _assessment.Evaluation);
                finaldynamicParameters.Add("@Environment", _assessment.Environment);
                finaldynamicParameters.Add("@Difficultylevel", _assessment.Difficultylevel);
                finaldynamicParameters.Add("@EmployerType", _assessment.EmployerType);
                finaldynamicParameters.Add("@EmployerStage", _assessment.EmployerStage);
                finaldynamicParameters.Add("@EmployerSize", _assessment.EmployerSize);
                finaldynamicParameters.Add("@createdby", userid);
                finaldynamicParameters.Add("@Delivery", _assessment.Delivery);
                finaldynamicParameters.Add("@Assessmentid", _assessment.id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    assessmentid = SqlMapper.Query<int>(con, "usp_AddAssessment", finaldynamicParameters,
                                          commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
                //Add question set
                DataTable questionsDt = new DataTable();
                questionsDt.Columns.Add("id", typeof(int));
                questionsDt.Columns.Add("Interviewid", typeof(int));
                questionsDt.Columns.Add("Question", typeof(string));
                questionsDt.Columns.Add("Priority", typeof(int));
                questionsDt.Columns.Add("VideoTitle", typeof(string));
                questionsDt.Columns.Add("VideoLink", typeof(string));
                questionsDt.Columns.Add("ThumbnailLink", typeof(string));

                foreach (var item in _assessment.Questionslist)
                {
                    if (!string.IsNullOrEmpty(item.QuestionCovered))
                    {
                        questionsDt.Rows.Add(assessmentid, 0, item.QuestionCovered, item.Level, item.Time, item.VideoLink, item.Skills);
                    }
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmdQuestions = new SqlCommand("usp_AssessmentQuestionInsert", con)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmdQuestions.Parameters.Add(new SqlParameter("@QuestionsDiscussedBulkData", SqlDbType.Structured));
                    cmdQuestions.Parameters["@QuestionsDiscussedBulkData"].Value = questionsDt;
                    con.Open();
                    if (cmdQuestions.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }

        public async Task<IEnumerable<InterviewerQuestionsDiscussed>> GetQuestionsDiscussed(int interviewId)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@InterviewId", interviewId);
                    //parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<InterviewerQuestionsDiscussed>(con, "usp_GetInterviewerQuestionsDiscussed", dynamicParameters,
                         commandType: CommandType.StoredProcedure);

                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<string>> GetAllTagsFromIRF()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<string>(con, "[dbo].[usp_GetAllTagsFromIRF]", null,
                        commandType: StoredProcedure);
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
        public int GetCandidateId(int id, string irfName)
        {
            int result = 0;
            try
            {
                var query = "select cm.id from UnapprovedInterviewerMeetings im join UserList ul on im.candidateEmail = ul.EmailId join CandidatesMaster cm on cm.UserId=ul.id where im.ISCId = @irfName and im.id =@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@irfName", irfName);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    result = con.Query<int>(query, dynamicParameters).FirstOrDefault();
                    return result;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return result;
            }
        }
        public IEnumerable<TechSkillReport> GetTechSkillReport(int id)
        {
            try
            {
                var query = "select a.*, Skill.[Name] as SkillName from (select ts.*, [LevelTech].[Name] as RequiredLevel from InterviewerTechSkillReport ts inner join [LevelTech] on ts.RequiredLevelId=[LevelTech].id) as a inner join Skill on a.SkillId = Skill.id where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TechSkillReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<InterviewReport> GetInterviewerRecommendationDetails(int Userid)
        {
            try
            {
                //var query = "Select * from InterviewerRecommendation where UserId= @UserId";
                var query = "Select ir.*,ip.CodedResumeLink,ip.AssessmentGroupId from InterviewerRecommendation ir left join InterviewerProfile ip on ip.Userid=ir.Userid where ir.UserId= @UserId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@UserId", Userid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<InterviewReport>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> InterviewerRecommendation(InterviewReport reportDetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters parametersRecomm = new DynamicParameters();
                    parametersRecomm.Add("@interviewId", reportDetails.InterviewId);
                    parametersRecomm.Add("@CompanySize", reportDetails.CompanySize);
                    parametersRecomm.Add("@Country", reportDetails.Country);
                    parametersRecomm.Add("@CompanyType", reportDetails.CompanyType);
                    parametersRecomm.Add("@CompanyStage", reportDetails.CompanyStage);
                    parametersRecomm.Add("@SkillCategory", reportDetails.SkillCategory);
                    parametersRecomm.Add("@Yearsofexperience", reportDetails.Yearsofexperience);
                    parametersRecomm.Add("@Senioritylevel", reportDetails.Senioritylevel);
                    parametersRecomm.Add("@SkillNature", reportDetails.SkillNature);
                    parametersRecomm.Add("@Domain", reportDetails.Domain);
                    parametersRecomm.Add("@PerHourRateIndia", reportDetails.PerHourRateIndia);
                    parametersRecomm.Add("@PerHourRateOutside", reportDetails.PerHourRateOutside);
                    parametersRecomm.Add("@ProfileType", reportDetails.ProfileType);
                    parametersRecomm.Add("@CompentencyLevel", reportDetails.CompentencyLevel);
                    parametersRecomm.Add("@Userid", reportDetails.Userid);
                    parametersRecomm.Add("@AssessmentGroupId", reportDetails.AssessmentGroupId);
                    parametersRecomm.Add("@CodedResumeLink", reportDetails.CodedResumeLink);
                    parametersRecomm.Add("@Trained", reportDetails.Trained);
                    parametersRecomm.Add("@DomainName", "");
                    parametersRecomm.Add("@IndiaProductRate", reportDetails.IndiaProductRate);
                    parametersRecomm.Add("@OutsideIndiaProductRate", reportDetails.OutsideIndiaProductRate);

                    if (reportDetails.Domain == "Other")
                    {
                        parametersRecomm.Add("@DomainName", reportDetails.DomainName);
                    }

                    await SqlMapper.QueryAsync<int>(con, "usp_AddOrUpdateInterviewerRecommendation", parametersRecomm,
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<TransInterviewerAccDetails> fetchBankDetails(int interviewerid)
        {
            try
            {
                var query = "Select * from TransInterviewerAccDetails Where InterviewerProfileId=@id";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters parametersRecomm = new DynamicParameters();
                    parametersRecomm.Add("@id", interviewerid);


                    var results = await SqlMapper.QueryAsync<TransInterviewerAccDetails>(con, query, parametersRecomm,
                             commandType: CommandType.Text);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public async Task<List<TransInterviewerAvailability>> fetchAvailDetails(int interviewerid)
        {
            try
            {

                var query = "Select * from TransInterviewerAvailability Where InterviewerProfileId=@id";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters parametersRecomm = new DynamicParameters();
                    parametersRecomm.Add("@id", interviewerid);


                    var results = await SqlMapper.QueryAsync<TransInterviewerAvailability>(con, query, parametersRecomm,
                             commandType: CommandType.Text);
                    return results.ToList();
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string GetUserRolebyEmailid(string Emailid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Emailid", Emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<string>(
                        con, "Select p.Name from Userlist ul join Profile p on p.id=ul.idProfile where ul.Emailid = @Emailid", parameters, commandType: Text);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public async Task<List<InterviewerRating>> fetchClientFeedbackbyInterviewer(string emailid, string type)
        {
            try
            {
                if (type == "Candidate")
                {
                    var query = "Select cr.*,cr.Comment as CommentforInterviewer,IRF.JDTiltle as PrimarySkill,im.Interviewer as interviewerEmail,im.CandidateEmail as EmailAddress,im.Interviewdate from CandidateRatings cr join InterviewMeetings im on cr.Interviewid=im.id join IRF on im.IRFId=IRF.IRFId where im.confirmationStatus=1  and im.Interviewer=@Emailid";
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        DynamicParameters parametersRecomm = new DynamicParameters();
                        parametersRecomm.Add("@Emailid", emailid);
                        var results = await SqlMapper.QueryAsync<InterviewerRating>(con, query, parametersRecomm,
                                 commandType: CommandType.Text);
                        return results.ToList();
                    }
                }
                else
                {
                    var query = "Select im.id as interviewid,im.interviewdate,im.candidateName as Name,ir.Comment,ir.StarRating From InterviewRatings ir join InterviewMeetings im on im.id=ir.Interviewid join Userlist ul on im.Interviewer=ul.Emailid where ul.Emailid=@Emailid union Select im.id as interviewid,im.interviewdate,im.candidateName as Name,ir.Comment,ir.Rating as StarRating From ClientCommentsForReport ir join InterviewMeetings im on im.id=ir.Interviewid join Userlist ul on im.Interviewer=ul.Emailid where ul.Emailid=@Emailid";
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        DynamicParameters parametersRecomm = new DynamicParameters();
                        parametersRecomm.Add("@Emailid", emailid);
                        var results = await SqlMapper.QueryAsync<InterviewerRating>(con, query, parametersRecomm,
                                 commandType: CommandType.Text);
                        return results.ToList();
                    }
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public string FetchClientName(string IRFId)
        {
            try
            {
                var query = "Select c.Name from IRF Join Client c on c.id=IRF.ClientId where IRF.IRFId=@IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<string>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<CommonInterviewDetailsVM>> FetchAllInterviewerallUpcomingInterview(string type, int? irfId  = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@type", type);
                    parameters.Add("@irfId", irfId);
                    var results = await SqlMapper.QueryAsync<CommonInterviewDetailsVM>(con, "usp_FetchAllInterviewerupcommingInterviewes", parameters,
                           commandType: CommandType.StoredProcedure);
                    return (List<CommonInterviewDetailsVM>)results;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public async Task<List<string>> FetchAllPasswordResetDetails(int noofdays)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@noofdays", noofdays);
                    var results = await SqlMapper.QueryAsync<string>(con, "usp_FetchPasswordresetDetails", parameters,
                           commandType: CommandType.StoredProcedure);
                    return (List<string>)results;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public async Task<List<TdsCertificateReport>> FetchTdsCertificatesbyEmailid(string Emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", Emailid);
                    var results = await SqlMapper.QueryAsync<TdsCertificateReport>(con, "usp_FetchInterviewerTdsCertificates", parameters,
                           commandType: CommandType.StoredProcedure);
                    return (List<TdsCertificateReport>)results;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public bool AddTdsCertificates(TdsCertificateReport model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", model.emailid);
                    parameters.Add("@year", model.year);
                    parameters.Add("@comment", model.comment);
                    parameters.Add("@clink", model.certificatelink);
                    parameters.Add("@month", model.month);
                    parameters.Add("@id", model.id);
                    var results = SqlMapper.Query(con, "usp_AddCertificates", parameters,
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return false;
        }
        public TdsCertificateReport FetchTdsCertificatebyid(int certificateid)
        {
            try
            {
                var query = "Select * from InterviewerCertificates where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", certificateid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<TdsCertificateReport>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public async Task<IEnumerable<IRFInterviewerMappingVM>> IRFInterviewerMappingByInterviewerid(string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", emailid);
                    var results = await SqlMapper.QueryAsync<IRFInterviewerMappingVM>(con, "usp_IRFInterviewerMappingByInterviewerid", parameters,
                           commandType: CommandType.StoredProcedure);
                    return results;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public async Task<IEnumerable<IRFInterviewerMembers>> GetAllInterviewerMembers()
        {
            try
            {
                var query = "select EmailId,uniqueid,(IsNUll(FirstName,'')+' '+ISnull(LastName,'')) as InterviewerName from UserList where idProfile=3 and DATEDIFF(Day,LastLogin,Getdate())>90";
                //var query = "select EmailId,uniqueid,(IsNUll(FirstName,'')+' '+ISnull(LastName,'')) as InterviewerName from UserList ul join InterviewerProfile ip on ip.Userid=ul.id where idProfile=3 and ip.AvailabilityResponseDate is null and ul.IsActive=1";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<IRFInterviewerMembers>(query, null);
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
        public bool UpdateAllIntervieweravailbilityStatus(int status, string uniqueid)
        {
            try
            {
                var query = "Update ti set ti.IsAvailable=@status, ti.AvailabilityResponseDate = @timeNow from InterviewerProfile ti join UserList ul on ul.id=ti.Userid Where ul.uniqueid=@uniqueid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@uniqueid", uniqueid);
                dynamicParameters.Add("@status", status);
                dynamicParameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query(query, dynamicParameters);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<List<CommonInterviewDetailsVM>> FetchAllStaffingAgencyAndClientHRwithTime(DateTime stdt, DateTime etdt)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@stdate", stdt);
                    parameters.Add("@etdate", etdt);
                    var results = await SqlMapper.QueryAsync<CommonInterviewDetailsVM>(con, "usp_GetCandidateStaffingAgencyAndClientHRByTime", parameters,
                           commandType: CommandType.StoredProcedure);
                    return (List<CommonInterviewDetailsVM>)results;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        #endregion
        #region Client Feed
        public async Task<IEnumerable<ClientFeedDataVM>> GetClientFeedForInterviewer(string interviewerEmail, DateTime? postWhichDate, int? interviewId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@interviewerEmail", interviewerEmail);
                parameters.Add("@timeNow", DateTime.Now);
                parameters.Add("@postWhichDate", postWhichDate);
                parameters.Add("@interviewId", interviewId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<ClientFeedDataVM>(con, "[usp_GetClientFeedForInterviewer]", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        #endregion
        public async Task<bool> SendInviteCode(CodeInviteVM codeInviteVM, string loggedinUser)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserName", codeInviteVM.userName);
                    parameters.Add("@Experience", codeInviteVM.experience);
                    parameters.Add("@ProgrammingLanguage", codeInviteVM.programmingLanguage);
                    parameters.Add("@DifficultyLevel", codeInviteVM.difficultyLevel);
                    parameters.Add("@InviteStartTime", codeInviteVM.inviteStartTime);
                    parameters.Add("@InviteEndTime", codeInviteVM.inviteEndTime);
                    parameters.Add("@TimeZone", "IST");
                    parameters.Add("@CreatedBy", loggedinUser);
                    parameters.Add("@CreatedAt", DateTime.Now);
                    parameters.Add("@ModifiedBy", loggedinUser);
                    parameters.Add("@invite_id", codeInviteVM.inviteId);
                    parameters.Add("@invite_UrL", codeInviteVM.invite_UrL);
                    parameters.Add("@candidateEmail", codeInviteVM.candidateEmail);
                    parameters.Add("@botURL", codeInviteVM.botURL);

                    var results = await SqlMapper.QueryAsync(con, "usp_InsertCodeInviteDetails", parameters,
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<CodeInviteVM>> GetAllIntviteDetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<CodeInviteVM>(con, "usp_FetchAllCodeInviteDetails", null,
                           commandType: CommandType.StoredProcedure);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<CodeInviteReportVM> CheckCodeConsoleReport(string inviteid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@inviteid", inviteid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<CodeInviteReportVM>(
                        con, "Select * From CodeInviteDetails where inviteId=@inviteid", parameters, commandType: Text);
                    var resultsets = result.FirstOrDefault();
                    if (!string.IsNullOrEmpty(resultsets.submissionId))
                    {
                        DynamicParameters parameters1 = new DynamicParameters();
                        parameters1.Add("@inviteid", inviteid);
                        parameters1.Add("@submissionId", resultsets.submissionId);
                        var resultquestions = await SqlMapper.QueryAsync<CodeInviteReportDetails>(
                           con, "Select * From CodeInviteReports where inviteId=@inviteid and submissionId=@submissionId", parameters1, commandType: Text);
                        resultsets.codeinvitereport = (List<CodeInviteReportDetails>)resultquestions;
                    }
                    return resultsets;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public async Task<bool> AddCodeConsoleReport(CodeInviteReportVM model)
        {
            try
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@inviteid", model.inviteId);
                dynamicParameters.Add("@submissionId", model.submissionId);
                dynamicParameters.Add("@assignmentId", model.assignmentId);
                dynamicParameters.Add("@submittedBy", model.submittedBy);
                dynamicParameters.Add("@submittedTime", model.submittedTime);
                dynamicParameters.Add("@score", model.score);
                dynamicParameters.Add("@percentageScore", model.percentageScore);
                dynamicParameters.Add("@totalAssignmentScore", model.totalAssignmentScore);
                dynamicParameters.Add("@status", model.status);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync(con, "usp_UpdateCodeInviteReport", dynamicParameters,
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return false;
        }
        public async Task<bool> AddCodeConsoleReportDetails(CodeInviteReportVM model)
        {
            try
            {
                DataTable ReportDt = new DataTable();
                ReportDt.Columns.Add("inviteId", typeof(string));
                ReportDt.Columns.Add("submissionId", typeof(string));
                ReportDt.Columns.Add("questionid", typeof(int));
                ReportDt.Columns.Add("questionName", typeof(string));
                ReportDt.Columns.Add("questionDescription", typeof(string));
                ReportDt.Columns.Add("candidateSolution", typeof(string));
                ReportDt.Columns.Add("achievedScore", typeof(int));
                ReportDt.Columns.Add("questionScore", typeof(int));
                ReportDt.Columns.Add("questionstatus", typeof(string));
                ReportDt.Columns.Add("input", typeof(string));
                ReportDt.Columns.Add("expectedOutput", typeof(string));
                ReportDt.Columns.Add("actualOutput", typeof(string));
                ReportDt.Columns.Add("testcasestatus", typeof(string));
                int questionnumber = 1;
                foreach (var item in model.codeinvitereport)
                {
                    foreach (var testcase in item.testCaseResults)
                    {
                        ReportDt.Rows.Add(model.inviteId, model.submissionId, questionnumber, item.questionName, item.questionDescription, item.candidateSolution,
                        item.achievedScore, item.questionScore, item.status, testcase.input, testcase.expectedOutput, testcase.actualOutput, testcase.status);
                    }
                    questionnumber++;
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmdReport = new SqlCommand("usp_CodeReportBulkInsert", con);
                    cmdReport.CommandType = CommandType.StoredProcedure;
                    cmdReport.Parameters.Add(new SqlParameter("@CodeReportBulkData", SqlDbType.Structured));
                    cmdReport.Parameters["@CodeReportBulkData"].Value = ReportDt;
                    con.Open();
                    if (cmdReport.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return false;
        }

        public async Task<bool> DeleteAllHistorybefore90days()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    var results = await SqlMapper.QueryAsync<int>(con, "usp_DeleteAllHistorybefore90days",
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> AddInterviewerAssesssmentDetails(int Assessmentid, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Assessmentid", Assessmentid);
                    parameters.Add("@emailid", emailid);
                    var results = await SqlMapper.QueryAsync(con, "usp_AddAssessmentInterviewerDetails", parameters,
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> UpdateInterviewerInterest(int status, string uniqueid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    if (status == 1)
                    {
                        parameters.Add("@status", true);
                    }
                    else
                    {
                        parameters.Add("@status", false);
                    }
                    parameters.Add("@uniqueid", uniqueid);
                    var results = await SqlMapper.QueryAsync(con, "usp_UpdateInterviewerinterest", parameters,
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> SaveInterviewerResume(InterviewerProfileVM model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", model.EmailId);
                    parameters.Add("@ResumeLink", model.ResumeLink);
                    var results = await SqlMapper.QueryAsync(con, "usp_SaveInterviewerResume", parameters,
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        /// <summary>
        /// Returns all the skill channels for which the current logged in interviewer
        /// has been assigned as an SME(Subject Matter Expert)
        /// </summary>
        /// <param name="loggedInInterviewer"></param>
        /// <returns></returns>
        public async Task<IEnumerable<SkillChannelDetailsForSME>> GetSkillChannelsAssignedToSME(string loggedInInterviewer)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@loggedInInterviewer", loggedInInterviewer);
                    return await SqlMapper.QueryAsync<SkillChannelDetailsForSME>(con, "usp_GetSkillChannelsAssignedToSME", parameters,
                           commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IRFVm> GetSkillChannelDetailsForSMEByIdToUpdate(int irfId, int interviewerProfileId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@interviewerProfileId", interviewerProfileId);
                    var result = await SqlMapper.QueryAsync<IRFVm>(con, "usp_GetSkillChannelDetailsForSMEByIdToUpdate", parameters,
                           commandType: CommandType.StoredProcedure);
                    return result?.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> UpdateSkillChannelDetailsBySME(IRFVm model, int interviewerProfileId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {


                    DataTable techSkillDt = new DataTable();
                    techSkillDt.Columns.Add("IRFId", typeof(int));
                    techSkillDt.Columns.Add("LevelId", typeof(int));
                    techSkillDt.Columns.Add("SkillId", typeof(int));
                    techSkillDt.Columns.Add("Weightage", typeof(int));
                    techSkillDt.Columns.Add("Concepts", typeof(string));
                    techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                    techSkillDt.Columns.Add("Timelimit", typeof(int));

                    foreach (var item in model.SkillCollection)
                    {
                        if (string.IsNullOrEmpty(item.Concepts))
                        {
                            item.Concepts = "";
                        }
                        if (string.IsNullOrEmpty(item.Natureofquestion))
                        {
                            item.Natureofquestion = "";
                        }
                        techSkillDt.Rows.Add(model.id, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit);
                    }

                    SqlCommand cmdUpdateIRF = new SqlCommand("usp_UpdateSkillChannelDetailsBySME", con);
                    cmdUpdateIRF.CommandType = CommandType.StoredProcedure;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@irfId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@irfId"].Value = model.id;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@interviewerProfileId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@interviewerProfileId"].Value = interviewerProfileId;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JDLink", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@JDLink"].Value = model.JDLink;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@timeNow", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@timeNow"].Value = DateTime.Now;

                    if (techSkillDt != null && techSkillDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    }

                    con.Open();

                    if (await cmdUpdateIRF.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }

                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> SaveSMEAudio(int irfId, int interviewerProfileId, string feedbackAudioLink)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfId", irfId);
                dynamicParameters.Add("@interviewerProfileId", interviewerProfileId);
                dynamicParameters.Add("@feedbackAudioLink", feedbackAudioLink);
                dynamicParameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(con, "usp_SaveSMEFeedbackAudio", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public async Task<bool> UpdateVerifiedInterviewers(List<TransInterviewer> interviewers, string loggedInSME)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable verifiedDt = new DataTable();
                    verifiedDt.Columns.Add("id", typeof(int));
                    verifiedDt.Columns.Add("IsSMEVerified", typeof(bool));

                    foreach (var item in interviewers)
                    {
                        verifiedDt.Rows.Add(item.id, item.IsSMEVerified);
                    }

                    SqlCommand cmdUpdateVerifiedInterviewers = new SqlCommand("usp_UpdateVerifiedInterviewers", con);
                    cmdUpdateVerifiedInterviewers.CommandType = CommandType.StoredProcedure;


                    if (verifiedDt != null && verifiedDt.Rows.Count > 0)
                    {
                        cmdUpdateVerifiedInterviewers.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                        cmdUpdateVerifiedInterviewers.Parameters["@InterviewerBulkData"].Value = verifiedDt;
                    }

                    cmdUpdateVerifiedInterviewers.Parameters.Add(new SqlParameter("@smeEmail", SqlDbType.VarChar));
                    cmdUpdateVerifiedInterviewers.Parameters["@smeEmail"].Value = loggedInSME;

                    con.Open();

                    if (await cmdUpdateVerifiedInterviewers.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }

                    return true;
                }

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


        public async Task<bool> CheckIfSME(string loggedInUser)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@emailID", loggedInUser);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QuerySingleAsync<bool>(con, "usp_CheckIfSME", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }



        public async Task<List<ClientIRFsFeedbackVM>> FetchClientIRFFeedbackList()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<ClientIRFsFeedbackVM>(con, "usp_FetchClientIRFFeedbackCountList", null,
                           commandType: CommandType.StoredProcedure);
                    return results.ToList();
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public async Task<InterviewerProfileVM> GetInterviewerFlagDetails(string intervieweremailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", intervieweremailid);
                    var results = await SqlMapper.QueryAsync<InterviewerProfileVM>(con, "usp_FetchInterviewersFlags", parameters,
                           commandType: CommandType.StoredProcedure);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> UpdateInterviewerFlag(string Emailid, bool? LeadInterviewer, bool? IsApproved, bool? IsActive)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", Emailid);
                    parameters.Add("@LeadInterviewer", LeadInterviewer);
                    parameters.Add("@IsApproved", IsApproved);
                    parameters.Add("@IsActive", IsActive);
                    var results = await SqlMapper.QueryAsync(con, "usp_UpdateInterviewerFlags", parameters,
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<IEnumerable<StatsDetailsVM>> GetInterviewerStatsAndRatingsDetails(int interviewerUserId, string type, int? irfId = null, string clientEmailId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@interviewerUserId", interviewerUserId);
                    parameters.Add("@type", type);
                    parameters.Add("@clientEmailId", clientEmailId);
                    return await SqlMapper.QueryAsync<StatsDetailsVM>(con, "dbo.usp_GetInterviewerStatsAndRatingsDetails", parameters, commandTimeout: 600,
                        commandType: CommandType.StoredProcedure);
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

        public async Task<bool> DeactivateSkillChannelInterviewer(int irfId, string interviewerEmail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@interviewerEmail", interviewerEmail);
                    _ = await SqlMapper.QueryAsync(con, "dbo.usp_DeactivateSkillChannelInterviewer", parameters, commandTimeout: 600,
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

        public async Task<IEnumerable<InterviewerStatsVM>> GetInterviewerStatsAndRatings(int? irfId = null, string clientEmailId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@clientEmailId", clientEmailId);
                    return await SqlMapper.QueryAsync<InterviewerStatsVM>(con, "dbo.usp_GetInterviewerStatsAndRatings", parameters, commandTimeout: 600,
                        commandType: CommandType.StoredProcedure);
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
        public async Task<bool> SaveInterviewerEmploymentVerification(InterviewerProfileVM _interviewer)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", _interviewer.EmailId);
                    parameters.Add("@CompanyName", _interviewer.CompanyName ?? string.Empty);
                    parameters.Add("@IsEmployed", _interviewer.IsEmployed);
                    parameters.Add("@EmploymentComment", _interviewer.EmploymentComment ?? string.Empty);
                    parameters.Add("@isVerified", _interviewer.isVerified);
                    await SqlMapper.QueryAsync<InterviewerStatsVM>(con, "dbo.usp_SaveInterviewerEmploymentDetails", parameters, commandTimeout: 600,
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

        public async Task<bool> AddSuspiciousType(SuspiciousInterviewerVM model)
        {
            try
            {
                DynamicParameters dynamicParam = new DynamicParameters();
                dynamicParam.Add("@Email", model.EmailId);
                dynamicParam.Add("@Comment",model.Comment);
                dynamicParam.Add("@CreatedDate", model.Createddate);
                dynamicParam.Add("@CreatedBy", model.CreatedBy);
                dynamicParam.Add("@SuspiciousStatus", model.SuspiciousStatus);
                using (SqlConnection conChange = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(conChange, "usp_InsertInterviewerSuspicious", dynamicParam,
                       commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> UpdateVideoConfirmationStatus(int status,string Emailid)
        {
            try
            {
                DynamicParameters dynamicParam = new DynamicParameters();
                dynamicParam.Add("@Emailid", Emailid);
                dynamicParam.Add("@status", status);
                using (SqlConnection conChange = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(conChange, "usp_UpdateVideoConfirmationStatus", dynamicParam,
                       commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

    }

}
