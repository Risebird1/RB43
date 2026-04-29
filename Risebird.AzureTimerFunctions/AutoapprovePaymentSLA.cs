using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository;
using Risebird.Repository.Interface;
using System;

namespace Risebird.AzureTimerFunctions;

public class AutoapprovePaymentSLA
{
    private readonly IApplicationErrorLogger _applicationErrorLogger;
    private readonly ILogger _logger;
    private readonly IMeetingService _meetingService;

    public AutoapprovePaymentSLA(ILoggerFactory loggerFactory, IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger)
    {
        _logger = loggerFactory.CreateLogger<AutoapprovePaymentSLA>();
        _meetingService = meetingService;
        _applicationErrorLogger = applicationErrorLogger;
    }

    [Function("AutoapprovePaymentSLA")]
    public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
        try
        {
            var qcautoapprovalPending = await _meetingService.GetAutoApprovalPaymentSLAPendingList();

            foreach (var item in qcautoapprovalPending)
            {
                try
                {
                    var model = await _meetingService.FetchPaymentSlaDetailsById(item.Interviewid, "");
                    if (model == null)
                    {
                        model = new PaymentSLAVM();
                    }
                    model.isapprovedchecked = true;
                    if (await _meetingService.AddorUpdatePaymentSLADetails(model))
                    {
                        if (model.isapprovedchecked)
                        {
                            var result = new TransInterviewersReliabilityScoresVm();
                            result.InterviewMeetingId = model.Interviewid;
                            result.ReliabilityScoreTypeId = (int)ReliabilityScopeTypeEnum.PerPaymentSLA;
                            await _meetingService.AddTransInterviewersReliabilityScoresService(result);
                        }
                        else
                        {
                            await _meetingService.ApplyPenaltyQCForm(item.Interviewid, model.Interviewer, 0, model.VideoReward);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("AutoapprovePaymentSLA get error interviewid: {Interviewid} and Error is {error}", item.Interviewid, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        }
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }
}