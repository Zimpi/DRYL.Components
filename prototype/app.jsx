/* DRYL — App Shell */

const TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "accentA": "#7c5cff",
  "accentB": "#22d3ee",
  "auroraIntensity": 0.85,
  "grain": true,
  "spotlights": true
}/*EDITMODE-END*/;

const NAV = [
  { id: "overview",    title: "Overview",      icon: <Icons.Home size={15}/>,    kbd: "1" },
  { id: "foundations", title: "Foundations",   icon: <Icons.Palette size={15}/>, kbd: "2" },
  { id: "components",  title: "Components",    icon: <Icons.Box size={15}/>,     kbd: "3" },
  { id: "showcase",    title: "Live Showcase", icon: <Icons.Chart size={15}/>,   kbd: "4" },
  { id: "blazor",      title: "Blazor",        icon: <Icons.Code size={15}/>,    kbd: "5" },
];

function App() {
  const initial = (typeof window !== "undefined" && window.location.hash.replace("#", "")) || "overview";
  const [page, setPage] = useState(NAV.find((n) => n.id === initial) ? initial : "overview");
  const [tweaks, setTweak] = useTweaks(TWEAK_DEFAULTS);

  // Apply tweaks live
  useEffect(() => {
    const root = document.documentElement;
    root.style.setProperty("--accent-a", tweaks.accentA);
    root.style.setProperty("--accent-b", tweaks.accentB);
    root.style.setProperty("--accent",   tweaks.accentA);
    root.style.setProperty("--accent-grad", `linear-gradient(135deg, ${tweaks.accentA} 0%, ${tweaks.accentB} 100%)`);
    // Recompute soft/line colors from accentA
    root.style.setProperty("--accent-soft", hexToRgba(tweaks.accentA, 0.18));
    root.style.setProperty("--accent-line", hexToRgba(tweaks.accentA, 0.45));
    const aurora = document.querySelector(".aurora");
    if (aurora) aurora.style.opacity = String(tweaks.auroraIntensity);
    document.body.style.setProperty("--grain-opacity", tweaks.grain ? "0.4" : "0");
    document.documentElement.dataset.spotlights = tweaks.spotlights ? "on" : "off";
  }, [tweaks]);

  // Keyboard shortcuts
  useEffect(() => {
    const onKey = (e) => {
      if (e.target.tagName === "INPUT" || e.target.tagName === "TEXTAREA") return;
      const hit = NAV.find((n) => n.kbd === e.key);
      if (hit) {
        setPage(hit.id);
        window.location.hash = hit.id;
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  const go = (id) => { setPage(id); window.location.hash = id; window.scrollTo({ top: 0, behavior: "smooth" }); };

  const Pages = {
    overview:    <PageOverview go={go}/>,
    foundations: <PageFoundations/>,
    components:  <PageComponents/>,
    showcase:    <PageShowcase/>,
    blazor:      <PageBlazor/>,
  };

  const active = NAV.find((n) => n.id === page);

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">DD</div>
          <div>
            <div className="brand-name">DRYL</div>
            <div className="brand-sub">DEVELOPMENT</div>
          </div>
        </div>

        <div className="search" style={{ width: "100%" }}>
          <Icons.Search size={13}/>
          <input placeholder="Search system…"/>
          <span className="kbd">⌘K</span>
        </div>

        <div className="nav-section">
          <div className="nav-title">System</div>
          {NAV.map((n) => (
            <div key={n.id}
                 className={`nav-item ${page === n.id ? "active" : ""}`}
                 onClick={() => go(n.id)}>
              <span className="ico">{n.icon}</span>
              {n.title}
              <span className="kbd">{n.kbd}</span>
            </div>
          ))}
        </div>

        <div className="nav-section">
          <div className="nav-title">Workspace</div>
          {[
            { ico: <Icons.Folder size={15}/>, t: "Projects" },
            { ico: <Icons.Users size={15}/>,  t: "Team" },
            { ico: <Icons.Bell size={15}/>,   t: "Notifications" },
            { ico: <Icons.Settings size={15}/>, t: "Settings" },
          ].map((x) => (
            <div key={x.t} className="nav-item">
              <span className="ico">{x.ico}</span>{x.t}
            </div>
          ))}
        </div>

        <div style={{ marginTop: "auto" }}>
          <div style={{
            padding: 14,
            borderRadius: "var(--r-md)",
            background: "linear-gradient(180deg, rgba(124,92,255,0.12), rgba(34,211,238,0.04))",
            border: "1px solid var(--accent-line)",
            position: "relative",
            overflow: "hidden",
          }}>
            <div style={{
              position: "absolute",
              top: -20, right: -20,
              width: 80, height: 80,
              borderRadius: "50%",
              background: "radial-gradient(circle, rgba(124,92,255,0.4), transparent 70%)",
              filter: "blur(10px)",
            }}/>
            <div className="row" style={{ gap: 8, marginBottom: 6 }}>
              <Icons.Sparkle size={13}/>
              <div style={{ fontSize: 12, fontWeight: 500 }}>Tweak it live</div>
            </div>
            <div style={{ fontSize: 11.5, color: "var(--fg-muted)", lineHeight: 1.45, marginBottom: 10 }}>
              Toolbar oben → „Tweaks", um Akzente, Aurora und Glanz anzupassen.
            </div>
            <div style={{ fontSize: 10, color: "var(--fg-dim)" }} className="mono">v0.1.0 · 2026</div>
          </div>
        </div>
      </aside>

      <main>
        <div className="topbar">
          <div className="crumb">
            <span>DRYL</span>
            <span className="sep">/</span>
            <b>{active.title}</b>
          </div>
          <div style={{ flex: 1 }}/>
          <div className="row" style={{ gap: 8 }}>
            <button className="btn btn-ghost btn-sm btn-icon"><Icons.Bell size={15}/></button>
            <button className="btn btn-ghost btn-sm btn-icon"><Icons.Globe size={15}/></button>
            <div style={{ width: 1, height: 22, background: "var(--line)" }}/>
            <div className="row" style={{ gap: 8 }}>
              <div className="avatar">D</div>
              <div style={{ fontSize: 12, lineHeight: 1.2 }}>
                <div style={{ color: "var(--fg)", fontWeight: 500 }}>Daniel</div>
                <div style={{ color: "var(--fg-dim)", fontSize: 11 }} className="mono">d@dryl.dev</div>
              </div>
            </div>
          </div>
        </div>

        <div className="main" key={page}>
          {Pages[page]}
        </div>
      </main>

      <DrylTweaks tweaks={tweaks} setTweak={setTweak}/>
    </div>
  );
}

function hexToRgba(hex, a) {
  const h = hex.replace("#", "");
  const bigint = parseInt(h.length === 3 ? h.split("").map(c => c + c).join("") : h, 16);
  const r = (bigint >> 16) & 255;
  const g = (bigint >> 8)  & 255;
  const b =  bigint        & 255;
  return `rgba(${r}, ${g}, ${b}, ${a})`;
}

function DrylTweaks({ tweaks, setTweak }) {
  const palettes = [
    ["#7c5cff", "#22d3ee"], // violet → cyan (default)
    ["#22d3ee", "#7c5cff"], // cyan → violet
    ["#f472b6", "#7c5cff"], // pink → violet
    ["#34d399", "#22d3ee"], // green → cyan
    ["#fbbf24", "#f87171"], // amber → coral
    ["#a78bfa", "#f0abfc"], // soft violet → orchid
  ];
  return (
    <TweaksPanel title="Tweaks">
      <TweakSection label="Accent palette">
        <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 8 }}>
          {palettes.map((p, i) => {
            const active = tweaks.accentA.toLowerCase() === p[0].toLowerCase() && tweaks.accentB.toLowerCase() === p[1].toLowerCase();
            return (
              <button
                key={i}
                onClick={() => setTweak({ accentA: p[0], accentB: p[1] })}
                style={{
                  height: 40,
                  borderRadius: 10,
                  border: active ? `1px solid ${p[0]}` : "1px solid var(--line-strong)",
                  background: `linear-gradient(135deg, ${p[0]}, ${p[1]})`,
                  boxShadow: active ? `0 0 0 2px ${p[0]}55, 0 0 24px ${p[0]}66` : "none",
                  cursor: "pointer",
                  transition: "all 200ms var(--ease-out)",
                }}
              />
            );
          })}
        </div>
      </TweakSection>
      <TweakSlider label="Aurora intensity" value={tweaks.auroraIntensity}
        min={0} max={1.5} step={0.05}
        onChange={(v) => setTweak("auroraIntensity", v)}/>
      <TweakToggle label="Film grain" value={tweaks.grain}
        onChange={(v) => setTweak("grain", v)}/>
      <TweakToggle label="Card spotlights" value={tweaks.spotlights}
        onChange={(v) => setTweak("spotlights", v)}/>
    </TweaksPanel>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(<App/>);
