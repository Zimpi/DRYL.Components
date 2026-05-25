/* DRYL — Overview / Landing page */

function PageOverview({ go }) {
  return (
    <div className="fade-in">
      {/* Hero */}
      <div style={{
        position: "relative",
        padding: "64px 0 80px",
        marginBottom: 24,
      }}>
        {/* Hero glow */}
        <div style={{
          position: "absolute",
          inset: "-40px -120px 0",
          background: "radial-gradient(60% 70% at 30% 30%, rgba(124,92,255,0.18), transparent 70%), radial-gradient(50% 50% at 80% 60%, rgba(34,211,238,0.12), transparent 70%)",
          filter: "blur(20px)",
          pointerEvents: "none",
          zIndex: -1,
        }} />

        <div className="row" style={{ marginBottom: 22, gap: 10 }}>
          <span className="badge badge-accent badge-dot">v 0.1.0 · alpha</span>
          <span className="badge">Blazor Server</span>
          <span className="badge">.NET 9</span>
        </div>

        <h1 style={{ maxWidth: 18 + "ch", marginBottom: 24 }}>
          The base layer for <span className="gradient-text">everything you build.</span>
        </h1>

        <p className="lead" style={{ maxWidth: "60ch", marginBottom: 32, fontSize: 18 }}>
          DRYL Development ist ein Design-System für moderne, dunkle, lebendige Web-Tools.
          Glas, indirektes Licht, weiche Bewegung — gebaut für Blazor Server, aber framework-agnostisch im Kern.
        </p>

        <div className="row" style={{ gap: 12 }}>
          <button className="btn btn-primary btn-lg" onClick={() => go('components')}>
            <Icons.Sparkle size={15}/> Komponenten erkunden
          </button>
          <button className="btn btn-secondary btn-lg" onClick={() => go('showcase')}>
            Live-Showcase ansehen <Icons.ArrowRight size={15}/>
          </button>
        </div>
      </div>

      {/* Pillars */}
      <div className="section">
        <div className="section-head">
          <div>
            <div className="eyebrow" style={{ marginBottom: 8 }}>Prinzipien</div>
            <h2>Vier Säulen</h2>
          </div>
        </div>

        <div style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(240px, 1fr))",
          gap: 16,
        }} className="stagger">
          {[
            { icon: <Icons.Layers size={20}/>, title: "Glas, nicht Plastik", body: "Mehrlagige Transparenz mit echtem Backdrop-Blur. Jede Fläche kommuniziert ihre Tiefe." },
            { icon: <Icons.Bolt size={20}/>,   title: "Indirektes Licht",   body: "Akzente glühen, statt zu schreien. Aurora, Spotlights und sanfte Ringe sind das Vokabular." },
            { icon: <Icons.Activity size={20}/>, title: "Bewegung mit Sinn", body: "Easing-Kurven mit Charakter. Kein Bouncen ohne Grund, kein Fade ohne Funktion." },
            { icon: <Icons.Code size={20}/>,   title: "Blazor-first",       body: "Komponenten und Tokens sind so geformt, dass sich Razor-Syntax natürlich anschmiegt." },
          ].map((p, i) => (
            <SpotlightCard key={i} style={{ padding: 24 }}>
              <div style={{
                width: 40, height: 40, borderRadius: 12,
                background: "var(--accent-soft)",
                color: "#c4b5fd",
                display: "grid", placeContent: "center",
                marginBottom: 16,
                border: "1px solid var(--accent-line)",
                boxShadow: "0 0 24px rgba(124,92,255,0.25)",
              }}>{p.icon}</div>
              <h3 style={{ marginBottom: 6 }}>{p.title}</h3>
              <p style={{ fontSize: 13.5, lineHeight: 1.55 }}>{p.body}</p>
            </SpotlightCard>
          ))}
        </div>
      </div>

      {/* Quick-nav */}
      <div className="section">
        <div className="section-head">
          <div>
            <div className="eyebrow" style={{ marginBottom: 8 }}>System</div>
            <h2>Was drin ist</h2>
            <p style={{ marginTop: 8 }}>Vier Bereiche. Eine konsistente Sprache. Direkt in jedem neuen Blazor-Projekt einsetzbar.</p>
          </div>
        </div>

        <div style={{
          display: "grid",
          gridTemplateColumns: "repeat(2, 1fr)",
          gap: 16,
        }} className="stagger">
          {[
            { id: "foundations", icon: <Icons.Palette size={18}/>, title: "Foundations", body: "Color, Typografie, Spacing, Motion und Lichtquellen.", count: "32 Tokens" },
            { id: "components",  icon: <Icons.Box size={18}/>,     title: "Components",  body: "Buttons, Inputs, Cards, Nav, Daten, Feedback, Charts, Loading.", count: "42 Bausteine" },
            { id: "showcase",    icon: <Icons.Chart size={18}/>,   title: "Live Showcase", body: "Ein vollständiges Dashboard, das alle Tokens und Komponenten im Kontext zeigt.", count: "1 App" },
            { id: "blazor",      icon: <Icons.Code size={18}/>,    title: "Blazor Patterns", body: "Razor-Snippets, Komponenten-Namen und CSS-Isolation für saubere Integration.", count: "12 Patterns" },
          ].map((c) => (
            <SpotlightCard key={c.id} onClick={() => go(c.id)} style={{ padding: 24, cursor: "pointer" }}>
              <div className="between" style={{ marginBottom: 18 }}>
                <div style={{
                  width: 36, height: 36, borderRadius: 10,
                  background: "var(--glass-2)",
                  border: "1px solid var(--line-strong)",
                  display: "grid", placeContent: "center",
                  color: "var(--fg)",
                }}>{c.icon}</div>
                <span className="mono" style={{ fontSize: 11, color: "var(--fg-dim)" }}>{c.count}</span>
              </div>
              <h3 style={{ marginBottom: 4 }}>{c.title}</h3>
              <p style={{ fontSize: 13.5, marginBottom: 16 }}>{c.body}</p>
              <div className="row" style={{ color: "var(--accent-b)", fontSize: 12, fontWeight: 500 }}>
                Öffnen <Icons.ArrowRight size={13}/>
              </div>
            </SpotlightCard>
          ))}
        </div>
      </div>

      {/* Quote / philosophy */}
      <div className="section">
        <SpotlightCard style={{ padding: "44px 48px" }}>
          <div className="eyebrow" style={{ marginBottom: 18 }}>Philosophie</div>
          <p style={{
            fontSize: 24,
            lineHeight: 1.4,
            color: "var(--fg)",
            maxWidth: "62ch",
            fontWeight: 400,
            letterSpacing: "-0.015em",
          }}>
            „Ein Tool sollte sich anfühlen wie ein Werkzeug aus dem Studio eines Architekten —
            schwer, präzise, ruhig beleuchtet. Nichts schreit. Alles antwortet."
          </p>
          <div className="row" style={{ marginTop: 24, gap: 12 }}>
            <div className="avatar">D</div>
            <div>
              <div style={{ fontSize: 13, fontWeight: 500 }}>DRYL Development</div>
              <div style={{ fontSize: 11, color: "var(--fg-dim)" }} className="mono">DESIGN PRINCIPLES · 2026</div>
            </div>
          </div>
        </SpotlightCard>
      </div>
    </div>
  );
}

window.PageOverview = PageOverview;
