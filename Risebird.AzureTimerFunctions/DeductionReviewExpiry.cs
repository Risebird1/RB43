using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;

namespace Risebird.AzureTimerFunctions;

/// <summary>
/// Deduction review (My Quality phase 2): requests QC did not decide within the working-day deadline close as
/// "deduction stands" (Expired), and the QC lead (DeductionReview:QcLeadEmails) is emailed about them once.
/// The QC queue and the interviewer page also close overdue requests on load; this timer makes sure the lead hears.
/// </summary>
public class DeductionReviewExpiry
{
    private readonly IApplicationErrorLogger _applicationErrorLogger;
    private readonly ILogger _logger;
    private readonly IDeductionReviewService _deductionReviewService;

    public DeductionReviewExpiry(ILoggerFactory loggerFactory, IDeductionReviewService deductionReviewService,
        IApplicationErrorLogger applicationErrorLogger)
    {
        _logger = loggerFactory.CreateLogger<DeductionReviewExpiry>();
        _deductionReviewService = deductionReviewService;
        _applicationErrorLogger = applicationErrorLogger;
    }

    [Function("DeductionReviewExpiry")]
    public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("DeductionReviewExpiry executed at: {executionTime}", DateTime.Now);
        try
        {
            var reported = await _deductionReviewService.ExpireAndNotifyLeadAsync();
            if (reported > 0)
                _logger.LogInformation("DeductionReviewExpiry: {count} expired request(s) reported to the QC lead.", reported);
        }
        catch (Exception ex)
        {
            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        }
    }
}
