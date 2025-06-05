using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Asn1.Crmf;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class ChatgptController : Controller
    {
        // Index action - displays the chat interface
        public IActionResult ChatIndex()
        {
            return View();
        }

        // SendMessage action - processes chat messages
        //    [HttpPost]
        //    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        //    {
        //        1. Takes user message
        //        2. Sends it to OpenAI API
        //       3. Returns the AI response
        //    }
        //}
    }
}
