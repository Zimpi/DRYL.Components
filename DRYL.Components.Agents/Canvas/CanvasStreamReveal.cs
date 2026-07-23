using System.Text.Json;

namespace DRYL.Components.Agents;

/// <summary>
/// Incrementally reveals a streaming <c>create_artifact</c> snapshot into a live <see cref="CanvasSpec"/>,
/// one <em>complete</em> node at a time, so the artifact fades in element by element instead of flickering
/// as a half-parsed tree.
///
/// <para>Within every container the currently-streaming last child is treated as unfinished: a leaf tail is
/// withheld until it completes (so it never renders as a "waiting…" skeleton), while a container tail is
/// surfaced as an empty shell and filled recursively as its own children finish. A node counts as complete
/// once a later sibling has begun (or the stream has ended) — the same "all but the last" guarantee the
/// update path relies on.</para>
///
/// <para>Already-revealed nodes are <em>frozen</em>: their instances are added by reference and never
/// replaced, so settled parts of the artifact keep the same object identity across snapshots and Blazor
/// skips re-rendering them (no re-flicker, no replayed reveal animation) while later nodes stream in.</para>
/// </summary>
internal static class CanvasStreamReveal
{
    /// <summary>
    /// Merges <paramref name="snapshot"/> into <paramref name="live"/>. When <paramref name="streamDone"/>
    /// is false the trailing (still-streaming) node at each level is withheld or shown as a shell; when true
    /// every node is flushed. Returns true if a new node was revealed (or the title grew) — i.e. the canvas
    /// should re-render.
    /// </summary>
    public static bool Reveal(CanvasSpec live, CanvasSpec snapshot, bool streamDone)
    {
        var changed = false;

        if (snapshot.Title is { Length: > 0 } title && title != live.Title)
        {
            live.Title = title;
            changed = true;
        }

        var snapRoot = snapshot.Root;
        if (snapRoot is null || !HasSealedIdentity(snapRoot))
            return changed;

        if (live.Root is null || live.Root.Id != snapRoot.Id)
        {
            // Seed the root shell (root is a container by convention); its children reveal incrementally.
            live.Root = Shell(snapRoot);
            changed = true;
        }
        else if (!streamDone && PropsDiffer(live.Root.Props, snapRoot.Props))
        {
            live.Root.Props = snapRoot.Props;
            changed = true;
        }

        // The root is the outermost still-streaming container until the whole stream ends.
        changed |= RevealChildren(live.Root, snapRoot, streamDone);
        return changed;
    }

    private static bool RevealChildren(CanvasNode liveParent, CanvasNode snapParent, bool streamDone)
    {
        var snapKids = snapParent.Children;
        if (snapKids is null || snapKids.Count == 0) return false;

        liveParent.Children ??= new List<CanvasNode>();
        var changed = false;

        // Every child before the last is fully streamed; when the stream ends, the last one is too.
        var completeCount = streamDone ? snapKids.Count : snapKids.Count - 1;
        for (var i = 0; i < completeCount; i++)
        {
            var s = snapKids[i];
            if (!Revealable(s)) break;   // no id yet — can't key it; later siblings aren't meaningful either

            var existing = Find(liveParent.Children, s.Id);
            if (existing is null)
            {
                liveParent.Children.Add(s);   // freeze the whole complete subtree by reference
                changed = true;
            }
            else
            {
                // A container we had shown as a still-filling shell has now completed: flush any child it
                // was still withholding, then it is sealed (this recursion is idempotent thereafter).
                changed |= RevealChildren(existing, s, streamDone: true);
            }
        }

        // The still-streaming last child: reveal a container shell early so its finished inner children can
        // appear; withhold a leaf until it too is complete (avoids the "waiting for…" skeleton flicker).
        if (!streamDone)
        {
            var tail = snapKids[^1];
            if (HasSealedIdentity(tail) && CanvasCatalog.IsContainer(tail.Type))
            {
                var liveTail = Find(liveParent.Children, tail.Id);
                if (liveTail is null)
                {
                    liveTail = Shell(tail);
                    liveParent.Children.Add(liveTail);
                    changed = true;
                }
                else if (PropsDiffer(liveTail.Props, tail.Props))
                {
                    liveTail.Props = tail.Props;
                    changed = true;
                }

                changed |= RevealChildren(liveTail, tail, streamDone: false);
            }
        }

        return changed;
    }

    // A fully-streamed (complete) node — one a later sibling already follows — has its whole object closed,
    // so its id + type strings are final. This gates the append-by-reference of complete children.
    private static bool Revealable(CanvasNode n) =>
        !string.IsNullOrEmpty(n.Id) && !string.IsNullOrEmpty(n.Type);

    // A node we seed as a still-filling shell isn't closed yet, so its streamed id/type could be a partial
    // string ("stac" for "stack"). They're only trustworthy once the model has moved *past* type — i.e. the
    // props or children that follow it in the object have started. Gate shell seeding on that.
    private static bool HasSealedIdentity(CanvasNode n) =>
        !string.IsNullOrEmpty(n.Id) && !string.IsNullOrEmpty(n.Type)
        && (n.Props is not null || n.Children is not null);

    // A live-owned container node whose Children list we grow ourselves across snapshots.
    private static CanvasNode Shell(CanvasNode n) =>
        new() { Id = n.Id, Type = n.Type, Props = n.Props };

    private static bool PropsDiffer(JsonElement? a, JsonElement? b) =>
        RawText(a) != RawText(b);

    private static string RawText(JsonElement? el) =>
        el is { } e && e.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            ? e.GetRawText()
            : string.Empty;

    private static CanvasNode? Find(List<CanvasNode> nodes, string id)
    {
        foreach (var n in nodes)
            if (n.Id == id) return n;
        return null;
    }
}
