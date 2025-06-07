using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class AIChatController : Controller
    {
        private readonly IAIChatService _aiChatService;

        public AIChatController(IAIChatService aiChatService)
        {
            _aiChatService = aiChatService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("Message cannot be empty");
            }

            var userId = User.Identity.Name; // Or your user ID retrieval logic
            var response = await _aiChatService.GetAIResponseAsync(message);
            await _aiChatService.SaveChatHistoryAsync(userId, message, response);

            return Json(new
            {
                success = true,
                response = response,
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistory()
        {
            var userId = User.Identity.Name; // Or your user ID retrieval logic
            var history = await _aiChatService.GetChatHistoryAsync(userId);
            return Json(history);
        }
    }
} 