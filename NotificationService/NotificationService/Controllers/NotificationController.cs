using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("sendMessage")]
        public async Task<IActionResult> Send([FromBody] NotificationRequest request)
        {
            try
            {
                var result = await _notificationService.SendNotificationRequestAsync(request);
                return Ok(new { Message = $"Notification of type '{result}' sent successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "An error occurred while processing your request.", Details = ex.Message });
            }
        }
    }
}
