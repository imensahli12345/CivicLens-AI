using System.Threading;
using System.Threading.Tasks;
using CivicLens.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace CivicLens.API.Controllers
{
    [Route("api/whatsapp")]
    [ApiController]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsAppService _whatsApp;
        private readonly IConfiguration _config;

        public WhatsAppController(IWhatsAppService whatsApp, IConfiguration config)
        {
            _whatsApp = whatsApp;
            _config = config;
        }

        [HttpGet("diag")]
        public IActionResult Diag()
        {
            var enabledStr = _config["WhatsApp:Enabled"];
            var enabled = bool.TryParse(enabledStr, out var e) && e;
            var phoneNumberId = _config["WhatsApp:PhoneNumberId"];
            var hasEnvToken = !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("WHATSAPP_ACCESS_TOKEN"));

            return Ok(new
            {
                enabled,
                phoneNumberId,
                hasEnvToken
            });
        }

        [HttpPost("test-hello")]
        public async Task<IActionResult> SendHello([FromQuery] string to, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(to))
                return BadRequest("Paramètre 'to' requis (ex: +216XXXXXXXX).");

            var ok = await _whatsApp.SendHelloWorldAsync(to, ct);
            if (!ok)
                return StatusCode(502, new { message = "Envoi WhatsApp échoué (vérifie token, PhoneNumberId, et restrictions)."});

            return Ok(new { message = "Message WhatsApp envoyé." });
        }
    }
}
