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

        public InterviewerRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }



        #region Interviewer Profile Repository function

        public async Task<IEnumerable<InterviewerProfileVM>> GetInterviewers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<InterviewerProfileVM>(con, "[dbo].[usp_GetInterviewerDetails]", null,
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

        public async Task<bool> AddOrEditInterviewerProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {
                int ProfileId;

                DynamicParameters parameters = SetSPParametersForInterviewProfile(interviewerProfile);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //Just added to get Id this way
                    //var result = await SqlMapper.QueryAsync<int>(con, "usp_AddOrEditInterviewerProfile", parameters,
                    //   commandType: CommandType.StoredProcedure);
                    //int ProfileId = result.FirstOrDefault();

                    await SqlMapper.QueryAsync<int>(con, "usp_AddOrEditInterviewerProfile", parameters,
                       commandType: CommandType.StoredProcedure);

                    var ProfileList = SqlMapper.QueryAsync<InterviewerProfileVM>(
                        con, "select id, NickName, userId from InterviewerProfile orderby ", null, commandType: Text).Result.AsList();
                    ProfileId = ProfileList.Where(x => x.NickName == interviewerProfile.NickName).Last().id;//irfList.Find(x => x.IRFId == irfDetails.IRFId).id;
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
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerSkills", Params,
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
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerCertificate", Params,
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
                        await SqlMapper.QueryAsync<int>(con, "[dbo].[usp_AddInterviewerEducation]", Params,
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
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerProjects", Params,
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
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerAvailability", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerAccDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@AccountName", item.AccountNumber);
                    Params.Add("@AccountNumber", item.AccountNumber);
                    Params.Add("@IFSCCode", item.IFSCCode);
                    Params.Add("@UPIId", item.UPIId);
                    Params.Add("@PAN", item.PAN);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerAccDetails", Params,
                       commandType: CommandType.StoredProcedure);

                    }
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
                        con, "select EmailId from [dbo].[UserList] where Id != @UserId", parameters, commandType: Text);
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
        public async Task<IEnumerable<TransInterviewerAccDetails>> GetAccDetailsList(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //Id, InterviewerProfileId,  AccountHolderName, AccountNumber, IFSCCode, Location, UPIId
                    return await SqlMapper.QueryAsync<TransInterviewerAccDetails>(
                        con, "select * from [dbo].[TransInterviewerAccDetails] where InterviewerProfileId = @ProfileId",
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
        #endregion

        #region Interviewer My Profile Repository function
        public async Task<bool> AddInterviewerMyProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {
                int ProfileId;

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
                parameters.Add("@v_ProfileId", SqlDbType.Int, direction: ParameterDirection.Output);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_MyProfile", parameters,
                       commandType: CommandType.StoredProcedure);

                    var ProfileList = SqlMapper.QueryAsync<InterviewerProfileVM>(
                        con, "select id, NickName, userId from InterviewerProfile orderby ", null, commandType: Text).Result.AsList();
                    ProfileId = ProfileList.Where(x => x.UserId == interviewerProfile.UserId).Last().id;//irfList.Find(x => x.IRFId == irfDetails.IRFId).id;
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
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerAvailability", Params,
                       commandType: CommandType.StoredProcedure);

                    }
                }

                foreach (var item in interviewerProfile.InterviewerAccDetails)
                {
                    DynamicParameters Params = new DynamicParameters();
                    Params.Add("@InterviewerProfileId", ProfileId);
                    Params.Add("@AccountName", item.AccountNumber);
                    Params.Add("@AccountNumber", item.AccountNumber);
                    Params.Add("@IFSCCode", item.IFSCCode);
                    Params.Add("@UPIId", item.UPIId);
                    Params.Add("@PAN", item.PAN);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerAccDetails", Params,
                       commandType: CommandType.StoredProcedure);

                    }
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
                        con, "update [dbo].[InterviewMeetings] set interviewer=@value where id = @meetingid", new { value = interviewerMail, meetingid = meetingid }, commandType: Text);
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
                        con, "usp_UnAssignInterviewer", parameters, commandType: CommandType.StoredProcedure);
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
                var query = "select interviewDate, candidateEmail from InterviewMeetings where id = @meetingid";
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
                var query = "Select InterviewerSkillId as IRFId, JDTiltle from InterviewerSkillsChannel where id = @id";
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
        public IEnumerable<InterviewReport> GetInterviewerReportById(int id, int irfId = 0)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewId", id);//@IrfId
                dynamicParameters.Add("@IrfId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<InterviewReport>(con, "usp_GetInterviewerReportById", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
        public IEnumerable<InterviewReport> GetInterviewerRecommendationbyinterviewid(int id)
        {
            try
            {
                var query = "Select * from InterviewerRecommendation where Interviewid= @Interviewid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Interviewid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<InterviewReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                    paramx.Add("@InterviewId", reportDetails.InterviewId);
                    paramx.Add("@SkillQuality", reportDetails.SkillQuality);
                    paramx.Add("@OverallScore", reportDetails.OverallScore);
                    paramx.Add("@Rating", reportDetails.Rating);
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

                    parametersRecomm.Add("@interviewId", reportDetails.InterviewId);
                    parametersRecomm.Add("@CompanySize", reportDetails.CompanySize);
                    parametersRecomm.Add("@JobFunction", reportDetails.JobFunction);
                    parametersRecomm.Add("@Industry", reportDetails.Industry);
                    parametersRecomm.Add("@CompanyHeadquarters", reportDetails.CompanyHeadquarters);
                    parametersRecomm.Add("@CompanyType", reportDetails.CompanyType);
                    parametersRecomm.Add("@CompanyStage", reportDetails.CompanyStage);
                    parametersRecomm.Add("@SkillCategory", reportDetails.SkillCategory);
                    parametersRecomm.Add("@Yearsofexperience", reportDetails.Yearsofexperience);
                    parametersRecomm.Add("@Senioritylevel", reportDetails.Senioritylevel);
                    parametersRecomm.Add("@SkillNature", reportDetails.SkillNature);
                    parametersRecomm.Add("@Domain", reportDetails.Domain);
                    parametersRecomm.Add("@SkillIP", reportDetails.SkillIP);
                    parametersRecomm.Add("@PerHourRateIndia", reportDetails.PerHourRateIndia);

                    if (reportDetails.IsEdited)
                    {
                        var result = SqlMapper.Query<int>(con, "usp_UpdateInterviewerReport", paramx,
                                            commandType: CommandType.StoredProcedure);

                    }
                    else
                    {
                        //add to interviewReport table
                        string sqlInsertReport = "INSERT INTO InterviewerReport (InterviewerSkillId, InterviewId, CandidateName,OverallScore, Rating, TechSkillComments,SkillQuality,CertificationQuality, EducationQuality, EmployersQuality,CareerProgression,ProjectsQuality,OnlinePresence,BehavioralScore,InterviwerCommunication,InterviewExperience ) Values   (@InterviewerSkillId, @InterviewId, @CandidateName,@OverallScore, @Rating, @TechSkillComments, @SkillQuality,   @CertificationQuality, @EducationQuality, @EmployersQuality,@CareerProgression,@ProjectsQuality,@OnlinePresence,@BehavioralScore,@InterviwerCommunication,@InterviewExperience);";
                        var affectedRows = con.ExecuteAsync(sqlInsertReport, new
                        {
                            InterviewerSkillId = reportDetails.IRFId,
                            InterviewId = reportDetails.InterviewId,
                            CandidateName = reportDetails.CandidateName,
                            EducationQuality = reportDetails.EducationQuality,
                            OverallScore = reportDetails.OverallScore,
                            Rating = reportDetails.Rating,
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
                        }).Result;

                    }

                    DataTable techReportDt = new DataTable();
                    techReportDt.Columns.Add("InterviewId", typeof(int));
                    techReportDt.Columns.Add("RequiredLevelId", typeof(int));
                    techReportDt.Columns.Add("SkillId", typeof(int));
                    techReportDt.Columns.Add("ActualLevelId", typeof(int));
                    techReportDt.Columns.Add("WeightageRequired", typeof(int));
                    techReportDt.Columns.Add("WeightageActual", typeof(int));


                    foreach (var item in reportDetails.TechSkillReportCollection)
                    {
                        techReportDt.Rows.Add(reportDetails.InterviewId, item.RequiredLevelId, item.SkillId, item.ActualLevelId, item.WeightageRequired, item.WeightageActual);
                    }

                    SqlCommand cmdTechReport = new SqlCommand("usp_TechSkillReportBulkInsert", con);
                    cmdTechReport.CommandType = CommandType.StoredProcedure;
                    cmdTechReport.Parameters.Add(new SqlParameter("@TechSkillReportBulkData", SqlDbType.Structured));
                    cmdTechReport.Parameters["@TechSkillReportBulkData"].Value = techReportDt;
                    con.Open();
                    if (cmdTechReport.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }


                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewerReportComments", parameters,
                            commandType: CommandType.StoredProcedure);
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewerReportRecommendation", parametersRecomm,
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
    }
}
