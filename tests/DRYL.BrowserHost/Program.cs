using DRYL.BrowserHost;
using DRYL.BrowserHost.Components;
using DRYL.Components;
using DRYL.Components.Agents;
using Microsoft.JSInterop;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddDrylComponents().AddDrylAgents();

// The public HTTP seam keeps token minting on the real runner while making an
// outbound voice request impossible in this test-only host.
builder.Services.AddSingleton(new HttpClient(new FakeTokenHandler()));
builder.Services.AddScoped(sp => new DrylVoiceRunner(
    sp.GetRequiredService<IJSRuntime>(), sp.GetRequiredService<HttpClient>()));

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapGet("/health", () => Results.Text("ready"));
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

public partial class Program;
