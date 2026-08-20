using System.Runtime.CompilerServices;
using Bunit;

namespace DRYL.Components.Tests;

/// <summary>
/// Suite-wide bUnit defaults, applied once as the test assembly loads.
/// </summary>
internal static class TestDefaults
{
    /// <summary>
    /// How long a <c>WaitForAssertion</c> / <c>WaitForState</c> may wait before
    /// it gives up.
    /// </summary>
    /// <remarks>
    /// bUnit's own default is one second, and 88 of this suite's 95 wait calls
    /// took it implicitly. One second is not a statement about any of those
    /// tests — it is a number nobody chose, and it is thin for the things this
    /// suite waits on: a streamed <c>IAsyncEnumerable</c> draining, a canvas
    /// re-render, a <c>DrylPresence</c> exit watchdog. On a loaded CI agent that
    /// is how a green test turns red for a reason that has nothing to do with
    /// the code.
    /// <para>
    /// Three seconds is not a licence to be slow: <c>WaitForAssertion</c>
    /// returns the moment its assertion passes, so a raised ceiling costs a
    /// passing suite nothing at all and is only ever spent by a test that was
    /// going to fail anyway. Measured either way, the suite runs in the same
    /// four to five seconds.
    /// </para>
    /// <para>
    /// It is set here, once, rather than written onto every call site: the
    /// value is a property of the environment the suite runs in, not of any
    /// individual assertion, and a per-call number repeated 95 times only
    /// invites drift. The seven call sites that already pass
    /// <c>TimeSpan.FromSeconds(3)</c> explicitly now agree with the default and
    /// are left alone; a test that genuinely needs a different bound still says
    /// so at its own call site and overrides this.
    /// </para>
    /// </remarks>
    [ModuleInitializer]
    internal static void Apply()
        => BunitContext.DefaultWaitTimeout = TimeSpan.FromSeconds(3);
}

/// <summary>
/// One fact for <see cref="TestDefaults"/>, because a suite that stays green is
/// exactly what it would look like if the module initializer never ran at all.
/// </summary>
public class TestDefaultsTests
{
    [Fact]
    public void The_suite_wide_wait_timeout_is_applied_before_any_test_runs()
        => Assert.Equal(TimeSpan.FromSeconds(3), BunitContext.DefaultWaitTimeout);
}
