using System.Text;
using System.Text.Json;
using DRYL.Components;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

/// <summary>An <see cref="ICommandResolver"/> that asks an <see cref="AIAgent"/> to resolve a
/// natural-language query into exactly one registered command + filled arguments — surfaced by the
/// palette as a confirmable suggestion. Each <see cref="DrylCommand"/> becomes an
/// <see cref="AIFunction"/> whose name is the command id; the function only records the selection,
/// so the model never executes anything (execution stays in the palette, after confirmation).</summary>
public sealed class DrylAiCommandResolver : ICommandResolver
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly AIAgent _agent;
    private readonly Func<CancellationToken, Task<AgentSession>>? _sessionFactory;

    /// <summary>Creates the resolver over an agent. By default each resolution runs in a fresh
    /// session (<c>agent.CreateSessionAsync</c>); pass <paramref name="sessionFactory"/> to reuse or
    /// customise sessions.</summary>
    public DrylAiCommandResolver(
        AIAgent agent,
        Func<CancellationToken, Task<AgentSession>>? sessionFactory = null)
    {
        _agent = agent;
        _sessionFactory = sessionFactory;
    }

    /// <inheritdoc />
    public async Task<CommandResolution?> ResolveAsync(
        string query, IReadOnlyList<DrylCommand> commands, CancellationToken ct)
    {
        DrylCommand? chosen = null;
        JsonElement chosenArgs = default;

        var tools = new List<AITool>();
        foreach (var command in commands.Where(c => !c.Disabled))
        {
            var c = command;
            tools.Add(AIFunctionFactory.Create(
                (JsonElement args) =>
                {
                    chosen = c;
                    chosenArgs = args.Clone();
                    return "recorded";
                },
                c.ResolvedId,
                BuildDescription(c)));
        }

        if (tools.Count == 0) return null;

        var session = _sessionFactory is not null
            ? await _sessionFactory(ct).ConfigureAwait(false)
            : await _agent.CreateSessionAsync(ct).ConfigureAwait(false);

        var runOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions =
                    "Select exactly one command that fulfils the user's request and call its " +
                    "function with filled arguments. If none fit, call nothing.",
                Tools = tools,
            }
        };

        await _agent.RunAsync(query, session, runOptions, ct).ConfigureAwait(false);

        if (chosen is null) return null;
        return new CommandResolution(chosen, ParseArguments(chosen, chosenArgs), 1.0);
    }

    private static string BuildDescription(DrylCommand c)
    {
        var sb = new StringBuilder();
        sb.Append(c.Title);
        if (!string.IsNullOrWhiteSpace(c.Description)) sb.Append(". ").Append(c.Description);
        if (c.Arguments.Count > 0)
        {
            sb.Append(" Arguments: ");
            sb.Append(string.Join(", ", c.Arguments.Select(a =>
                $"{a.Name} ({a.Type}{(a.Required ? ", required" : "")})" +
                (a.Options is { Length: > 0 } ? $" one of [{string.Join('|', a.Options)}]" : "") +
                (string.IsNullOrWhiteSpace(a.Description) ? "" : $" — {a.Description}"))));
        }
        return sb.ToString();
    }

    /// <summary>Coerces a model-produced JSON argument object into a typed dictionary using each
    /// argument's <see cref="CommandArgType"/>.</summary>
    public static IReadOnlyDictionary<string, object?> ParseArguments(DrylCommand command, JsonElement args)
    {
        var result = new Dictionary<string, object?>();
        if (args.ValueKind != JsonValueKind.Object) return result;

        foreach (var arg in command.Arguments)
        {
            if (!args.TryGetProperty(arg.Name, out var prop)) continue;
            result[arg.Name] = arg.Type switch
            {
                CommandArgType.Number => prop.ValueKind == JsonValueKind.Number
                    ? prop.GetDouble()
                    : double.TryParse(prop.GetString(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null,
                CommandArgType.Boolean => prop.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? prop.GetBoolean()
                    : bool.TryParse(prop.GetString(), out var b) ? b : null,
                _ => prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString(),
            };
        }
        return result;
    }
}
