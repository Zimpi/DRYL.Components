namespace DRYL.Components.Agents;

/// <summary>
/// Observable handle to a running agent. Drives <see cref="DrylRunBase.State"/> automatically and
/// exposes the accumulated <see cref="DrylRunBase.Text"/>, the live <see cref="DrylRunBase.ToolCalls"/>
/// trace, and a <see cref="DrylRunBase.TextStream"/> ready to drop into <c>DrylAiStream</c>/<c>DrylMarkdown</c>.
/// </summary>
public sealed class DrylAgentRun : DrylRunBase
{
}
