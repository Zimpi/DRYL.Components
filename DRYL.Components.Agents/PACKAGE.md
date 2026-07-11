# DRYL.Components.Agents

Companion package for [**DRYL.Components**](https://www.nuget.org/packages/DRYL.Components)
that bridges the [Microsoft Agent Framework](https://www.nuget.org/packages/Microsoft.Agents.AI)
(`Microsoft.Agents.AI`) to DRYL's AI vocabulary. It takes real work off your hands across
four subsystems — without you ever setting `Ai="…"` by hand.

> **Experimental — 0.5.0.** Independently versioned and deliberately decoupled from the
> stable core so the agent integration can mature without breaking core SemVer.

The core stays dependency-free (Markdig only); the LLM SDK lives **exclusively** in this
package. You bring your own `AIAgent` (provider + credentials); DRYL wires it to the UI.

## Install

```bash
dotnet add package DRYL.Components.Agents
```

```csharp
// Program.cs — alongside the core registration
builder.Services.AddDrylComponents().AddDrylAgents();
```

Place a single `<DrylDialogProvider />` in your root layout (required by the tool dialogs).

## Platform requirement

All subsystems run in **interactive Blazor (Server or WASM) with a live circuit**.
The human-in-the-loop tools show a dialog and *await* its result while the agent run is in
flight, so **the agent run must execute in the same DI scope (circuit) as the UI**
(`IDrylDialogService` is scoped per circuit). An agent run started outside the circuit
cannot ask the user back.

## 1 — Agent run → automatic `AiState` + tool calls

Inject `DrylAgentRunner`, start a run, and wrap the region in a `DrylAiScope`. Every
AI-aware component inside switches Thinking → Streaming → Generated in lockstep — no manual
state anywhere. Tool calls surface through the core `DrylToolCall`.

```razor
@inject DrylAgentRunner Runner

<DrylAiScope Key="chat">
  <DrylAiStream Source="@_run.TextStream" Key="chat">
    <DrylMarkdown Content="@context.Text" Ai="@context.State" />
  </DrylAiStream>
  <DrylAgentToolCalls Run="@_run" />
</DrylAiScope>

@code {
    private DrylAgentRun _run = default!;

    private void Ask(string userMsg) =>
        _run = Runner.Start(_agent, _session, userMsg, aiKey: "chat");
}
```

## 2 — `DrylAiGenerate<T>` (structured streaming → UI)

The model emits JSON for `T`; a tolerant `PartialJsonReader<T>` produces a *partial snapshot*
on every chunk, so titles and text grow character by character (guided, type-as-you-go
generation). On a parse failure it holds the last good snapshot — never a flicker.

```razor
<DrylAiGenerate T="Recipe" Source="@_jsonStream" Key="recipe">
  <ChildContent Context="snap">
    <DrylCard Ai="@snap.State">
      <h3>@snap.Value?.Title</h3>
      @foreach (var step in snap.Value?.Steps ?? []) { <li>@step</li> }
    </DrylCard>
  </ChildContent>
</DrylAiGenerate>

@code {
    // Sets the JSON schema for Recipe and yields the raw token stream.
    private IAsyncEnumerable<string> _jsonStream =
        Runner.GenerateStreamingAsync<Recipe>(_agent, _session, prompt, aiKey: "recipe");
}
```

## 3 — Human-in-the-loop tool functions

Four ready-made `AIFunction` tools backed by DRYL dialogs. Hand them all to the agent, or
pick individually. Each awaits the user's answer and returns a model-friendly string; if the
user dismisses the dialog (or the run is cancelled) it returns a defined "cancelled" value
rather than throwing.

```csharp
var uiTools = DrylUiTools.Create(DialogService);   // scoped to this circuit
var agent = new ChatClientAgent(chatClient, instructions: prompt, tools: uiTools.All);
```

| Tool                  | UI                                          | Returns          |
| --------------------- | ------------------------------------------- | ---------------- |
| `AskChoice`           | `DrylAskChoiceDialog` (radio, recommended)  | chosen option    |
| `AskMultiChoice`      | `DrylAskMultiChoiceDialog` (checkboxes)     | chosen options   |
| `RequestPermission`   | core `DrylConfirmDialog`                     | allowed (bool)   |
| `AskText`             | `DrylAskTextDialog` (`DrylInputText`)        | entered text     |

## 4 — Display tools (the model answers with components)

The mirror image of the human-in-the-loop tools: six ready-made display `AIFunction`s
(`show_line_chart`, `show_area_chart`, `show_bar_chart`, `show_donut_chart`, `show_stats`,
`show_timeline`). Hand them to the agent; the model can answer with live DRYL components
inline in the conversation. The tools only validate and acknowledge — rendering is driven
from the run's tool-call trace by `DrylAgentAttachments`, so they work with `Start`,
`Replay` and the orchestrations alike. Invalid arguments never render; the model receives
a corrective error string and retries.

```csharp
var display = DrylDisplayTools.Create();          // no dependencies
var agent = new ChatClientAgent(chatClient, instructions: prompt, tools: display.All);
```

```razor
@* Smooth: some providers (e.g. Ollama) buffer a generation while parsing tool-call
   syntax and deliver it in one burst — the paced reveal keeps it reading as a stream.
   Burst-delivered attachments cascade in one by one automatically. *@
<DrylAiStream Source="@_run.TextStream" Smooth>
  <DrylMarkdown Content="@context.Text" Ai="@context.State" />
</DrylAiStream>
<DrylAgentAttachments Run="@_run" />   @* charts / stats / timeline glide in here *@
```

## 5 — Run health: errors + token usage

A faulted run settles at `AiState.None` with `Run.Error` set (message, exception type);
`UsageContent` updates are summed into `Run.Usage` as they stream. Two small components
render both without any manual wiring:

```razor
<DrylAgentError Run="@_run" OnRetry="Ask" />   @* danger alert + optional retry *@
<DrylAgentUsage Run="@_run" />                 @* prompt / completion / total badges *@
```

## 6 — Multi-agent flows → `DrylHandoffTrace`

`StartSequential` chains agents (each receives the previous answer; the flow's `TextStream`
carries the final one), `StartConcurrent` fans the same prompt out. Both return one
observable `DrylMultiAgentRun`; `DrylHandoffTrace` renders it as a living timeline — the
active lane wears the shared aura, the connector fills on handoff, failed lanes show their
error in place.

```razor
<DrylHandoffTrace Run="@_flow" />

@code {
    private DrylMultiAgentRun _flow = default!;

    private void Start() => _flow = Runner.StartSequential(new[]
    {
        new DrylAgentStep { Name = "Researcher", Agent = _researcher },
        new DrylAgentStep { Name = "Writer",     Agent = _writer },
    }, "Write about glass surfaces.");
}
```

## 7 — `DrylAiField` — AI affordance for any input

Lay it around any existing text-like input; pass only the agent. The field gets
an unobtrusive ✨ trigger: empty field → generate, selected text → transform
only the selection. The result streams live into the field, the shared ai-aura
plays, the user accepts or rejects.

```razor
<DrylAiField Agent="agent" Instruction="Formuliere professioneller">
    <DrylTextarea @bind-Value="mail.Body" />
</DrylAiField>
```

No `Instruction` (or `ShowPrompt`) opens a mini-prompt popover instead. The
wrapped component is never touched — the value bridge dispatches native
`input` events, so `@bind-Value` just works.

## Versioning & publishing

This package carries its own `Version` (starting at `0.1.0`) and is published independently
of the core. CI validates that it packs; the first NuGet publish is a maintainer action.

See the repository [`CHANGELOG.md`](https://github.com/Zimpi/DRYL.Components/blob/main/CHANGELOG.md)
for the full list of public types.
