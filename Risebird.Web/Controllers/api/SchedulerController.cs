using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers.api
{
    [Produces("application/json")]
    [Route("api/Scheduler")]
    public class SchedulerController : Controller
    {
        private readonly IIRFService _irfService;
        private readonly INotificationHelper _notificationHelper;

        public SchedulerController(IIRFService irfFService, INotificationHelper notificationHelper)
        {
            _irfService = irfFService;
            _notificationHelper = notificationHelper;
        }

        [HttpGet]
        public async Task<HttpResponseMessage> IRFBasedInterviewerMail(string IRFID, string interviewDate)
        {
            var interviwers = _irfService.GetListOfInterviwerBasedOnIRFId(IRFID);
            if(interviwers!= null)
            {
                _notificationHelper.SendIRFBasedInterviewerMail(interviwers, interviewDate);

            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}