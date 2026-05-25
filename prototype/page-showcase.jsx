/* DRYL — Showcase: an "Internal Tools" dashboard mock */

function PageShowcase() {
  const labels = ["00", "04", "08", "12", "16", "20", "Now"];
  const series = [
    { name: "Sessions", color: "#7c5cff", data: [140, 220, 180, 310, 280, 420, 480] },
    { name: "API Calls", color: "#22d3ee", data: [80, 120, 95, 180, 160, 240, 290] },
  ];

  return (
    <div className="fade-in">
      {/* Header */}
      <div className="between" style={{ marginBottom: 30 }}>
        <div>
          <div className="row" style={{ gap: 10, marginBottom: 10 }}>
            <span className="eyebrow">Showcase</span>
            <span style={{ color: "var(--fg-faint)" }}>·</span>
            <span className="mono" style={{ fontSize: 11, color: "var(--fg-dim)" }}>DRYL.OPS.DASHBOARD</span>
          </div>
          <h1 style={{ fontSize: 40, marginBottom: 6 }}>Ops Control</h1>
          <p className="lead" style={{ fontSize: 14 }}>Eine echte App, gebaut aus reinen Tokens. Klick dich durch — alles ist live.</p>
        </div>
        <div className="row" style={{ gap: 10 }}>
          <div className="row" style={{
            padding: "0 12px", height: 34,
            border: "1px solid var(--line)",
            borderRadius: "var(--r-md)",
            background: "var(--glass-1)",
            fontSize: 12,
            color: "var(--fg-muted)",
            gap: 8,
          }}>
            <span style={{ width: 6, height: 6, borderRadius: 99, background: "#34d399", boxShadow: "0 0 8px #34d399" }}/>
            All systems operational
          </div>
          <button className="btn btn-secondary"><Icons.Download size={14}/> Export</button>
          <button className="btn btn-primary"><Icons.Plus size={14}/> New Service</button>
        </div>
      </div>

      {/* Stat row */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 14, marginBottom: 14 }} className="stagger">
        {[
          { label: "Requests / 24h", value: "4.82M", delta: "+8.2%", up: true, ico: <Icons.Activity size={13}/>, spark: [10,12,11,14,13,17,16,18,21,22,24,26] },
          { label: "p95 Latency",   value: "84ms",  delta: "-12ms", up: true, ico: <Icons.Bolt size={13}/>,     spark: [22,20,21,18,19,16,15,14,13,12,11,10] },
          { label: "Active Users",  value: "1,204", delta: "+142",  up: true, ico: <Icons.Users size={13}/>,    spark: [50,52,55,53,58,60,62,65,68,72,76,80] },
          { label: "Error Rate",    value: "0.04%", delta: "+0.01%",up: false,ico: <Icons.Alert size={13}/>,    spark: [4,5,5,6,5,7,6,8,7,9,8,10] },
        ].map((m, i) => (
          <SpotlightCard key={i} className="metric">
            <div className="label">
              <span style={{
                width: 22, height: 22, borderRadius: 6,
                background: "var(--glass-2)", border: "1px solid var(--line-strong)",
                display: "grid", placeContent: "center",
                color: "var(--accent-b)",
              }}>{m.ico}</span>
              {m.label}
            </div>
            <div className="value">{m.value}</div>
            <div className={`delta ${m.up ? "up" : "down"}`}>
              <Icons.ArrowUp size={11} style={{ transform: m.up ? "" : "rotate(180deg)", display: "inline-block", marginRight: 4 }}/>
              {m.delta}
            </div>
            <Sparkline data={m.spark}/>
          </SpotlightCard>
        ))}
      </div>

      {/* Chart + side */}
      <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr", gap: 14, marginBottom: 14 }}>
        <SpotlightCard style={{ padding: 24 }}>
          <div className="between" style={{ marginBottom: 18 }}>
            <div>
              <h3>Traffic</h3>
              <div className="mono" style={{ fontSize: 11, color: "var(--fg-dim)", marginTop: 2 }}>UTC · LAST 24H</div>
            </div>
            <div className="row" style={{ gap: 12 }}>
              <div className="chart-legend">
                <span><span className="legend-dot" style={{ background: "#7c5cff", boxShadow: "0 0 8px #7c5cff" }}/>Sessions</span>
                <span><span className="legend-dot" style={{ background: "#22d3ee", boxShadow: "0 0 8px #22d3ee" }}/>API Calls</span>
              </div>
              <div className="row" style={{ gap: 4 }}>
                <button className="btn btn-ghost btn-sm">24h</button>
                <button className="btn btn-secondary btn-sm">7d</button>
                <button className="btn btn-ghost btn-sm">30d</button>
              </div>
            </div>
          </div>
          <AreaChart series={series} labels={labels} height={260}/>
        </SpotlightCard>

        <SpotlightCard style={{ padding: 24 }}>
          <div className="between" style={{ marginBottom: 18 }}>
            <h3>Service Mix</h3>
            <button className="btn btn-ghost btn-sm btn-icon"><Icons.Dots size={14}/></button>
          </div>
          <div style={{ display: "grid", placeItems: "center", padding: "10px 0 16px" }}>
            <div style={{ position: "relative" }}>
              <Donut size={170} segments={[
                { value: 48, color: "#7c5cff" },
                { value: 24, color: "#22d3ee" },
                { value: 16, color: "#34d399" },
                { value: 12, color: "#fbbf24" },
              ]}/>
              <div style={{
                position: "absolute", inset: 0,
                display: "grid", placeContent: "center",
                textAlign: "center",
              }}>
                <div className="mono" style={{ fontSize: 10, color: "var(--fg-dim)" }}>TOTAL</div>
                <div style={{ fontSize: 22, fontWeight: 600, letterSpacing: "-0.02em" }}>4.82M</div>
              </div>
            </div>
          </div>
          <div className="col" style={{ gap: 8 }}>
            {[
              { c: "#7c5cff", n: "auth-service",     v: "48%" },
              { c: "#22d3ee", n: "search-index",     v: "24%" },
              { c: "#34d399", n: "billing-worker",   v: "16%" },
              { c: "#fbbf24", n: "report-generator", v: "12%" },
            ].map((s) => (
              <div key={s.n} className="between" style={{ fontSize: 12, color: "var(--fg-muted)" }}>
                <span className="row"><span className="legend-dot" style={{ background: s.c, boxShadow: `0 0 6px ${s.c}` }}/>{s.n}</span>
                <span className="mono">{s.v}</span>
              </div>
            ))}
          </div>
        </SpotlightCard>
      </div>

      {/* Activity + Services */}
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 14 }}>
        <SpotlightCard style={{ padding: 24 }}>
          <div className="between" style={{ marginBottom: 18 }}>
            <h3>Recent Activity</h3>
            <button className="btn btn-ghost btn-sm">View all <Icons.ArrowRight size={12}/></button>
          </div>
          <div className="col" style={{ gap: 4 }}>
            {[
              { who: "DK", what: "deployed auth-service", when: "vor 4m", kind: "ok" },
              { who: "MR", what: "merged PR #842 in dryl/core", when: "vor 18m", kind: "info" },
              { who: "—", what: "alert resolved: billing-worker latency", when: "vor 1h", kind: "ok" },
              { who: "JS", what: "rotated production secrets", when: "vor 2h", kind: "warn" },
              { who: "DK", what: "scaled search-index to 6 replicas", when: "vor 3h", kind: "info" },
            ].map((a, i) => (
              <div key={i} className="row" style={{
                padding: "10px 10px",
                borderRadius: 10,
                gap: 12,
                transition: "background var(--dur-fast) var(--ease-out)",
                cursor: "default",
              }}
              onMouseEnter={(e) => e.currentTarget.style.background = "var(--glass-2)"}
              onMouseLeave={(e) => e.currentTarget.style.background = "transparent"}>
                <div className="avatar" style={{
                  background: a.who === "—" ? "var(--glass-2)" : "var(--accent-grad)",
                  color: a.who === "—" ? "var(--fg-dim)" : "white",
                }}>{a.who}</div>
                <div style={{ flex: 1, fontSize: 13 }}>
                  <span style={{ color: "var(--fg)" }}>{a.what}</span>
                </div>
                <div className="mono" style={{ fontSize: 11, color: "var(--fg-dim)" }}>{a.when}</div>
              </div>
            ))}
          </div>
        </SpotlightCard>

        <SpotlightCard style={{ padding: 24 }}>
          <div className="between" style={{ marginBottom: 18 }}>
            <h3>Services</h3>
            <div className="row" style={{ gap: 6 }}>
              <button className="btn btn-secondary btn-sm"><Icons.Filter size={12}/> Filter</button>
              <button className="btn btn-ghost btn-sm btn-icon"><Icons.Dots size={14}/></button>
            </div>
          </div>
          <div className="col" style={{ gap: 10 }}>
            {[
              { n: "auth-service",     s: "healthy",   m: 99.98, l: "24ms" },
              { n: "search-index",     s: "healthy",   m: 99.92, l: "38ms" },
              { n: "billing-worker",   s: "throttled", m: 98.40, l: "142ms" },
              { n: "media-pipeline",   s: "failed",    m: 87.20, l: "—" },
              { n: "report-generator", s: "healthy",   m: 99.99, l: "56ms" },
            ].map((sv) => {
              const cls = sv.s === "healthy" ? "badge-success" : sv.s === "throttled" ? "badge-warning" : "badge-danger";
              return (
                <div key={sv.n} style={{
                  padding: "12px 14px",
                  borderRadius: 12,
                  border: "1px solid var(--line)",
                  background: "rgba(255,255,255,0.02)",
                }}>
                  <div className="between" style={{ marginBottom: 8 }}>
                    <div className="row" style={{ gap: 10 }}>
                      <div style={{
                        width: 26, height: 26, borderRadius: 7,
                        background: "var(--glass-2)",
                        border: "1px solid var(--line-strong)",
                        display: "grid", placeContent: "center",
                        color: "var(--accent-b)",
                      }}><Icons.Server size={13}/></div>
                      <span className="name mono" style={{ fontSize: 13, color: "var(--fg)" }}>{sv.n}</span>
                    </div>
                    <div className="row" style={{ gap: 12 }}>
                      <span className="mono" style={{ fontSize: 11, color: "var(--fg-dim)" }}>{sv.l}</span>
                      <span className={`badge ${cls} badge-dot`}>{sv.s}</span>
                    </div>
                  </div>
                  <div className="between" style={{ marginBottom: 4, fontSize: 11, color: "var(--fg-dim)" }} className="mono">
                    <span className="mono">UPTIME · 30d</span>
                    <span className="mono">{sv.m.toFixed(2)}%</span>
                  </div>
                  <div className="progress">
                    <div className="progress-bar" style={{
                      width: sv.m + "%",
                      background: sv.s === "failed" ? "linear-gradient(135deg, #f87171, #fbbf24)" : sv.s === "throttled" ? "linear-gradient(135deg, #fbbf24, #f87171)" : "var(--accent-grad)",
                    }}/>
                  </div>
                </div>
              );
            })}
          </div>
        </SpotlightCard>
      </div>
    </div>
  );
}

window.PageShowcase = PageShowcase;
