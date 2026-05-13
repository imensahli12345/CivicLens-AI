using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CivicLens.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CivicLens.API.Services.Classes
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(HttpClient http, IConfiguration config, ILogger<WhatsAppService> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendHelloWorldAsync(string to, CancellationToken ct)
        {
            if (!IsEnabled())
            {
                _logger.LogInformation("WhatsApp disabled: skip hello_world.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(to))
                return false;

            var phoneNumberId = _config["WhatsApp:PhoneNumberId"];
            if (string.IsNullOrWhiteSpace(phoneNumberId))
                throw new InvalidOperationException("WhatsApp:PhoneNumberId est manquant.");

            var token = Environment.GetEnvironmentVariable("WHATSAPP_ACCESS_TOKEN") ?? _config["WhatsApp:AccessToken"];
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("WHATSAPP_ACCESS_TOKEN est manquant (variable d'environnement).");

            var normalizedTo = NormalizeToDigits(to);
            if (string.IsNullOrWhiteSpace(normalizedTo))
                return false;

            var payload = new
            {
                messaging_product = "whatsapp",
                to = normalizedTo,
                type = "template",
                template = new
                {
                    name = "hello_world",
                    language = new { code = "en_US" }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{phoneNumberId}/messages");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(payload);

            try
            {
                var resp = await _http.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode)
                    return true;

                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("WhatsApp API error {StatusCode}: {Body}", (int)resp.StatusCode, body);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WhatsApp send failed");
                return false;
            }
        }

        public async Task<bool> SendTextAsync(string to, string body, CancellationToken ct)
        {
            if (!IsEnabled())
            {
                _logger.LogInformation("WhatsApp disabled: skip text message.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(body))
                return false;

            var phoneNumberId = _config["WhatsApp:PhoneNumberId"];
            if (string.IsNullOrWhiteSpace(phoneNumberId))
                throw new InvalidOperationException("WhatsApp:PhoneNumberId est manquant.");

            var token = Environment.GetEnvironmentVariable("WHATSAPP_ACCESS_TOKEN") ?? _config["WhatsApp:AccessToken"];
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("WHATSAPP_ACCESS_TOKEN est manquant (variable d'environnement).");

            var normalizedTo = NormalizeToDigits(to);
            if (string.IsNullOrWhiteSpace(normalizedTo))
                return false;

            var payload = new
            {
                messaging_product = "whatsapp",
                to = normalizedTo,
                type = "text",
                text = new
                {
                    preview_url = false,
                    body = body.Trim()
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{phoneNumberId}/messages");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(payload);

            try
            {
                var resp = await _http.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode)
                    return true;

                var respBody = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("WhatsApp API error {StatusCode}: {Body}", (int)resp.StatusCode, respBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WhatsApp send failed");
                return false;
            }
        }

        private bool IsEnabled()
        {
            var v = _config["WhatsApp:Enabled"];
            return bool.TryParse(v, out var enabled) && enabled;
        }

        private static string NormalizeToDigits(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var chars = input.Trim().ToCharArray();
            Span<char> buffer = stackalloc char[chars.Length];
            var j = 0;
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (c >= '0' && c <= '9')
                    buffer[j++] = c;
            }
            return new string(buffer[..j]);
        }
    }
}
