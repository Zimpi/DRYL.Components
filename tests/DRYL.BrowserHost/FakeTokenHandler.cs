using System.Net;
using System.Text;

namespace DRYL.BrowserHost;

/// <summary>Offline-only token response for the browser host's voice runner.</summary>
public sealed class FakeTokenHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Method != HttpMethod.Post ||
            request.RequestUri?.AbsoluteUri != "https://voice-fixture.invalid/v1/realtime/client_secrets")
        {
            throw new InvalidOperationException("The browser fixture refuses non-fixture token requests.");
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"value\":\"ek_offline_fixture\"}", Encoding.UTF8, "application/json")
        });
    }
}
