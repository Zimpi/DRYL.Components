using System.Reflection;
using System.Text.Json;

namespace DRYL.Components.Canvas;

/// <summary>What a handler gets besides its typed parameters.</summary>
public sealed class CanvasDataContext
{
    internal CanvasDataContext(IServiceProvider services) => Services = services;

    /// <summary>
    /// The circuit's (or request's) service scope. The handler resolves its <c>DbContext</c>,
    /// <c>IHttpClientFactory</c> or the signed-in user from here — tenant and user never travel
    /// through the artifact spec, so the model cannot influence them.
    /// </summary>
    public IServiceProvider Services { get; }
}

/// <summary>One parameter of a data source, derived from the parameter record.</summary>
/// <param name="Name">camelCase name as it appears in the spec's <c>params</c> object.</param>
/// <param name="TypeName">Model-facing type name, e.g. <c>int</c>, <c>string</c>, <c>"a"|"b"</c>.</param>
/// <param name="Required">False when the record parameter is nullable or has a default.</param>
public sealed record CanvasParamInfo(string Name, string TypeName, bool Required);

/// <summary>Everything the model, the validator and the binder need to know about one source —
/// and deliberately nothing more: never a row, never a personal record (A3).</summary>
public sealed record CanvasDataDescriptor(
    string Name, string Description, CanvasDataShape Shape, IReadOnlyList<CanvasParamInfo> Params);

/// <summary>A registered source: its descriptor plus the invoker that runs the host's handler.</summary>
public sealed class CanvasDataSource
{
    internal CanvasDataSource(CanvasDataDescriptor descriptor,
                              Func<JsonElement?, IServiceProvider, CancellationToken, Task<CanvasData>> invoke)
    {
        Descriptor = descriptor;
        Invoke = invoke;
    }

    /// <summary>Name, description, result shape and parameter schema.</summary>
    public CanvasDataDescriptor Descriptor { get; }

    internal Func<JsonElement?, IServiceProvider, CancellationToken, Task<CanvasData>> Invoke { get; }
}

/// <summary>
/// The application-wide set of named canvas data sources, filled at startup by
/// <c>AddDrylCanvasDataSource</c>. Registered as a singleton; the per-scope
/// <see cref="ICanvasDataService"/> runs its entries against the current scope.
/// </summary>
public sealed class CanvasDataRegistry
{
    private readonly Dictionary<string, CanvasDataSource> _sources = new(StringComparer.Ordinal);
    private readonly List<CanvasDataDescriptor> _descriptors = new();

    /// <summary>Every registered source, in registration order.</summary>
    public IReadOnlyList<CanvasDataDescriptor> Descriptors => _descriptors;

    /// <summary>Looks up a source by its registered name.</summary>
    public bool TryGet(string name, out CanvasDataSource source) => _sources.TryGetValue(name, out source!);

    internal void Add(CanvasDataSource source)
    {
        if (!_sources.TryAdd(source.Descriptor.Name, source))
            throw new InvalidOperationException(
                $"A canvas data source named '{source.Descriptor.Name}' is already registered.");
        _descriptors.Add(source.Descriptor);
    }
}

/// <summary>
/// Derives a source's parameter schema from the primary constructor of its parameter record.
/// One place of truth: the host writes the record, the model reads the derived schema, and the
/// handler gets IntelliSense — nobody has to keep a prose description in step.
/// </summary>
internal static class CanvasParamSchema
{
    private static readonly NullabilityInfoContext Nullability = new();

    public static IReadOnlyList<CanvasParamInfo> Describe(Type paramsType)
    {
        // Records put their positional parameters on the primary constructor; for a plain class
        // the constructor with the most parameters is the one that carries the schema.
        var ctor = paramsType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new ArgumentException(
                $"Canvas data source parameter type '{paramsType.Name}' has no public constructor — " +
                "use a record with a primary constructor.");

        var parameters = ctor.GetParameters();
        if (parameters.Length == 0)
            throw new ArgumentException(
                $"Canvas data source parameter type '{paramsType.Name}' has no constructor parameters — " +
                "use the parameterless AddDrylCanvasDataSource overload instead.");

        var result = new List<CanvasParamInfo>(parameters.Length);
        foreach (var p in parameters)
        {
            if (!IsSupported(p.ParameterType, out var typeName))
                throw new ArgumentException(
                    $"Canvas data source parameter '{p.Name}' of type '{p.ParameterType.Name}' is not supported. " +
                    "Use string, int, long, double, decimal, bool, DateOnly, DateTime, Guid, an enum, " +
                    "a nullable of those, or IReadOnlyList<T> of those.");

            result.Add(new CanvasParamInfo(Camel(p.Name!), typeName, Required(p)));
        }
        return result;
    }

    // Optional means "the host said it may be absent": a default value, or a nullable type.
    // Nullable value types are visible on the type itself; nullable reference types only through
    // the compiler's nullability metadata, which is what NullabilityInfoContext reads.
    private static bool Required(ParameterInfo p)
    {
        if (p.HasDefaultValue) return false;
        if (Nullable.GetUnderlyingType(p.ParameterType) is not null) return false;
        if (!p.ParameterType.IsValueType &&
            Nullability.Create(p).WriteState == NullabilityState.Nullable) return false;
        return true;
    }

    private static bool IsSupported(Type type, out string typeName)
    {
        typeName = string.Empty;
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (ListElement(t) is { } element)
        {
            if (!IsScalar(element, out var elementName)) return false;
            typeName = elementName + "[]";
            return true;
        }

        if (!IsScalar(t, out typeName)) return false;
        return true;
    }

    private static Type? ListElement(Type t)
    {
        if (t.IsArray) return t.GetElementType();
        if (!t.IsGenericType) return null;
        var def = t.GetGenericTypeDefinition();
        return def == typeof(IReadOnlyList<>) || def == typeof(IList<>) ||
               def == typeof(List<>) || def == typeof(IEnumerable<>)
            ? t.GetGenericArguments()[0]
            : null;
    }

    private static bool IsScalar(Type t, out string typeName)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;

        if (t.IsEnum)
        {
            // The model needs the allowed literals, not the CLR type name — an enum is a
            // closed set, and spelling it out is the cheapest way to stop it guessing.
            typeName = string.Join("|", Enum.GetNames(t).Select(n => $"\"{Camel(n)}\""));
            return true;
        }

        typeName = Type.GetTypeCode(t) switch
        {
            TypeCode.String => "string",
            TypeCode.Int32 => "int",
            TypeCode.Int64 => "long",
            TypeCode.Double => "number",
            TypeCode.Decimal => "decimal",
            TypeCode.Boolean => "bool",
            TypeCode.DateTime => "datetime",
            _ when t == typeof(DateOnly) => "date",
            _ when t == typeof(Guid) => "guid",
            _ => string.Empty,
        };
        return typeName.Length > 0;
    }

    internal static string Camel(string name) =>
        name.Length == 0 || char.IsLower(name[0])
            ? name
            : char.ToLowerInvariant(name[0]) + name[1..];
}
