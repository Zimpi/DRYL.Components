using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using DRYL.Components.Agents;
using Xunit;

namespace DRYL.Components.Tests.Agents.Voice;

/// <summary>The runner is the only place the API key exists. What leaves it is a token.</summary>
public class DrylVoiceRunnerTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string Response { get; set; } = """{"value":"ek_abc","expires_at":1}""";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(Status)
            {
                Content = new StringContent(Response, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static DrylVoiceRunner Runner(CapturingHandler handler) =>
        new(new NoopJsRuntime(), new HttpClient(handler));

    [Fact]
    public async Task It_posts_the_session_to_the_client_secrets_endpoint()
    {
        var handler = new CapturingHandler();

        var token = await Runner(handler).MintTokenAsync(
            new DrylVoiceOptions { ApiKey = "sk-test", Voice = "cedar" }, CancellationToken.None);

        Assert.Equal("ek_abc", token);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal(
            "https://api.openai.com/v1/realtime/client_secrets",
            handler.Request.RequestUri!.ToString());
        Assert.Equal("sk-test", handler.Request.Headers.Authorization!.Parameter);

        var body = JsonNode.Parse(handler.Body!)!;
        Assert.Equal("cedar", (string?)body["session"]!["audio"]!["output"]!["voice"]);
        Assert.NotNull(body["expires_after"]);
    }

    [Fact]
    public async Task A_custom_base_url_is_honoured()
    {
        var handler = new CapturingHandler();

        await Runner(handler).MintTokenAsync(
            new DrylVoiceOptions { ApiKey = "sk-test", BaseUrl = "https://proxy.example/v1/" },
            CancellationToken.None);

        Assert.Equal(
            "https://proxy.example/v1/realtime/client_secrets",
            handler.Request!.RequestUri!.ToString());
    }

    [Fact]
    public async Task The_safety_identifier_travels_as_a_header_when_set()
    {
        var handler = new CapturingHandler();

        await Runner(handler).MintTokenAsync(
            new DrylVoiceOptions { ApiKey = "sk-test", SafetyIdentifier = "hashed-jan" },
            CancellationToken.None);

        Assert.Equal(
            "hashed-jan",
            handler.Request!.Headers.GetValues("OpenAI-Safety-Identifier").Single());
    }

    [Fact]
    public async Task A_rejected_key_surfaces_as_the_api_own_message()
    {
        // A wrong key, an exhausted quota and an unknown model all arrive as 4xx and mean
        // completely different things — the status code alone helps nobody.
        var handler = new CapturingHandler
        {
            Status = HttpStatusCode.Unauthorized,
            Response = """{"error":{"message":"Incorrect API key provided."}}""",
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Runner(handler).MintTokenAsync(
                new DrylVoiceOptions { ApiKey = "sk-bad" }, CancellationToken.None));

        Assert.Contains("Incorrect API key", ex.Message);
    }

    [Fact]
    public async Task A_refusal_that_is_not_json_still_says_something_useful()
    {
        var handler = new CapturingHandler
        {
            Status = HttpStatusCode.BadGateway,
            Response = "<html>gateway</html>",
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Runner(handler).MintTokenAsync(
                new DrylVoiceOptions { ApiKey = "sk-test" }, CancellationToken.None));

        Assert.Contains("502", ex.Message);
    }

    [Fact]
    public async Task A_missing_key_never_reaches_the_network()
    {
        var handler = new CapturingHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Runner(handler).MintTokenAsync(new DrylVoiceOptions(), CancellationToken.None));

        Assert.Null(handler.Request);
    }
}
