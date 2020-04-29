using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using static System.Data.CommandType;
using System.Threading.Tasks;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;

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
                        await SqlMapper.QueryAsync<int>(con, "TransInterviewerEducation", Params,
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

        public async Task<bool> AddEmployerProfile(InterviewerProfessional item)
        {
            try
            {
                DynamicParameters Params = SetSPParametersForEmployer(item);


                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddInterviewerProfessional", Params,
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
                    await SqlMapper.QueryAsync<int>(con, "Delete from TransInterviewerProfessional where id = @Id", parameters,
                   commandType: Text);

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
                        con, "select EmailId from [dbo].[InterviewerProfile] where UserId != @UserId", parameters, commandType: Text);
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
                parameters.Add("@userid", userid);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<InterviewerProfileVM>(con, "usp_GetInterviewerProfiletByUserId",
                        parameters, commandType: CommandType.StoredProcedure);
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
                    IEnumerable<InterviewerProfessional> result = await SqlMapper.QueryAsync<InterviewerProfessional>(con, "SELECT [Id], [InterviewerProfileId], [CompanyName],[Type], [CompanyStage], [Size], [JobFunctionId], [IndustryId]," +
                                                "[Headquarters], [Role], [Domain], [SeniorityLevel], [ExperienceId], [WorkNature], [PrimaryResponsibility]," +
                                                "[CompanyLinkedinPage], [CompanyWebsite] FROM[dbo].[TransInterviewerProfessional] where Id = @id;",
                        parameters, commandType: Text);
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
        public async Task<IEnumerable<TransInterviewerSkill>> GetSkills(int ProfileId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Profileid", ProfileId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<TransInterviewerSkill>(
                        con, "select Id, InterviewerProfileId,  SkillId, Level, SkillName from [dbo].[TransInterviewerSkill] where InterviewerProfileId = @ProfileId",
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
                    return await SqlMapper.QueryAsync<TransInterviewerCertificate>(
                        con, "select Id, InterviewerProfileId,  CertificateID, CertificateName from [dbo].[TransInterviewerCertificate] where InterviewerProfileId = @ProfileId",
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
                    return await SqlMapper.QueryAsync<TransInterviewerEducation>(
                        con, "select Id, InterviewerProfileId,  Graduation, CollegeType, Specialization, Score from [dbo].[TransInterviewerEducation] where InterviewerProfileId = @ProfileId",
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
                    return await SqlMapper.QueryAsync<TransInterviewerProjects>(
                        con, "select Id, InterviewerProfileId,  ProjectName, ClientName, StartDate, EndDate, Role, ProjectDescriptions from [dbo].[TransInterviewerProjects] where InterviewerProfileId = @ProfileId",
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
                    return await SqlMapper.QueryAsync<TransInterviewerAvailability>(
                        con, "select Id, InterviewerProfileId,  DayTimeSlot, DaySlot, StartTime, EndTime from [dbo].[TransInterviewerAvailability] where InterviewerProfileId = @ProfileId",
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
                    return await SqlMapper.QueryAsync<TransInterviewerAccDetails>(
                        con, "select Id, InterviewerProfileId,  AccountHolderName, AccountNumber, IFSCCode, Location, UPIId from [dbo].[TransInterviewerAccDetails] where InterviewerProfileId = @ProfileId",
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

        public async Task<List<InterviewerProfessional>> GetEmployerListById(int UserId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@UserId", UserId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    IEnumerable<InterviewerProfessional> result = await SqlMapper.QueryAsync<InterviewerProfessional>(con, "SELECT [TIP].[Id], [InterviewerProfileId], [CompanyName],[Type], [CompanyStage], [Size], [JobFunctionId], [IndustryId]," +
                                                "[Headquarters], [Role], [Domain], [SeniorityLevel], [ExperienceId], [WorkNature], [PrimaryResponsibility]," +
                                                "[CompanyLinkedinPage], [CompanyWebsite] from [TransInterviewerProfessional] TIP join InterviewerProfile IP on TIP.InterviewerProfileId = IP.Id where IP.UserId = @UserId; ",
                        parameters, commandType: Text);
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
    }
}
