using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Risebird.Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckIRFAvailabilityController : ControllerBase
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly ITwilioHelper _twilioHelper;
        private readonly IClientService _clientService;
        private readonly IIRFService _irfService;
        private readonly IIRFRepository _irfRepository;
        private readonly IInterviewSchedulerService _interviewSchedulerservice;
        private readonly INotificationHelper _notificationHelper;
        private readonly HttpClient _httpClient;
        private readonly CancellationToken _cancellationToken;
        public CheckIRFAvailabilityController(IApplicationErrorLogger applicationErrorLogger,
            IClientService clientService, IIRFService irfService,
            IIRFRepository irfRepository, IInterviewSchedulerService interviewSchedulerservice,
            INotificationHelper notificationHelper, IEncryptionHelper encryptionHelper,
            ITwilioHelper twilioHelper,
            HttpClientHelper factory)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _clientService = clientService;
            _irfService = irfService;
            _irfRepository = irfRepository;
            _interviewSchedulerservice = interviewSchedulerservice;
            _notificationHelper = notificationHelper;
            _encryptionHelper = encryptionHelper;
            _twilioHelper = twilioHelper;
            _httpClient = factory.GetHttpClient();
        }

        [HttpPost]
        [Route("InterviewerAvailabilityCheck")]
        public async Task<IActionResult> InterviewerAvailabilityCheck(AvailabilityCheckDTO obj)
        {
            try
            {
                if (obj != null)
                {
                    var interviewers = await _irfRepository.GetAvailableAssessmentGroupInterviewerMembers(Convert.ToInt16(obj?.AssessmentGroupId), obj?.id);

                    foreach (var interviewer in interviewers)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(interviewer.EmailId))
                            {

                                await _twilioHelper.MakeIVRCallToInterviewerForAvailability(interviewer.Phone, interviewer.InterviewerName, interviewer.uniqueid, obj.id, obj.JDTitle);
                                //string yesbutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=1&uniqueid=" + interviewer.uniqueid + "&Irfid=" + obj.id + "&dec=" + encryptedDate;
                                //string nobutton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=2&uniqueid=" + interviewer.uniqueid + "&Irfid=" + obj.id + "&dec=" + encryptedDate;
                                //string dropMyProfileButton = "https://portal.risebird.io/IRF/UpdateInterviewerAvailbilityStatusForIRF?status=4&uniqueid=" + interviewer.uniqueid + "&Irfid=" + obj.id + "&dec=" + encryptedDate;

                            }
                        }
                        catch (Exception ex1)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex1);
                        }
                    }

                    //should I update the following for IVR too?
                    //await _irfService.UpdateIntervieweravailbilityDetails(obj.id);


                    return new OkObjectResult("Successful");
                }
                else
                {
                    var detailsError = new Dictionary<string, string>();

                    detailsError.Add("RequestBody", "TRequest Body cannot be null.");

                    var errorResponse = new ErrorResponse
                    {
                        Error = "RequestBody is null",
                        Details = detailsError
                    };
                    return new BadRequestObjectResult(errorResponse);
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
