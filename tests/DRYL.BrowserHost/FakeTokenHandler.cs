using System.Net;
using System.Text;
using System.Text.Json;

namespace DRYL.BrowserHost;

/// <summary>Offline-only Realtime and Live responses for the browser host's voice runner.</summary>
public sealed class FakeTokenHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Method != HttpMethod.Post || request.RequestUri?.Host != "voice-fixture.invalid")
        {
            throw new InvalidOperationException("The browser fixture refuses non-fixture provider requests.");
        }

        var path = request.RequestUri.AbsolutePath;
        if (path == "/v1/live/sessions")
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var root = json.RootElement;
            var session = root.GetProperty("session");
            var backend = session.GetProperty("delegation").GetProperty("responses");
            var tools = backend.GetProperty("tools").EnumerateArray().ToArray();
            var valid = request.Headers.Authorization?.Parameter == "offline-fixture-key" &&
                root.GetProperty("transport").GetProperty("type").GetString() == "webrtc" &&
                root.GetProperty("transport").GetProperty("sdp").GetString() == "fixture-offer" &&
                session.GetProperty("model").GetString() == "gpt-live-1" &&
                session.GetProperty("audio").GetProperty("output").GetProperty("voice").GetString() == "gleam" &&
                !session.GetProperty("store").GetBoolean() && !session.TryGetProperty("type", out _) &&
                !session.GetProperty("audio").TryGetProperty("input", out _) &&
                session.GetProperty("delegation").GetProperty("type").GetString() == "responses" &&
                backend.GetProperty("model").GetString() == "gpt-5.6-terra" &&
                backend.GetProperty("reasoning").GetProperty("effort").GetString() == "medium" &&
                !backend.GetProperty("parallel_tool_calls").GetBoolean() &&
                session.GetProperty("input")[0].GetProperty("content")[0].GetProperty("text").GetString() == "Earlier conversation" &&
                tools.Any(tool => tool.GetProperty("type").GetString() == "web_search") &&
                tools.Any(tool => tool.GetProperty("type").GetString() == "function" &&
                    tool.GetProperty("name").GetString() == "fixture_lookup" && !tool.GetProperty("strict").GetBoolean());
            if (!valid) throw new InvalidOperationException("The Live fixture received an invalid server request.");
            return Response("""{"session":{"id":"live_offline_fixture"},"transport":{"type":"webrtc","sdp":"fixture-live-answer"}}""");
        }
        if (path == "/v1/live/sessions/live_offline_fixture/hangup") return Response("{}");
        if (path == "/v1/realtime/client_secrets") return Response("{\"value\":\"ek_offline_fixture\"}");
        throw new InvalidOperationException("The browser fixture refuses unexpected provider paths.");
    }

    private static HttpResponseMessage Response(string json) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
