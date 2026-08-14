using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>
/// The schema derivation runs while the host is being built. A test host, a WebAssembly host and
/// a server host can all be built at the same moment in one process (any test suite that spins up
/// several <c>WebApplicationFactory</c> instances does exactly that), so <c>Describe</c> has to
/// survive being called for the same parameter record from several threads at once.
/// </summary>
public class CanvasParamSchemaConcurrencyTests
{
    // A generic record gives us a fresh closed type per type argument, and every closed type is a
    // fresh cache key inside NullabilityInfoContext — one unheated race per type, which is exactly
    // what the bug needs to show itself.
    public sealed record RaceParams<T>(string Order);

    private static Type[] DistinctParamTypes(int count)
    {
        var types = new Type[count];
        var arg = typeof(int);
        for (var i = 0; i < count; i++)
        {
            types[i] = typeof(RaceParams<>).MakeGenericType(arg);
            arg = arg.MakeArrayType();
        }
        return types;
    }

    [Fact]
    public void Describe_is_safe_when_two_hosts_start_at_the_same_moment()
    {
        // Four workers, not one per core: the rest of the suite runs in parallel with this test,
        // and two threads are already enough to lose the race — saturating the machine would only
        // make the bUnit tests next door time out.
        const int workers = 4;
        var types = DistinctParamTypes(384);
        var errors = new System.Collections.Concurrent.ConcurrentQueue<Exception>();

        using var barrier = new Barrier(workers);
        var threads = new Thread[workers];
        for (var w = 0; w < workers; w++)
        {
            threads[w] = new Thread(() =>
            {
                foreach (var paramsType in types)
                {
                    barrier.SignalAndWait();
                    try { CanvasParamSchema.Describe(paramsType); }
                    catch (Exception ex) { errors.Enqueue(ex); }
                }
            }) { IsBackground = true };
            threads[w].Start();
        }
        foreach (var t in threads) t.Join();

        Assert.True(errors.IsEmpty,
            $"{errors.Count} concurrent Describe calls threw; first: {errors.FirstOrDefault()}");
    }

    // The stress test above needs to win a race to fail, so on its own it would let a revert
    // through some of the time. This one catches the shape itself and never flakes: a
    // NullabilityInfoContext kept in a static field is shared across every caller in the process,
    // and that is precisely what the type may not be used for.
    [Fact]
    public void No_NullabilityInfoContext_is_shared_in_a_static_field()
    {
        var shared = typeof(CanvasParamSchema)
            .GetFields(System.Reflection.BindingFlags.Static |
                       System.Reflection.BindingFlags.Public |
                       System.Reflection.BindingFlags.NonPublic)
            .Where(f => typeof(System.Reflection.NullabilityInfoContext).IsAssignableFrom(f.FieldType))
            .Select(f => f.Name)
            .ToList();

        Assert.True(shared.Count == 0,
            $"NullabilityInfoContext is not thread safe and must not live in a static field: " +
            $"{string.Join(", ", shared)}");
    }
}
