using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class AIChatController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("api/aichat")]
        public async Task<IActionResult> Chat([FromBody] ChatMessage message)
        {
            // TODO: Implement your AI chat logic here
            // This is where you'll integrate with your AI service
            
            var response = new
            {
                response = $"This is a sample response to: {message.Message}"
            };

            return Json(response);
        }
    }

    public class ChatMessage
    {
        public string Message { get; set; }
    }
} 