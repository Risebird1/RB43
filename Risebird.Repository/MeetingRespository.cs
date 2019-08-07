using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class MeetingRespository : IMeetingRespository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public MeetingRespository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<Meeting>> GetAllInterviewMeetings()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<Meeting>(
                        con, "select im.*, IRF.id as irfIntId from InterviewMeetings im inner join IRF on im.IRFId = IRF.IRFId", null, commandType: Text);
                }
                //select *, IRF.id as irfIntId from InterviewMeetings im inner join IRF on im.IRFId = IRF.IRFId
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

        public async Task<IEnumerable<Meeting>> GetAllOpenInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_OpenInterviews", parameters,
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

        public async Task<IEnumerable<Meeting>> GetAllQCInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_QCInterviews", parameters,
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

         public async Task<IEnumerable<MeetingVM>> GetAllClientInterviews(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetClientInterview", parameters,
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

        public async Task<IEnumerable<Meeting>> GetAllAssignedInterview(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("emailID", emailID);
                    return await SqlMapper.QueryAsync<Meeting>(con, "usp_MyInterviews", parameters,
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


        private DynamicParameters SetStoredProcedureParameterForMeeting(MeetingVM entity)
        {
            var parameters = new DynamicParameters();
            if (entity.imid > 0)
                parameters.Add("@interviewId", entity.imid);

            parameters.Add("@IRFId", entity.IRFId); // Normal parametersmeters           
            parameters.Add("@interviewDate", entity.interviewDate);
            parameters.Add("@interviewSlot", entity.interviewSlot);
            parameters.Add("@interviewDuration", entity.interviewDuration);
            parameters.Add("@candidateName", entity.candidateName);
            parameters.Add("@candidateEmail", entity.candidateEmail);
            parameters.Add("@@hrRiseBirdAssinged", entity.risebirdHR);
            parameters.Add("@resumeLink", entity.resumeLink);
            parameters.Add("@zoomLink", entity.zoomLink);
            parameters.Add("@ClientHrContact", entity.ClientHrContact);
            parameters.Add("@comfirmationStatus", entity.confirmationStatus);
            parameters.Add("@interviewer", entity.interviewer);
            parameters.Add("@preferredLocation", entity.preferredLocation);
            parameters.Add("@staffingAgency", entity.staffingAgency);
            parameters.Add("@expectedPerHourRate", entity.expectedPerHourRate);
            parameters.Add("@joiningTime", entity.joiningTime);            
            return parameters;
        }
        public async Task<bool> AddMeeting(MeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForMeeting(meeting);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddMeeting", parameters,
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

        public async Task<MeetingVM> GetInterviewMeetingById(int id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@interviewId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryFirstAsync<MeetingVM>(con, "usp_GetMeetingById", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> UpdateMeeting(MeetingVM meeting)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForMeeting(meeting);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateMeeting", parameters,
                        commandType: CommandType.StoredProcedure);
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

        public string AssignYourself(int meetingid, string interviewerMail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "update [dbo].[InterviewMeetings] set interviewer=@value where id = @meetingid", new { value = interviewerMail, meetingid = meetingid }, commandType: Text);
                    return "Interviewer assigned successfully.";
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

        public async Task<InterviewComments> GetInterviewComments(int interviewId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@interviewId", interviewId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QuerySingleAsync<InterviewComments>(con, "usp_GetInterviewComments", parameters,
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

        public async Task<bool> UpdateComments(InterviewComments interviewComments)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = SetStoredProcedureParameterForComments(interviewComments);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewComments", parameters,
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

        public IEnumerable<TransSkill> GetSkillsbyIRFid(int id)
        {
            try
            {
                var query = "Select * from TransTechSkill where IRFId = @IRFId";
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
        public IEnumerable<TransSkillGeneral> GetSkillsGeneralbyIRFid(int id)
        {
            try
            {
                var query = "Select * from TransSkillGeneral where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSkillGeneral>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransJoinability> GetTransJoinability(int id)
        {
            try
            {
                var query = "Select * from TransJoinability where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransJoinability>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransSelectability> GetTransSelectability(int id)
        {
            try
            {
                var query = "Select * from TransSelectability where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSelectability>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<SkillComment> GetTechSkillComments(int id)
        {
            try
            {


                var query = "Select * from TechSkillComments where IRFId = @IRFId";
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
        public IEnumerable<Level> GetLevels()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Level>(
                        con, "select * from Level", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<Skill> GetSkills()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Skill>(
                        con, "select * from Skill", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddInterviewReport(InterviewReport reportDetails)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //check if the data is edited
                    InterviewComments interviewComments = new InterviewComments();
                    DynamicParameters parameters = new DynamicParameters();
                    DynamicParameters paramx = new DynamicParameters();
                    paramx.Add("@InterviewId", reportDetails.InterviewId);
                    paramx.Add("@confirmationStatus", reportDetails.confirmationStatus);
                    paramx.Add("@OverallScore", reportDetails.OverallScore);
                    paramx.Add("@Rating", reportDetails.Rating);
                    paramx.Add("@TechSkillComments", reportDetails.TechSkillComments);
                    paramx.Add("@SkillGeneralComments", reportDetails.SkillGeneralComments);
                    paramx.Add("@SelectabilityComments", reportDetails.SelectabilityComments);
                    paramx.Add("@JoinabilityComments", reportDetails.JoinabilityComments);

                    parameters.Add("@interviewId", reportDetails.InterviewId);
                    parameters.Add("@interviewStatus", reportDetails.InterviewStatus);
                    parameters.Add("@MeetingStatus", reportDetails.MeetingStatus);
                    parameters.Add("@interviewerComment", reportDetails.InterviewerComment);
                    parameters.Add("@audiolink",reportDetails.AudioLink);
                    parameters.Add("@videolink", reportDetails.VideoLink);

                    if (reportDetails.IsEdited)
                    {
                        var result=SqlMapper.Query<int>(con, "usp_UpdateInterviewReport", paramx,
                                            commandType: CommandType.StoredProcedure);
                       
                    }

                    else
                    {
                        //add to interviewReport table
                        string sqlInsertReport = "INSERT INTO InterviewReport (IRFId, InterviewId, CandidateName,confirmationStatus,OverallScore, Rating, TechSkillComments, SkillGeneralComments, SelectabilityComments, JoinabilityComments ) Values (@IRFId, @InterviewId, @CandidateName, @confirmationStatus, @OverallScore, @Rating, @TechSkillComments, @SkillGeneralComments, @SelectabilityComments, @JoinabilityComments);";
                        var affectedRows = con.ExecuteAsync(sqlInsertReport, new
                        {
                            IRFId = reportDetails.IRFId,
                            InterviewId = reportDetails.InterviewId,
                            CandidateName = reportDetails.CandidateName,
                            confirmationStatus = reportDetails.confirmationStatus,
                            OverallScore = reportDetails.OverallScore,
                            Rating= reportDetails.Rating,
                            TechSkillComments = reportDetails.TechSkillComments,
                            SkillGeneralComments = reportDetails.SkillGeneralComments,
                            SelectabilityComments = reportDetails.SelectabilityComments,
                            JoinabilityComments = reportDetails.JoinabilityComments,

                        }).Result;

                    }

                    if (reportDetails.TechSkillReportCollection!=null)
                    {
                        //update techskillreport table
                        foreach (var item in reportDetails.TechSkillReportCollection)
                        {
                            string sqlTechReport = "INSERT INTO TechSkillReport (InterviewId, RequiredLevelId ,SkillId, ActualLevelId, WeightageRequired,WeightageActual) Values (@InterviewId, @RequiredLevelId, @SkillId, @ActualLevelId, @WeightageRequired, @WeightageActual);";
                            var result1 = con.ExecuteAsync(sqlTechReport, new
                            {
                                InterviewId = reportDetails.InterviewId,
                                RequiredLevelId = item.RequiredLevelId,
                                SkillId = item.SkillId,
                                ActualLevelId = item.ActualLevelId,
                                WeightageRequired = item.WeightageRequired,
                                WeightageActual = item.WeightageActual
                            }).Result;
                        }

                    }
                    

                    if(reportDetails.SkillGeneralReportCollection!=null)
                    {
                        //update general skill report table
                        foreach (var item in reportDetails.SkillGeneralReportCollection)
                        {

                            string sqlSkillGeneralReport = "INSERT INTO SkillGeneralReport (InterviewId, RequiredLevelId ,Type, ActualLevelId, WeightageRequired,WeightageActual) Values (@InterviewId, @RequiredLevelId, @Type, @ActualLevelId, @WeightageRequired, @WeightageActual);";
                            var result2 = con.ExecuteAsync(sqlSkillGeneralReport, new
                            {
                                InterviewId = reportDetails.InterviewId,
                                RequiredLevelId = item.RequiredLevelId,
                                Type = item.Type,
                                ActualLevelId = item.ActualLevelId,
                                WeightageRequired = item.WeightageRequired,
                                WeightageActual = item.WeightageActual
                            }).Result;
                        }
                    }

                    if(reportDetails.JoinabilityReportCollection!=null)
                    {
                        //update Joinability Report table

                        foreach (var item in reportDetails.JoinabilityReportCollection)
                        {

                            string sqlJoinReport = "INSERT INTO JoinabilityReport (InterviewId, RequiredLevelId ,Type, ActualLevelId, WeightageRequired,WeightageActual) Values (@InterviewId, @RequiredLevelId, @Type, @ActualLevelId, @WeightageRequired, @WeightageActual);";
                            var result3 = con.Execute(sqlJoinReport, new
                            {
                                InterviewId = reportDetails.InterviewId,
                                RequiredLevelId = item.RequiredLevelId,
                                Type = item.Type,
                                ActualLevelId = item.ActualLevelId,
                                WeightageRequired = item.WeightageRequired,
                                WeightageActual = item.WeightageActual
                            });
                        }
                    }

                    if(reportDetails.SelectabilityReportCollection!=null)
                    {
                        //update Selectability Report table
                        foreach (var item in reportDetails.SelectabilityReportCollection)
                        {

                            string sqlSelectableReport = "INSERT INTO SelectabilityReport (InterviewId, RequiredLevelId ,Type, ActualLevelId, WeightageRequired,WeightageActual) Values (@InterviewId, @RequiredLevelId, @Type, @ActualLevelId, @WeightageRequired, @WeightageActual);";
                            var result3 = con.Execute(sqlSelectableReport, new
                            {
                                InterviewId = reportDetails.InterviewId,
                                RequiredLevelId = item.RequiredLevelId,
                                Type = item.Type,
                                ActualLevelId = item.ActualLevelId,
                                WeightageRequired = item.WeightageRequired,
                                WeightageActual = item.WeightageActual
                            });
                        }

                    }
                    
                    if(reportDetails.QuestionsCollection !=null)
                    {
                        //update questions discussed table
                        foreach (var item in reportDetails.QuestionsCollection)
                        {
                            string sqlJoinReport = "INSERT INTO QuestionsDiscussed (InterviewId, Comment ,RatingId, Timing) Values (@InterviewId, @Comment, @RatingId, @Timing);";
                            var result3 = con.Execute(sqlJoinReport, new
                            {
                                InterviewId = reportDetails.InterviewId,
                                Comment = item.Comment,
                                RatingId = item.RatingId,
                                Timing = item.Timing,

                            });

                        }
                    }

                    if (reportDetails.Proctoring != null)
                    {
                        //update protoring report table
                        string sqlProcReport = "INSERT INTO ProctoringReport Values (@InterviewId, @EyesDirection, @EyesReflection, @HeadDirection, @LipsOverhead, @LipsDarklight, @LipsHeadphone, @LatencyJitter, @DesktopSuspiciousApps, @DesktopScreenSharing, @VoiceAudioSplitter, @VoiceToneExpression, @VoicePersonalUnsatisfactory, @VoiceSuspicious, @LinkedInPhotoVerification);";
                        var result3 = con.Execute(sqlProcReport, new
                        {
                            InterviewId = reportDetails.InterviewId,
                            EyesDirection = reportDetails.Proctoring.EyesDirection,
                            EyesReflection = reportDetails.Proctoring.EyesReflection,
                            HeadDirection = reportDetails.Proctoring.HeadDirection,
                            LipsOverhead = reportDetails.Proctoring.LipsOverhead,
                            LipsDarklight = reportDetails.Proctoring.LipsDarklight,
                            LipsHeadphone = reportDetails.Proctoring.LipsHeadphone,
                            LatencyJitter = reportDetails.Proctoring.LatencyJitter,
                            DesktopSuspiciousApps = reportDetails.Proctoring.DesktopSuspiciousApps,
                            DesktopScreenSharing = reportDetails.Proctoring.DesktopScreenSharing,
                            VoiceAudioSplitter = reportDetails.Proctoring.VoiceAudioSplitter,
                            VoiceToneExpression = reportDetails.Proctoring.VoiceToneExpression,
                            VoicePersonalUnsatisfactory = reportDetails.Proctoring.VoicePersonalUnsatisfactory,
                            VoiceSuspicious = reportDetails.Proctoring.VoiceSuspicious,
                            LinkedInPhotoVerification = reportDetails.Proctoring.LinkedInPhotoVerification

                        });
                        
                    }

                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateInterviewComments", parameters,
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

        public IEnumerable<InterviewReport> GetInterviewReportById(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@InterviewId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<InterviewReport>(con, "usp_GetInterviewReportById", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<JoinabilityReport> GetJoinabilityReport(int id)
        {
            try
            {
                var query = "select JoinabilityReport.*, [Level].[Name] as RequiredLevel from JoinabilityReport inner join [Level] on JoinabilityReport.RequiredLevelId=[Level].id where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<JoinabilityReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<SelectabilityReport> GetSelectabilityReport(int id)
        {
            try
            {
                var query = "select SelectabilityReport.*, [Level].[Name] as RequiredLevel from SelectabilityReport inner join [Level] on SelectabilityReport.RequiredLevelId=[Level].id where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<SelectabilityReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<QuestionsDiscussed> GetQuestionsDiscussed(int id)
        {
            try
            {
                var query = "Select * from QuestionsDiscussed where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<QuestionsDiscussed>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TechSkillReport> GetTechSkillReport(int id)
        {
            try
            {
                var query = "select a.*, Skill.[Name] as SkillName from (select TechSkillReport.*, [Level].[Name] as RequiredLevel from TechSkillReport inner join [Level] on TechSkillReport.RequiredLevelId=[Level].id) as a inner join Skill on a.SkillId = Skill.id where InterviewId = @id";
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

        public IEnumerable<SkillGeneralReport> GetSkillGeneralReport(int id)
        {
            try
            {
                var query = "select SkillGeneralReport.*, [Level].[Name] as RequiredLevel from SkillGeneralReport inner join [Level] on SkillGeneralReport.RequiredLevelId=[Level].id where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<SkillGeneralReport>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public ProctoringReport GetProctoringReport(int id)
        {
            try
            {
                var query = "select * from ProctoringReport where InterviewId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<ProctoringReport>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }



        private DynamicParameters SetStoredProcedureParameterForComments(InterviewComments entity)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@interviewId", entity.InterviewId); // Normal parametersmeters 
            parameters.Add("@@interviewStatus", entity.InterviewStatus);
            parameters.Add("@@interviewerComment", entity.InterviewerComment);
            parameters.Add("@fraudCheckStatus", entity.FraudCheckStatus);
            parameters.Add("@hrComment", entity.HRComment);
            parameters.Add("@ClientInterviewStatus", entity.FinalInterviewMeetingStatus);
            parameters.Add("@qcStatus", entity.QCStatus);
            parameters.Add("@qcComment", entity.QCComment);
            parameters.Add("@trainingStatus", entity.TrainingStatus);
            parameters.Add("@trainerComment", entity.TrainerComment);
            parameters.Add("@audiolink", entity.AudioLink);
            parameters.Add("@videolink", entity.VideoLink);
            parameters.Add("@MeetingStatus", entity.MeetingStatus);
            parameters.Add("@OfferStatus", entity.OfferStatus);
            parameters.Add("@PreparationStatus", entity.PreparationStatus);
            parameters.Add("@JoiningStatus", entity.JoiningStatus);
            return parameters;
        }
    }
}
