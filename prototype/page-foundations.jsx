/* DRYL — Foundations */

function PageFoundations() {
  const colorTokens = [
    { name: "bg-0", val: "#000000", style: { background: "#000000" } },
    { name: "bg-1", val: "#07070a", style: { background: "#07070a" } },
    { name: "bg-2", val: "#0c0c12", style: { background: "#0c0c12" } },
    { name: "bg-3", val: "#14141c", style: { background: "#14141c" } },
    { name: "line",        val: "rgba(255,255,255,.06)", style: { background: "rgba(255,255,255,0.06)" } },
    { name: "line-strong", val: "rgba(255,255,255,.12)", style: { background: "rgba(255,255,255,0.12)" } },
  ];
  const accents = [
    { name: "accent-a", val: "#7c5cff", style: { background: "#7c5cff", boxShadow: "0 0 32px rgba(124,92,255,0.5)" } },
    { name: "accent-b", val: "#22d3ee", style: { background: "#22d3ee", boxShadow: "0 0 32px rgba(34,211,238,0.5)" } },
    { name: "accent-grad", val: "violet → cyan", style: { background: "linear-gradient(135deg,#7c5cff,#22d3ee)", boxShadow: "0 0 32px rgba(124,92,255,0.4)" } },
  ];
  const semantic = [
    { name: "success", val: "#34d399", style: { background: "#34d399", boxShadow: "0 0 24px rgba(52,211,153,0.4)" } },
    { name: "warning", val: "#fbbf24", style: { background: "#fbbf24", boxShadow: "0 0 24px rgba(251,191,36,0.4)" } },
    { name: "danger",  val: "#f87171", style: { background: "#f87171", boxShadow: "0 0 24px rgba(248,113,113,0.4)" } },
    { name: "info",    val: "#22d3ee", style: { background: "#22d3ee", boxShadow: "0 0 24px rgba(34,211,238,0.4)" } },
  ];

  const typeSamples = [
    { label: "Display",  cls: "", size: "56 / 700 / -3.5%", el: <h1 style={{ fontSize: 56 }}>The future is dark.</h1> },
    { label: "Title",    cls: "", size: "32 / 600 / -2.5%", el: <h2>Build with intent.</h2> },
    { label: "Heading",  cls: "", size: "20 / 600 / -2%",   el: <h3>Section heading</h3> },
    { label: "Body",     cls: "", size: "14 / 400",         el: <p style={{ color: "var(--fg)" }}>Wir bauen Werkzeuge, die sich wie zuhause anfühlen — präzise, ruhig, lebendig.</p> },
    { label: "Caption",  cls: "", size: "12 / 400",         el: <p style={{ fontSize: 12 }}>Aktualisiert vor 2 Minuten · von Daniel</p> },
    { label: "Mono",     cls: "mono", size: "13 / mono", el: <code style={{ fontSize: 13 }}>--accent-a: #7c5cff;</code> },
  ];

  const spacing = [4, 8, 12, 16, 24, 32, 48, 64];
  const radii = [
    { name: "r-xs", v: 6 },
    { name: "r-sm", v: 10 },
    { name: "r-md", v: 14 },
    { name: "r-lg", v: 20 },
    { name: "r-xl", v: 28 },
    { name: "r-pill", v: 999 },
  ];

  return (
    <div className="fade-in">
      <div style={{ marginBottom: 48 }}>
        <div className="eyebrow" style={{ marginBottom: 12 }}>Foundations</div>
        <h1 style={{ fontSize: 44, marginBottom: 14 }}>Tokens & Primitives</h1>
        <p className="lead">Die atomare Sprache. Alle Komponenten lesen aus diesen Werten — anpassen heißt einmal an einer Stelle ändern.</p>
      </div>

      {/* Colors */}
      <section className="section">
        <div className="section-head">
          <div>
            <div className="eyebrow" style={{ marginBottom: 6 }}>Color · Surfaces</div>
            <h2>Schwarz, geschichtet</h2>
            <p style={{ marginTop: 8 }}>Reines Schwarz als Boden. Jede Ebene darüber gewinnt Lichtdurchlass, nicht Helligkeit.</p>
          </div>
        </div>
        <div className="swatch-grid stagger">
          {colorTokens.map((t) => (
            <div key={t.name} className="swatch" style={t.style}>
              <div className="name">{t.name}</div>
              <div className="val">{t.val}</div>
            </div>
          ))}
        </div>
      </section>

      <section className="section">
        <div className="section-head">
          <div>
            <div className="eyebrow" style={{ marginBottom: 6 }}>Color · Accents</div>
            <h2>Der Glow</h2>
            <p style={{ marginTop: 8 }}>Violett zu Cyan als Hauptachse. Jeder Akzent trägt seinen eigenen Lichtkegel.</p>
          </div>
        </div>
        <div className="swatch-grid stagger">
          {accents.map((t) => (
            <div key={t.name} className="swatch" style={t.style}>
              <div className="name" style={{ color: "white" }}>{t.name}</div>
              <div className="val" style={{ color: "rgba(255,255,255,0.85)" }}>{t.val}</div>
            </div>
          ))}
        </div>
      </section>

      <section className="section">
        <div className="section-head">
          <div>
            <div className="eyebrow" style={{ marginBottom: 6 }}>Color · Semantic</div>
            <h2>Bedeutung</h2>
          </div>
        </div>
        <div className="swatch-grid stagger">
          {semantic.map((t) => (
            <div key={t.name} className="swatch" style={t.style}>
              <div className="name" style={{ color: "black" }}>{t.name}</div>
              <div className="val" style={{ color: "rgba(0,0,0,0.7)" }}>{t.val}</div>
            </div>
          ))}
        </div>
      </section>

      {/* Type */}
      <section className="section">
        <div className="section-head">
          <div>
            <div className="eyebrow" style={{ marginBottom: 6 }}>Typography</div>
            <h2>Inter, mit Atem</h2>
            <p style={{ marginTop: 8 }}>Eine Familie für alles. Negatives Letter-Spacing in den großen Größen für ein dichtes, präzises Bild.</p>
          </div>
        </div>
        <SpotlightCard style={{ padding: 28 }}>
          {typeSamples.map((s, i) => (
            <div key={i} className="type-row">
              <div className="label">{s.label}</div>
              <div className={s.cls}>{s.el}</div>
              <div className="spec">{s.size}</div>
            </div>
          ))}
        </SpotlightCard>
      </section>

      {/* Spacing */}
      <section className="section">
        <div className="section-head">
          <div>
            <div className="eyebrow" style={{ marginBottom: 6 }}>Spacing</div>
            <h2>4er-Raster</h2>
            <p style={{ marginTop: 8 }}>Acht Stufen. Mehr braucht es nicht.</p>
          </div>
        </div>
        <SpotlightCard style={{ padding: 28, display: "flex", alignItems: "flex-end", gap: 18 }}>
          {spacing.map((v) => (
            <div key={v} style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
              <div style={{
                width: v, height: v,
                background: "var(--accent-grad)",
                borderRadius: 4,
                boxShadow: "0 0 16px rgba(124,92,255,0.4)",
              }}/>
              <div className="mono" style={{ fontSize: 10.5, color: "var(--fg-dim)" }}>{v}</div>
            </div>
          ))}
        </SpotlightCard>
      </section>

      {/* Radii */}
      <section className="section">
        <div className="section-head">
          <div>
            <div className="eyebrow" style={{ marginBottom: 6 }}>Radii</div>
            <h2>Ecken</h2>
            <p style={{ marginTop: 8 }}>Konsistent, mit einem klaren Sprung pro Stufe.</p>
          </div>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(140px, 1fr))", gap: 14 }} className="stagger">
          {radii.map((r) => (
            <SpotlightCard key={r.name} style={{ padding: 16, display: "flex", flexDirection: "column", alignItems: "center", gap: 12 }}>
              <div style={{
                width: 80, height: 60,
                background: "linear-gradient(135deg, rgba(124,92,255,0.5), rgba(34,211,238,0.4))",
                borderRadius: r.v === 999 ? r.v : r.v,
                boxShadow: "inset 0 1px 0 rgba(255,255,255,0.2)",
              }}/>
              <div style={{ textAlign: "center" }}>
                <div className="mono" style={{ fontSize: 11, color: "var(--fg)" }}>--{r.name}</div>
                <div className="mono" style={{ fontSize: 10.5, color: "var(--fg-dim)" }}>{r.v === 999 ? "pill" : r.v + "px"}</div>
              </div>
            </SpotlightCard>
          ))}
        </div>
      </section>

      {/* Motion */}
      <section className="section">
        <div className="section-head">
          <div>
            <div className="eyebrow" style={{ marginBottom: 6 }}>Motion</div>
            <h2>Easing & Dauer</h2>
            <p style={{ marginTop: 8 }}>Drei Geschwindigkeiten. Drei Kurven. Mehr braucht ein gutes UI selten.</p>
          </div>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 14 }} className="stagger">
          {[
            { name: "ease-out",    spec: "(0.16, 1, 0.3, 1)",      desc: "Exits, Einblendungen" },
            { name: "ease-in-out", spec: "(0.65, 0, 0.35, 1)",     desc: "Layout-Wechsel" },
            { name: "ease-spring", spec: "(0.34, 1.56, 0.64, 1)",  desc: "Toggles, Pings" },
          ].map((m) => (
            <SpotlightCard key={m.name} style={{ padding: 22 }}>
              <div className="mono" style={{ fontSize: 11, color: "var(--accent-b)", marginBottom: 6 }}>--{m.name}</div>
              <div className="mono" style={{ fontSize: 11.5, color: "var(--fg-dim)", marginBottom: 14 }}>{m.spec}</div>
              <div style={{ fontSize: 13, color: "var(--fg-muted)" }}>{m.desc}</div>
              <MotionDemo cssEase={`var(--${m.name})`} />
            </SpotlightCard>
          ))}
        </div>
      </section>

      {/* Shadows / glow */}
      <section className="section">
        <div className="section-head">
          <div>
            <div className="eyebrow" style={{ marginBottom: 6 }}>Light · Shadows</div>
            <h2>Indirektes Licht</h2>
            <p style={{ marginTop: 8 }}>Schatten beschreiben Tiefe. Glühen beschreibt Energie.</p>
          </div>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))", gap: 16 }} className="stagger">
          {[
            { name: "shadow-sm", style: { boxShadow: "0 1px 2px rgba(0,0,0,0.4)" } },
            { name: "shadow-md", style: { boxShadow: "0 8px 24px rgba(0,0,0,0.45), 0 2px 6px rgba(0,0,0,0.35)" } },
            { name: "shadow-lg", style: { boxShadow: "0 24px 64px rgba(0,0,0,0.55), 0 8px 16px rgba(0,0,0,0.35)" } },
            { name: "glow-accent", style: { boxShadow: "0 0 0 1px rgba(124,92,255,0.45), 0 8px 32px rgba(124,92,255,0.35), 0 0 64px rgba(34,211,238,0.18)" } },
          ].map((s) => (
            <div key={s.name} style={{
              padding: 28,
              borderRadius: 16,
              background: "linear-gradient(180deg, rgba(255,255,255,0.04), rgba(255,255,255,0.01))",
              border: "1px solid var(--line)",
              ...s.style,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              minHeight: 130,
              flexDirection: "column",
              gap: 8,
            }}>
              <div className="mono" style={{ fontSize: 11, color: "var(--fg)" }}>--{s.name}</div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}

function MotionDemo({ cssEase }) {
  const [on, setOn] = useState(false);
  useEffect(() => {
    const t = setInterval(() => setOn((s) => !s), 2400);
    return () => clearInterval(t);
  }, []);
  return (
    <div style={{
      marginTop: 16,
      height: 36,
      borderRadius: 999,
      background: "rgba(255,255,255,0.04)",
      border: "1px solid var(--line)",
      position: "relative",
      overflow: "hidden",
    }}>
      <div style={{
        position: "absolute",
        top: 4, bottom: 4,
        left: on ? "calc(100% - 32px)" : 4,
        width: 28,
        borderRadius: 999,
        background: "linear-gradient(135deg, #7c5cff, #22d3ee)",
        boxShadow: "0 0 16px rgba(124,92,255,0.5)",
        transition: `left 800ms ${cssEase}`,
      }}/>
    </div>
  );
}

window.PageFoundations = PageFoundations;
