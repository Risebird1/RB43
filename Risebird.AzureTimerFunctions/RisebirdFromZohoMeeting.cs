using System;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository;
using Risebird.Repository.Interface;
using UglyToad.PdfPig.Logging;
using static Risebird.Entities.Data.RchilliResumeMapFields;

namespace Risebird.AzureTimerFunctions;

public class RisebirdFromZohoMeeting
{
    private readonly ILogger _logger;
    private readonly IZohoMeetingHelper _zoho;
    private readonly IConfiguration _configuration;
    private readonly IMeetingRespository _meetingRespository;

    public RisebirdFromZohoMeeting(ILoggerFactory loggerFactory, IZohoMeetingHelper zoho, IConfiguration configuration, IMeetingRespository meetingRespository)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<RisebirdFromZohoMeeting>();
        _zoho = zoho;
        _meetingRespository = meetingRespository;
    }

    [Function("RisebirdFromZohoMeeting")]
    public async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer) //Every 2 min 
    {
        _logger.LogInformation("RisebirdFromZohoMeeting function executed at: {executionTime}", DateTime.Now);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
        var meetingForParticipants = await _meetingRespository.GetZohoLiveMeetingInfomation();
        var accesstoken = await _zoho.GetAccessTokenAsync();
        if (meetingForParticipants.Count() > 0)
        {
            var _meetingList = meetingForParticipants.Select(x => x.ZoomMeetingId).Distinct().ToList();
            if (_meetingList.Count() > 0)
            {
                Parallel.ForEach(_meetingList, meeting =>
                {
                    try
                    {
                        var _participantList = new List<ZohoParticipantList>();
                        var ParticipantInfo = _zoho.GetParticipantReportAsync(meeting).Result;
                        foreach (var item in ParticipantInfo)
                        {
                            var meets = meetingForParticipants.Where(x => x.ZoomMeetingId == meeting && x.ParticipantName == item.email)?.ToList();
                            if (meets == null)
                            {
                                meets = new List<ZohoParticipantList>();
                            }
                            if (item.joinTime > 0)
                            {
                                DateTime joinDateTime = DateTimeOffset.FromUnixTimeMilliseconds(item.joinTime).ToLocalTime().DateTime;
                                joinDateTime = joinDateTime.AddTicks(-(joinDateTime.Ticks % TimeSpan.TicksPerSecond));
                                var _meets = meets.Where(x => x.Event == "PARTICIPANT_JOINED" && x.ParticipantName == item.email && x.ActionDate == joinDateTime)?.ToList();
                                if (_meets == null)
                                {
                                    _meets = new List<ZohoParticipantList>();
                                }
                                if (_meets.Count() == 0)
                                {
                                    _participantList.Add(new ZohoParticipantList
                                    {
                                        ParticipantName = item.email,
                                        ActionDate = joinDateTime,
                                        Meetingid = meeting,
                                        Event = "PARTICIPANT_JOINED"
                                    });
                                }
                            }
                            if (item.leaveTime > 0)
                            {
                                DateTime leaveDateTime = DateTimeOffset.FromUnixTimeMilliseconds(item.leaveTime).ToLocalTime().DateTime;
                                leaveDateTime = leaveDateTime.AddTicks(-(leaveDateTime.Ticks % TimeSpan.TicksPerSecond));
                                var _meets = meets.Where(x => x.Event == "PARTICIPANT_LEFT" && x.ParticipantName == item.email && x.ActionDate == leaveDateTime)?.ToList();
                                if (_meets == null)
                                {
                                    _meets = new List<ZohoParticipantList>();
                                }
                                if (_meets.Count() == 0)
                                {
                                    _participantList.Add(new ZohoParticipantList
                                    {
                                        ParticipantName = item.email,
                                        ActionDate = leaveDateTime,
                                        Meetingid = meeting,
                                        Event = "PARTICIPANT_LEFT"
                                    });
                                }
                            }
                        }
                        if (_participantList.Count() > 0)
                        {
                            _meetingRespository.SaveZohoMeetingParticipantDetails(_participantList);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("Zoho Meeting Fail to get Participants for Meeting Id: {meeting} and error: {exection}", meeting, ex);
                    }
                });
            }
        }
        _logger.LogInformation("RisebirdFromZohoMeeting function execution Completed: {executionTime}", DateTime.Now);
    }
}