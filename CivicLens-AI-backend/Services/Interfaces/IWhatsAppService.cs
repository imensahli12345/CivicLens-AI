using System.Threading;
using System.Threading.Tasks;

namespace CivicLens.API.Services.Interfaces
{
    public interface IWhatsAppService
    {
        Task<bool> SendHelloWorldAsync(string to, CancellationToken ct);
        Task<bool> SendTextAsync(string to, string body, CancellationToken ct);
    }
}
