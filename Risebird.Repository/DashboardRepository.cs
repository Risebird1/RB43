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


namespace Risebird.Repository
{
    public class DashboardRepository : IDashboarRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public DashboardRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IEnumerable<Meeting>> GetDateWiseMeetings(DateTime date, string Emailid)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@dateValue", date);
                parameters.Add("@Emailid", Emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Meeting>(con, "usp_GetDateWiseMeetings", parameters,
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

        public async Task<IEnumerable<DailyInterviewSlot>> GetDateWiseMeetingsSlots(DateTime date, string Emailid)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@dateValue", date);
                parameters.Add("@Emailid", Emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<DailyInterviewSlot>(con, "usp_GetDateWiseMeetingsSlots", parameters,
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

        public async Task<DashboardInterviewerCount> GetInterviewerCount()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<DashboardInterviewerCount>(con, "usp_GetInterviewerCount",
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

        public async Task<IEnumerable<IRF>> GetIRFProgress(DateTime date, string Emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@dateValue", date);
                    parameters.Add("@Emailid", Emailid);
                    var result = await SqlMapper.QueryAsync<IRF>(con, "[usp_GetIRFProgress]", parameters,
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

        public IEnumerable<IrfProgress> GetProgressOptions()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    return SqlMapper.Query<IrfProgress>(con, "usp_GetProgressOptions", commandType: CommandType.StoredProcedure);

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

        public async Task<bool> UpdateIRFProgress(IList<IRF> irfProgressList)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    //DataTable progressDt = new DataTable();
                    //progressDt.Columns.Add("id", typeof(int));
                    //progressDt.Columns.Add("Comment", typeof(string));
                    //progressDt.Columns.Add("idProgress", typeof(int));

                    DataTable activityprogressDt = new DataTable();
                    activityprogressDt.Columns.Add("id", typeof(int));
                    activityprogressDt.Columns.Add("Comment", typeof(string));
                    activityprogressDt.Columns.Add("idProgress", typeof(int));
                    activityprogressDt.Columns.Add("Userid", typeof(string));
                    activityprogressDt.Columns.Add("Status", typeof(string));

                    //foreach (var item in irfProgressList)
                    //{
                    //    progressDt.Rows.Add(item.id, item.Comment, item.idProgress);
                    //}
                    foreach (var item in irfProgressList)
                    {
                        activityprogressDt.Rows.Add(item.id, item.Comment, item.idProgress, item.AssignTo, item.AssignStatus);
                    }

                    SqlCommand cmdProgress = new SqlCommand("usp_IRFProgressStatusBulkUpdate", con);
                    cmdProgress.CommandType = CommandType.StoredProcedure;
                    cmdProgress.Parameters.Add(new SqlParameter("@ProgressBulkData", SqlDbType.Structured));
                    cmdProgress.Parameters["@ProgressBulkData"].Value = activityprogressDt;
                    con.Open();
                    if (cmdProgress.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
                    //Create Sp and Update/Insert value into db
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

        public async Task<MeetingResultCount> GetMeetingResultCount(string Emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", Emailid);
                    var result = await SqlMapper.QueryAsync<MeetingResultCount>(con, "usp_GetMeetingsResults", parameters,
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

        public async Task<CandidatesCount> GetCandidatesCount(string Emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Emailid", Emailid);
                    var result = await SqlMapper.QueryAsync<CandidatesCount>(con, "usp_GetCandidatesCount", parameters,
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

        public async Task<IEnumerable<Meeting>> GetDateWiseClientMeetings(DateTime date, string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@dateValue", date);
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Meeting>(con, "usp_GetDateWiseClientMeetings", parameters,
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

        public async Task<IRFFunnel> GetClientFunnelData(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                parameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<IRFFunnel>(con, "usp_GetClientFunnelData", parameters,
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

        public async Task<IEnumerable<ClientDailySlotsStatus>> GetDateWiseClientMeetingsSlots(DateTime date, string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@dateValue", date);
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<ClientDailySlotsStatus>(con, "usp_GetDateWiseClientMeetingsSlots", parameters,
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

        public async Task<CandidatesCount> GetClientCandidatesCount(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<CandidatesCount>(con, "usp_GetClientCandidatesCount", parameters,
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

        public async Task<IEnumerable<CandidateStatus>> GetClientCandidatesCurrentStatus(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<CandidateStatus>(con, "usp_GetClientCandidatesCurrentStatus", parameters,
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


        public async Task<MeetingResultCount> GetClientMeetingsResults(string emailId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingResultCount>(con, "usp_GetClientMeetingsResults", parameters,
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
        public IRF FetchActivityDetails(int id, int idprogess)
        {
            try
            {
                var query = "Select ul.EmailId as AssignTo,IR.Status AssignStatus,IR.Comment from IRFActivityStatus IR Left join UserList ul on IR.Userid=ul.id where IR.IRFID=@id and IR.idProgress=@idprogess";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@idprogess", idprogess);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<IRF>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<ChannelManagementVM>> FetchChannelRequests()
        {
            try
            {
                var query = "Select cm.id,cm.Client,cm.Status,cm.IRFIDs,cm.PrimarySkill,cm.CreatedDate,cm.Title,count(cn.id) as InterviewerCount,cm.RequesterEmailid from ChannelManagement cm left join ChannelMInterviewer cn on cm.id=cn.channelid group by cm.id,cm.Client,cm.Status,cm.IRFIDs,cm.PrimarySkill,cm.CreatedDate,cm.Title,cm.RequesterEmailid";
                var dynamicParameters = new DynamicParameters();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<ChannelManagementVM>(query, null);
                    return (List<ChannelManagementVM>)result;
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
        public async Task<ChannelVM> FetchChannelRequestsByid(int id)
        {
            try
            {
                var query = "Select * from ChannelManagement where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<ChannelVM>(query, dynamicParameters);
                    return result.FirstOrDefault();
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
        public async Task<string> AddChannelRequest(ChannelVM request)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    if (!request.isEditable)
                    {
                        var CreatedDate = DateTime.Now;
                        var Status = "Open";
                        string sqlInsertReport = "INSERT INTO ChannelManagement (Client,Requester,PrimarySkill,EndClient,Location,JdLink,CreatedBy,CreatedDate,Status,RequesterEmailid,RequesterMobile,Title,SeniorityLevel,TurnAroundTimeindays,Comments,NumberofInterviews) Values (@Client,@Requester,@PrimarySkill,@EndClient,@Location,@JdLink,@CreatedBy,@CreatedDate,@Status,@RequesterEmailid,@RequesterMobile,@Title,@SeniorityLevel,@TurnAroundTimeindays,@Comments,@NumberofInterviews);";
                        var affectedRows = con.ExecuteAsync(sqlInsertReport, new
                        {
                            request.Client,
                            request.Requester,
                            request.PrimarySkill,
                            request.EndClient,
                            request.Location,
                            request.JdLink,
                            request.CreatedBy,
                            CreatedDate,
                            Status,
                            request.RequesterEmailid,
                            request.RequesterMobile,
                            request.Title,
                            request.SeniorityLevel,
                            request.TurnAroundTimeindays,
                            request.Comments,
                            request.NumberofInterviews
                        }).Result;
                    }
                    else
                    {
                        var updatequery = "Update ChannelManagement set Client=@Client,Requester=@Requester,PrimarySkill=@PrimarySkill,EndClient=@EndClient,Location=@Location,ExpectedVolume=@ExpectedVolume,JdLink=@JdLink,WeekendDrive=@WeekendDrive,POGenerated=@POGenerated,IPSCreated=@IPSCreated,CalendlyLinkCreated=@CalendlyLinkCreated,CandidateSourcing=@CandidateSourcing,AdditionalComment=@AdditionalComment,HRCoordinator=@HRCoordinator,ProgramManager=@ProgramManager,IRFID=@IRFID,IRFIDs=@IRFIDs,Priority=@Priority,RequesterEmailid=@RequesterEmailid,RequesterMobile=@RequesterMobile,Title=@Title,SeniorityLevel=@SeniorityLevel,TurnAroundTimeindays=@TurnAroundTimeindays,Comments=@Comments,NumberofInterviews=@NumberofInterviews,Origin=@Origin,EngagementType=@EngagementType,Stage=@Stage,ExpectedPrice=@ExpectedPrice,Expectedrevenue=@Expectedrevenue,PaymentStatus=@PaymentStatus,ConfirmationStatus=@ConfirmationStatus where id=@id;";
                        var updatedynamicParameters = new DynamicParameters();
                        updatedynamicParameters.Add("@id", request.id);
                        updatedynamicParameters.Add("@Client", request.Client);
                        updatedynamicParameters.Add("@Requester", request.Requester);
                        updatedynamicParameters.Add("@PrimarySkill", request.PrimarySkill);
                        updatedynamicParameters.Add("@EndClient", request.EndClient);
                        updatedynamicParameters.Add("@Location", request.Location);
                        updatedynamicParameters.Add("@ExpectedVolume", request.ExpectedVolume);
                        updatedynamicParameters.Add("@JdLink", request.JdLink);
                        updatedynamicParameters.Add("@WeekendDrive", request.WeekendDrive);
                        updatedynamicParameters.Add("@POGenerated", request.POGenerated);
                        updatedynamicParameters.Add("@IPSCreated", request.IPSCreated);
                        updatedynamicParameters.Add("@CalendlyLinkCreated", request.CalendlyLinkCreated);
                        updatedynamicParameters.Add("@CandidateSourcing", request.CandidateSourcing);
                        updatedynamicParameters.Add("@AdditionalComment", request.AdditionalComment);
                        updatedynamicParameters.Add("@HRCoordinator", request.HRCoordinator);
                        updatedynamicParameters.Add("@ProgramManager", request.ProgramManager);
                        updatedynamicParameters.Add("@IRFID", request.IRFID);
                        updatedynamicParameters.Add("@IRFIDs", request.IRFIDs);
                        updatedynamicParameters.Add("@Priority", request.Priority);
                        updatedynamicParameters.Add("@RequesterEmailid", request.RequesterEmailid);
                        updatedynamicParameters.Add("@RequesterMobile", request.RequesterMobile);
                        updatedynamicParameters.Add("@Title", request.Title);
                        updatedynamicParameters.Add("@SeniorityLevel", request.SeniorityLevel);
                        updatedynamicParameters.Add("@TurnAroundTimeindays", request.TurnAroundTimeindays);
                        updatedynamicParameters.Add("@Comments", request.Comments);
                        updatedynamicParameters.Add("@NumberofInterviews", request.NumberofInterviews);
                        updatedynamicParameters.Add("@Origin", request.Origin);
                        updatedynamicParameters.Add("@EngagementType", request.EngagementType);
                        updatedynamicParameters.Add("@Stage", request.Stage);
                        updatedynamicParameters.Add("@ExpectedPrice", request.ExpectedPrice);
                        updatedynamicParameters.Add("@Expectedrevenue", request.Expectedrevenue);
                        updatedynamicParameters.Add("@PaymentStatus", request.PaymentStatus);
                        updatedynamicParameters.Add("@ConfirmationStatus", request.ConfirmationStatus);
                        var results = await con.QueryAsync(updatequery, updatedynamicParameters);
                    }
                    return "Success";
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<List<string>> FetchAdminandHRList()
        {
            try
            {
                var query = "Select Emailid from UserList where idProfile in(1,2)";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<string>(query, null);
                    return (List<string>)result;
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
        public async Task<string> AddChannelInterviewer(ChannelInterviewerVM request)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var TrainingTime = request.TrainingTime.ToString();
                    var CallTime = request.CallTime.ToString();
                    if (!request.isEditable)
                    {
                        var CreatedBy = 0;
                        string query = "Select id from UserList where Emailid=@Emailid";
                        var dynamicParameters = new DynamicParameters();
                        dynamicParameters.Add("@Emailid", request.CreatedBy);
                        var result = await con.QueryAsync<int>(query, dynamicParameters);
                        CreatedBy = result.FirstOrDefault();
                        string sqlInsertReport = "INSERT INTO ChannelMInterviewer (ProcessedDate,Name,EmailAddress,Mobile,CVLink,CodedCVLink,PrimarySKill,Role,Priority,Comment,Approved,DMCallAvailability,AddedoverPortal,AddedoverSKillChannel,QC,Emergency,Training,AdvancePayment,CallAttended,Channelid,CreatedBy,Trainingmailsend,PerInterviewrate,TrainingTime,CallTime) Values (GETDATE(),@Name,@EmailAddress,@Mobile,@CVLink,@CodedCVLink,@PrimarySKill,@Role,@Priority,@Comment,@Approved,@DMCallAvailability,@AddedoverPortal,@AddedoverSKillChannel,@QC,@Emergency,@Training,@AdvancePayment,@CallAttended,@Channelid,@CreatedBy,@Trainingmailsend,@PerInterviewrate,@TrainingTime,@CallTime);";
                        var insertdynamicParameters = new DynamicParameters();
                        insertdynamicParameters.Add("@Name", request.Name);
                        insertdynamicParameters.Add("@EmailAddress", request.EmailAddress);
                        insertdynamicParameters.Add("@Mobile", request.Mobile);
                        insertdynamicParameters.Add("@CVLink", request.CVLink);
                        insertdynamicParameters.Add("@CodedCVLink", request.CodedCVLink);
                        insertdynamicParameters.Add("@PrimarySKill", request.PrimarySKill);
                        insertdynamicParameters.Add("@Role", request.Role);
                        insertdynamicParameters.Add("@Priority", request.Priority);
                        insertdynamicParameters.Add("@Comment", request.Comment);
                        insertdynamicParameters.Add("@Approved", request.Approved);
                        insertdynamicParameters.Add("@DMCallAvailability", request.DMCallAvailability);
                        insertdynamicParameters.Add("@AddedoverPortal", request.AddedoverPortal);
                        insertdynamicParameters.Add("@AddedoverSKillChannel", request.AddedoverSKillChannel);
                        insertdynamicParameters.Add("@QC", request.QC);
                        insertdynamicParameters.Add("@Emergency", request.Emergency);
                        insertdynamicParameters.Add("@Training", request.Training);
                        insertdynamicParameters.Add("@AdvancePayment", request.AdvancePayment);
                        insertdynamicParameters.Add("@CallAttended", request.CallAttended);
                        insertdynamicParameters.Add("@Channelid", request.Channelid);
                        insertdynamicParameters.Add("@CreatedBy", CreatedBy);
                        insertdynamicParameters.Add("@Trainingmailsend", request.Trainingmailsend);
                        insertdynamicParameters.Add("@PerInterviewrate", request.PerInterviewrate);
                        insertdynamicParameters.Add("@TrainingTime", TrainingTime);
                        insertdynamicParameters.Add("@CallTime", CallTime);
                        var results = await con.QueryAsync(sqlInsertReport, insertdynamicParameters);
                    }
                    else
                    {
                        var updatequery = "Update ChannelMInterviewer set Name = @Name, EmailAddress = @EmailAddress, Mobile = @Mobile, CVLink = @CVLink, CodedCVLink = @CodedCVLink, PrimarySKill = @PrimarySKill, Role = @Role, Priority = @Priority, Comment = @Comment, Approved = @Approved, DMCallAvailability = @DMCallAvailability, AddedoverPortal = @AddedoverPortal, AddedoverSKillChannel = @AddedoverSKillChannel, QC = @QC, Emergency = @Emergency, Training = @Training, AdvancePayment = @AdvancePayment, CallAttended = @CallAttended, Trainingmailsend = @Trainingmailsend, PerInterviewrate = @PerInterviewrate, TrainingTime = @TrainingTime, CallTime = @CallTime where id = @id;";
                        var updatedynamicParameters = new DynamicParameters();
                        updatedynamicParameters.Add("@id", request.id);
                        updatedynamicParameters.Add("@Name", request.Name);
                        updatedynamicParameters.Add("@EmailAddress", request.EmailAddress);
                        updatedynamicParameters.Add("@Mobile", request.Mobile);
                        updatedynamicParameters.Add("@CVLink", request.CVLink);
                        updatedynamicParameters.Add("@CodedCVLink", request.CodedCVLink);
                        updatedynamicParameters.Add("@PrimarySKill", request.PrimarySKill);
                        updatedynamicParameters.Add("@Role", request.Role);
                        updatedynamicParameters.Add("@Priority", request.Priority);
                        updatedynamicParameters.Add("@Comment", request.Comment);
                        updatedynamicParameters.Add("@Approved", request.Approved);
                        updatedynamicParameters.Add("@DMCallAvailability", request.DMCallAvailability);
                        updatedynamicParameters.Add("@AddedoverPortal", request.AddedoverPortal);
                        updatedynamicParameters.Add("@AddedoverSKillChannel", request.AddedoverSKillChannel);
                        updatedynamicParameters.Add("@QC", request.QC);
                        updatedynamicParameters.Add("@Emergency", request.Emergency);
                        updatedynamicParameters.Add("@Training", request.Training);
                        updatedynamicParameters.Add("@AdvancePayment", request.AdvancePayment);
                        updatedynamicParameters.Add("@CallAttended", request.CallAttended);
                        updatedynamicParameters.Add("@Trainingmailsend", request.Trainingmailsend);
                        updatedynamicParameters.Add("@PerInterviewrate", request.PerInterviewrate);
                        updatedynamicParameters.Add("@TrainingTime", TrainingTime);
                        updatedynamicParameters.Add("@CallTime", CallTime);
                        var results = await con.QueryAsync(updatequery, updatedynamicParameters);
                    }
                    return "Success";
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<List<ChannelInterviewerVM>> FetchInterviewerBychannelid(int id)
        {
            try
            {
                var query = "Select cm.*,ul.Emailid as CreatedBy  from ChannelMInterviewer cm join UserList ul on ul.id=cm.CreatedBy where Channelid=@channelid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@channelid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<ChannelInterviewerVM>(query, dynamicParameters);
                    return (List<ChannelInterviewerVM>)result;
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
        public async Task<string> DeleteChannelInterviewerByid(int id, int channelid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "Delete from ChannelMInterviewer where id=@id and Channelid=@Channelid";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@id", id);
                    dynamicParameters.Add("@Channelid", channelid);
                    var result = await con.QueryAsync(query, dynamicParameters);
                    return "Success";
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<ChannelInterviewerVM> FecthInterviewerById(int id)
        {
            try
            {
                var query = "Select * from ChannelMInterviewer where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<ChannelInterviewerVM>(query, dynamicParameters);
                    return (ChannelInterviewerVM)result.FirstOrDefault();
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

        public async Task<NotUpdatedMeetings> GetNotUpdatedMeetingsCount(string Emailid)
        {
            try
            {
                string query = string.Empty;
                DateTime currentdate = DateTime.Now;
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@currentdate", currentdate);
                dp.Add("@Emailid", Emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<NotUpdatedMeetings>(con, "usp_GetNotUpdatedMeetingsCount", dp,
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
        public async Task<List<string>> FetchHRList(string searchterm)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@term", searchterm);
                string query = "Select Emailid from  Userlist where Emailid like @term+'%' and idProfile=2";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<string>(query, dynamicParameters);
                    return (List<string>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool AddChannelApprovedInterviewer(IEnumerable<ChannelApprovedInterviewer> model, int channelid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    foreach (var item in model)
                    {
                        string sqlAddInterviewer = "INSERT INTO TransChannelInterviewerList (UserId, Channelid, InterviewerProfileId,status) Values (@UserId, @Channelid, @InterviewerProfileId,@status);";
                        var result1 = con.ExecuteAsync(sqlAddInterviewer, new
                        {
                            item.UserId,
                            Channelid = channelid,
                            item.InterviewerProfileId,
                            item.status

                        }).Result;
                    }
                    return true;

                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<ChannelApprovedInterviewer>> GetChannelApprovedInterviewer(int id)
        {

            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Channelid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<ChannelApprovedInterviewer>(con, "usp_GetChannelApprovedInterviewer", dynamicParameters,
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
        public IEnumerable<ChannelApprovedInterviewer> GetChannelInterviewers(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Channelid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<ChannelApprovedInterviewer>(con, "usp_GetChannelInterviewersList", parameters,
                        commandType: CommandType.StoredProcedure);
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
        public async Task<bool> DeleteChannelInterviewer(int id)
        {
            try
            {
                var query = "Delete from TransChannelInterviewerList where Id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
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
        public async Task<bool> ChannelInterviewerAction(int id, string action)
        {
            try
            {
                var query = "Update TransChannelInterviewerList set status=@Action where Id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@Action", action);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
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
        public async Task<List<MeetingVM>> FetchRibbonDetailsValues(string actionstatus, string emailid, int actiontype, string profileName)
        {
            try
            {
                string query = string.Empty;
                if (actionstatus == "InterviewCount")
                {
                    // query = "select * from InterviewMeetings im  where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<7 ";
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1 and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1";
                        }
                    }
                    else
                    {
                        query = "select distinct im.*,irf.*,cl.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1 and ul.EmailId=@emailID union select distinct im.*,irf.*,cl.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and interviewer is null and DATEDIFF(DAY, interviewDate, GETDATE())<1 and ul.EmailId=@emailID";
                    }
                }
                else if (actionstatus == "MeetingStatus")
                {
                    //query = "select * from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewer is not null and ic.MeetingStatus is null and DATEDIFF(DAY, interviewDate, GETDATE())<7";
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, GETDATE())<7";
                        }
                    }
                    else
                    {
                        query = "select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId= im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID union select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId= im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.Emailid =@emailID";
                    }
                }
                //else if (actionstatus == "QcPending")
                //{
                //    // query = "select * from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and interviewer is not null and ic.MeetingStatus=4 and ic.QCStatus is null and DATEDIFF(DAY, interviewDate, GETDATE())<7";
                //    if (actiontype == 0)
                //    {
                //        if (profileName == "SeniorHR")
                //        {
                //            query = "select distinct * from InterviewMeetings im inner join InterviewComments ic on ic.InterviewId= im.id join IRF on irf.IRFId = im.IRFId join Client cl on cl.id=irf.ClientId join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join QCFeedbackReport qc on qc.InterviewId = im.id where confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4 ) and ic.MeetingStatus=4 and (qc.QCStatus = 0 or qc.QCStatus is null or qc.QCStatus=2) and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid";
                //        }
                //        else
                //        {
                //            query = "select distinct * from InterviewMeetings im inner join InterviewComments ic on ic.InterviewId= im.id join IRF on irf.IRFId = im.IRFId join Client cl on cl.id=irf.ClientId left join QCFeedbackReport qc on qc.InterviewId = im.id where confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4 ) and ic.MeetingStatus=4 and (qc.QCStatus = 0 or qc.QCStatus is null or qc.QCStatus=2) and DATEDIFF(DAY, interviewDate, GETDATE())<7";
                //        }
                //    }
                //    else
                //    {
                //        query = "select distinct im.*,irf.*,ic.*, cl.[Name] from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on cl.id=irf.ClientId inner join InterviewComments ic on ic.InterviewId= im.id left join QCFeedbackReport qc on qc.InterviewId = im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4 ) and ic.MeetingStatus=4 and (qc.QCStatus = 0 or qc.QCStatus is null or qc.QCStatus=2) and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID union select distinct im.*,irf.*,ic.*, cl.[Name] from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on cl.id=irf.ClientId inner join InterviewComments ic on ic.InterviewId= im.id left join QCFeedbackReport qc on qc.InterviewId = im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and (ic.InterviewStatus=2 or ic.InterviewStatus=4 ) and ic.MeetingStatus=4 and (qc.QCStatus = 0 or qc.QCStatus is null or qc.QCStatus=2) and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID";
                //    }
                //}
                else if (actionstatus == "UnAssignedCoordianter")
                {
                    if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and isAutoCancelled=1 and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid";
                    }
                    else
                    {
                        query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and isAutoCancelled=1 and DATEDIFF(DAY, interviewDate, GETDATE())<7";
                    }
                }
                else if (actionstatus == "ZoomLink")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "select  distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "select  distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='')";
                        }
                    }
                    else
                    {
                        query = "select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID union select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID";
                    }
                }
                else if (actionstatus == "UnAttendedZoomLink")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined where im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate())) and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined where im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,15,im.Interviewdate)>=dateadd(minute,330,getdate()))";
                        }
                    }
                    else
                    {
                        query = "select distinct im.*,irf.*,cl.*,ic.*,uli.Emailid as HRCoordinatorEmailId,ulin.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.* from InterviewMeetings im   join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist uli on uli.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist ulin on ulin.EmailId=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 LEFT join(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_LEFT' and Meetingid=im.ZoomMeetingId )t4 on t3.ParticipantName =t4.ParticipantName left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId where t4.id is null FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined   where confirmationStatus=1 and uli.Emailid =@emailID and im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1 and dateadd(minute,2,im.Interviewdate)<=dateadd(minute,330,getdate()) and dateadd(minute,20,im.Interviewdate)>=dateadd(minute,330,getdate()))";
                    }
                }
                else if (actionstatus == "VideoLink")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and DATEDIFF(DAY, im.interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and DATEDIFF(DAY, im.interviewDate, GETDATE())<7";
                        }
                    }
                    else
                    {
                        query = "select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID and DATEDIFF(DAY, im.interviewDate, GETDATE())<7 union select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID and DATEDIFF(DAY, im.interviewDate, GETDATE())<7";
                    }
                }
                else if (actionstatus == "UnProcessedResume")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "Select IRF.Id as irfIntId,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite as PrimarySkill,cl.Name as Client,count(cr.id) as TotalView from IRF join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join CandidateResume cr on cr.IRFId=IRF.id where ulsh.Emailid= @Emailid and ISNULL(cr.IsProcessed,0)=0 group by IRF.Id,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite,cl.Name";
                        }
                        else
                        {
                            query = "select IRF.Id as irfIntId,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite as PrimarySkill,cl.Name as Client,count(cr.id) as TotalView from IRF join Client cl on cl.id=irf.ClientId join CandidateResume cr on cr.IRFId=IRF.id Where ISNULL(cr.IsProcessed,0)=0 group by IRF.Id,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite,cl.Name";
                        }
                    }
                    else
                    {
                        query = "Select CR.id as irfIntId,CR.IRFId,CR.JDTiltle,CR.Name as Client,Cr.PrimarySkillInvite as PrimarySkill,Count(CR.Candidateid) as TotalView from (select IRF.Id,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite,cl.Name,cr.id as Candidateid from  IRF join Client cl on irf.ClientId=cl.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join CandidateResume cr on cr.IRFId=IRF.id where ul.EmailId=@emailID and ISNULL(cr.IsProcessed,0)=0 union select IRF.Id,IRF.IRFId,IRF.JDTiltle,IRF.PrimarySkillInvite,cl.Name,cr.id as Candidateid from IRF join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr join CandidateResume cr on cr.IRFId=IRF.id where ul.EmailId=@emailID and ISNULL(cr.IsProcessed,0)=0 ) as Cr Group By CR.id,CR.IRFId,CR.JDTiltle,CR.Name,Cr.PrimarySkillInvite";
                    }
                }
                else if (actionstatus == "QCPendingReport")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7";
                        }
                    }
                    else
                    {
                        query = "select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID union select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID";
                    }
                }
                else if (actionstatus == "QCReport")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7";
                        }
                    }
                    else
                    {
                        query = "select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID union select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id  join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and interviewDate<GETDATE() and DATEDIFF(DAY, interviewDate, GETDATE())<7 and ul.EmailId=@emailID";
                    }
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = new List<MeetingVM>();
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            DynamicParameters dp = new DynamicParameters();
                            dp.Add("@Emailid", emailid);
                            results = (List<MeetingVM>)await con.QueryAsync<MeetingVM>(query, dp);
                        }
                        else
                        {
                            results = (List<MeetingVM>)await con.QueryAsync<MeetingVM>(query, null);
                        }
                    }
                    else
                    {
                        DynamicParameters dp = new DynamicParameters();
                        dp.Add("@emailID", emailid);
                        results = (List<MeetingVM>)await con.QueryAsync<MeetingVM>(query, dp);
                    }
                    if (actionstatus == "MeetingStatus")
                    {
                        results = results.Where(x => x.interviewDate < DateTime.Now).ToList();
                    }
                    if (actionstatus == "ZoomLink")
                    {
                        results = results.Where(x => x.interviewDate > DateTime.Now).ToList();
                    }
                    return results;
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
        public async Task<List<MeetingVM>> FetchRibbonDetailsValuesbydate(string actionstatus, string emailid, int actiontype, string profileName)
        {
            try
            {
                string query = string.Empty;
                if (actionstatus == "InterviewCount")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid where confirmationStatus=1 and interviewer is null and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id where confirmationStatus=1 and interviewer is null";
                        }
                    }
                    else
                    {
                        query = "select distinct im.*,irf.*,cl.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and interviewer is null and ul.EmailId=@emailID union select distinct im.*,irf.*,cl.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and interviewer is null and ul.EmailId=@emailID";
                    }
                }
                else if (actionstatus == "MeetingStatus")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2)";
                        }
                    }
                    else
                    {
                        query = "select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId= im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and ul.EmailId=@emailID union select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId= im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and ul.EmailId=@emailID";
                    }
                }
                else if (actionstatus == "UnAssignedCoordianter")
                {
                    if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and isAutoCancelled=1 and ulsh.Emailid= @Emailid";
                    }
                    else
                    {
                        query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and isAutoCancelled=1";
                    }
                }
                else if (actionstatus == "ZoomLink")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 and (zoomLink is null or zoomLink='')";
                        }
                    }
                    else
                    {
                        query = "select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID union select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where confirmationStatus=1 and (zoomLink is null or zoomLink='') and ul.EmailId=@emailID";
                    }
                }
                else if (actionstatus == "UnAttendedZoomLink")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.WhoJoined from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer join TransZoomMeetingExtendDetails WhoJoined on WhoJoined.Meetingid=im.ZoomMeetingId and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "Select distinct im.*,irf.*,cl.Name,ic.*,ul.Emailid as HRCoordinatorEmailId,uli.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.WhoJoined from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist ul on ul.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist uli on uli.Emailid=im.interviewer join TransZoomMeetingExtendDetails WhoJoined on WhoJoined.Meetingid=im.ZoomMeetingId";
                        }
                    }
                    else
                    {
                        //where (ISNULL(t1.meetingMember,0)-ISNULL(t2.meetingMember,0))=1
                        query = "select distinct im.*,irf.*,cl.*,ic.*,uli.Emailid as HRCoordinatorEmailId,ulin.MobileNumber as InterviewerMobileNumber,imul.MobileNumber as ClientMobileNumber,WhoJoined.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id left join Userlist uli on uli.id=irf.risebirdhr left join Userlist imul on imul.EmailId=im.ClientHrContact left join Userlist ulin on ulin.EmailId=im.interviewer Outer Apply (select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from (Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=im.ZoomMeetingId )t3 left join InterviewerProfile ip on ip.NickName=t3.ParticipantName left join USERList ul on ul.id=ip.UserId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined from ZoomMeetingTransmission t where t.Meetingid=im.ZoomMeetingId) WhoJoined where confirmationStatus=1 and uli.EmailId =@emailID and im.ZoomMeetingId in (Select t1.meetingid from(Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_JOINED' and ParticipantName not like '%risebird.io' group by meetingid) as t1 left join (Select meetingid,Count(*) as meetingMember from ZoomMeetingTransmission where Event='PARTICIPANT_LEFT' and ParticipantName not like '%risebird.io' group by meetingid) as t2 on t1.Meetingid=t2.Meetingid)";
                    }
                }
                else if (actionstatus == "VideoLink")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "Select distinct * from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='')";
                        }
                    }
                    else
                    {
                        query = "select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID union select distinct im.*,irf.*,cl.*,ic.* from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id left join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr where ic.MeetingStatus=4 and (ic.VideoLink is null or ic.VideoLink='') and ul.EmailId=@emailID";
                    }
                }
                else if (actionstatus == "QCPendingReport")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null";
                        }
                    }
                    else
                    {
                        query = "select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ul.EmailId=@emailID union select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id left join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and tq.id is null and ul.EmailId=@emailID";
                    }
                }
                else if (actionstatus == "QCReport")
                {
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            query = "Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join SeniorHr sh on sh.Clientid=irf.ClientId left join UserList ulsh on ulsh.id=sh.Userid join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ulsh.Emailid= @Emailid";
                        }
                        else
                        {
                            query = "Select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)";
                        }
                    }
                    else
                    {
                        query = "select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join TransHR th on th.IRFId = irf.id join HR on HR.id=th.HRId join Userlist ul on ul.id=HR.UserId join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ul.EmailId=@emailID union select distinct im.IRFId,im.HRCoordinatorEmailId,ic.InterviewId,im.interviewDate,im.candidateName,im.candidateEmail,im.interviewer,ic.MeetingStatus,ic.InterviewStatus,cl.Name,tq.Redflags from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id join Userlist ul on ul.id=irf.RisebirdHr join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id join Candidate ca on ca.CandidateMasterId=cm.id join Trans_QCFormReport tq on tq.Interviewid=im.id where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) and ul.EmailId=@emailID";
                    }
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = new List<MeetingVM>();
                    if (actiontype == 0)
                    {
                        if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            DynamicParameters dp = new DynamicParameters();
                            dp.Add("@Emailid", emailid);
                            results = (List<MeetingVM>)await con.QueryAsync<MeetingVM>(query, dp);
                        }
                        else
                        {
                            results = (List<MeetingVM>)await con.QueryAsync<MeetingVM>(query, null);
                        }
                    }
                    else
                    {
                        DynamicParameters dp = new DynamicParameters();
                        dp.Add("@emailID", emailid);
                        results = (List<MeetingVM>)await con.QueryAsync<MeetingVM>(query, dp);
                    }
                    if (actionstatus == "MeetingStatus" || actionstatus == "VideoLink")
                    {
                        results = results.Where(x => x.interviewDate < DateTime.Now).ToList();
                    }
                    if (actionstatus == "ZoomLink")
                    {
                        results = results.Where(x => x.interviewDate > DateTime.Now).ToList();
                    }
                    return (List<MeetingVM>)results;
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
        public async Task<NotUpdatedMeetings> GetNotUpdatedMeetingsCountbyHr(string Emailid)
        {
            try
            {
                string query = string.Empty;
                DateTime currentdate = DateTime.Now;
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@currentdate", currentdate);
                dp.Add("@emailID", Emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<NotUpdatedMeetings>(con, "usp_GetNotUpdatedMeetingsCountforHR", dp,
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
        public async Task<List<MeetingDetailsForParticipant>> SendNotoificationForParticipantWaiting()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingDetailsForParticipant>(con, "usp_FetchParticipantZoomMeetingWaiting", null,
                        commandType: CommandType.StoredProcedure);
                    return (List<MeetingDetailsForParticipant>)result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<List<ZoomMeetingHistoryDetails>> FetchZoomMeetingHistory(string meetingid)
        {
            try
            {

                string query = "Select * from ZoomMeetingTransmission where Meetingid=@Meetingid";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters dp = new DynamicParameters();
                    dp.Add("@Meetingid", meetingid);
                    var results = (List<ZoomMeetingHistoryDetails>)await con.QueryAsync<ZoomMeetingHistoryDetails>(query, dp);
                    return (List<ZoomMeetingHistoryDetails>)results;
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
        public async Task<IEnumerable<InterviewManagementVM>> GetInterviewManagement(string Emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var date = DateTime.Now.AddDays(1);
                    var startdate = date.AddDays(-7);
                    var parameters = new DynamicParameters();
                    parameters.Add("@startdate", startdate.ToString("yyyy-MM-dd"));
                    parameters.Add("@enddate", date.ToString("yyyy-MM-dd"));
                    parameters.Add("@Emailid", Emailid);
                    var result = await SqlMapper.QueryAsync<InterviewManagementVM>(con, "[usp_GetInterviewManagement]", parameters,
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
        public async Task<InterviewerDashboardVM> GetInterviewDetailsCount(string emailId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    InterviewerDashboardVM _resultVM = new InterviewerDashboardVM();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_GetInterviewDetailsCount", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.AddWithValue("@emailID", emailId);
                    adapter.SelectCommand.Parameters.AddWithValue("@timeNow", DateTime.Now);
                    adapter.Fill(dataset);
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        _resultVM.MyInterviewCount = !String.IsNullOrEmpty(dt.Rows[0][0].ToString()) ? Convert.ToInt32(dt.Rows[0][0]) : 0;
                    }
                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        _resultVM.OpenInterviewCount = !String.IsNullOrEmpty(dt.Rows[0][0].ToString()) ? Convert.ToInt32(dt.Rows[0][0]) : 0;
                    }
                    if (dataset.Tables.Count > 2)
                    {
                        dt = dataset.Tables[2];
                        _resultVM.PendingReportCount = !String.IsNullOrEmpty(dt.Rows[0][0].ToString()) ? Convert.ToInt32(dt.Rows[0][0]) : 0;
                    }
                    if (dataset.Tables.Count > 3)
                    {
                        dt = dataset.Tables[3];
                        _resultVM.QCReportCount = !String.IsNullOrEmpty(dt.Rows[0][0].ToString()) ? Convert.ToInt32(dt.Rows[0][0]) : 0;
                    }
                    return _resultVM;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<List<string>>FetchAlertMessage()
        {
            try
            {            
               using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Currenttime", DateTime.Now);
                   var result = await SqlMapper.QueryAsync<string>(con, "usp_GetAlterMessage", parameters,
                     commandType: CommandType.StoredProcedure);
                    return (List<string>)result;
                }
               
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
    }
}
