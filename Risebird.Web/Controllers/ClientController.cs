using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using System;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class ClientController : Controller
    {
        private IClientService _clientService;
        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }

        public IActionResult List()
        {
            try
            {
                return View(_clientService.GetClients());
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}