using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Risebird.AzureTimerFunctions;

public class RisebirdFromZohoMeeting
{
    private readonly ILogger _logger;

    public RisebirdFromZohoMeeting(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RisebirdFromZohoMeeting>();
    }

    [Function("RisebirdFromZohoMeeting")]
    public void Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer) //Every 2 min 
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
        //Fetch Meeting Infomation From DB to check the Participent Report
        //Fetch Participant Report using Zoho API in loop
        //Save the data with validation of duplication Records
    }
}