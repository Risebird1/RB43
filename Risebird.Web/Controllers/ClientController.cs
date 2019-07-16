using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class ClientController : Controller
    {
        private IClientService _clientService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public ClientController(IClientService clientService, IApplicationErrorLogger applicationErrorLogger)
        {
            _clientService = clientService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IActionResult> List()
        {
            try
            {
                return View(_clientService.GetClients());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}