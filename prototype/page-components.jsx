/* DRYL — Components Gallery */

function Demo({ title, eyebrow, children, fullWidth }) {
  return (
    <section className="section">
      <div className="section-head">
        <div>
          {eyebrow && <div className="eyebrow" style={{ marginBottom: 6 }}>{eyebrow}</div>}
          <h2>{title}</h2>
        </div>
      </div>
      <SpotlightCard style={{ padding: fullWidth ? 0 : 28 }}>
        {children}
      </SpotlightCard>
    </section>
  );
}

function PageComponents() {
  return (
    <div className="fade-in">
      <div style={{ marginBottom: 48 }}>
        <div className="eyebrow" style={{ marginBottom: 12 }}>Components</div>
        <h1 style={{ fontSize: 44, marginBottom: 14 }}>Bausteine</h1>
        <p className="lead">Alles, was du brauchst, um ein Tool zu bauen, ohne ins Vakuum zu starren. Jede Komponente kommt mit Hover-, Focus- und Active-States.</p>
      </div>

      <ButtonsDemo />
      <FormsDemo />
      <BadgesDemo />
      <NavigationDemo />
      <DataDisplayDemo />
      <FeedbackDemo />
      <ChartsDemo />
      <LoadingDemo />
    </div>
  );
}

/* ----------------- BUTTONS ----------------- */
function ButtonsDemo() {
  return (
    <Demo eyebrow="Actions" title="Buttons">
      <div style={{ display: "grid", gap: 28 }}>
        <div>
          <div className="eyebrow" style={{ marginBottom: 12, fontSize: 10 }}>Variants</div>
          <div className="row" style={{ flexWrap: "wrap", gap: 12 }}>
            <button className="btn btn-primary"><Icons.Sparkle size={14}/> Primary</button>
            <button className="btn btn-secondary">Secondary</button>
            <button className="btn btn-ghost">Ghost</button>
            <button className="btn btn-danger"><Icons.X size={14}/> Delete</button>
            <button className="btn btn-icon btn-secondary"><Icons.Settings size={15}/></button>
          </div>
        </div>
        <div>
          <div className="eyebrow" style={{ marginBottom: 12, fontSize: 10 }}>Sizes</div>
          <div className="row" style={{ flexWrap: "wrap", gap: 12, alignItems: "center" }}>
            <button className="btn btn-primary btn-sm">Small</button>
            <button className="btn btn-primary">Medium</button>
            <button className="btn btn-primary btn-lg">Large</button>
          </div>
        </div>
        <div>
          <div className="eyebrow" style={{ marginBottom: 12, fontSize: 10 }}>States</div>
          <div className="row" style={{ flexWrap: "wrap", gap: 12 }}>
            <button className="btn btn-secondary"><Icons.Download size={14}/> Idle</button>
            <button className="btn btn-secondary"><div className="spinner"/> Loading</button>
            <button className="btn btn-secondary" disabled style={{ opacity: 0.4, cursor: "not-allowed" }}>Disabled</button>
          </div>
        </div>
      </div>
    </Demo>
  );
}

/* ----------------- FORMS ----------------- */
function FormsDemo() {
  const [check, setCheck] = useState(true);
  const [toggle, setToggle] = useState(true);
  return (
    <Demo eyebrow="Input" title="Form Controls">
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 28 }}>
        <div className="col" style={{ gap: 18 }}>
          <div>
            <label className="field-label">Project Name</label>
            <input className="input" placeholder="DRYL.Internal.Tools" defaultValue="DRYL.Internal.Tools"/>
          </div>
          <div>
            <label className="field-label">Description</label>
            <textarea className="textarea" placeholder="Tell us about your project…"/>
          </div>
          <div>
            <label className="field-label">Environment</label>
            <select className="select">
              <option>Production</option>
              <option>Staging</option>
              <option>Development</option>
            </select>
          </div>
        </div>
        <div className="col" style={{ gap: 18 }}>
          <div>
            <label className="field-label">Search</label>
            <div className="search" style={{ width: "100%" }}>
              <Icons.Search size={14}/>
              <input placeholder="Find anything…"/>
              <span className="kbd">⌘K</span>
            </div>
          </div>
          <div>
            <label className="field-label">Email</label>
            <input className="input" type="email" placeholder="d@dryl.dev"/>
          </div>
          <div>
            <label className="field-label">Options</label>
            <div className="col" style={{ gap: 10, marginTop: 6 }}>
              <label className="row" style={{ gap: 10, cursor: "pointer", fontSize: 13, color: "var(--fg-muted)" }}>
                <input type="checkbox" className="checkbox" checked={check} onChange={(e) => setCheck(e.target.checked)}/> Enable hot-reload
              </label>
              <label className="row" style={{ gap: 10, cursor: "pointer", fontSize: 13, color: "var(--fg-muted)" }}>
                <input type="checkbox" className="checkbox" defaultChecked/> Auto-deploy on push
              </label>
              <div className="row" style={{ justifyContent: "space-between", fontSize: 13, color: "var(--fg-muted)" }}>
                <span>Dark mode (always on)</span>
                <input type="checkbox" className="toggle" checked={toggle} onChange={(e) => setToggle(e.target.checked)}/>
              </div>
              <div className="row" style={{ justifyContent: "space-between", fontSize: 13, color: "var(--fg-muted)" }}>
                <span>Send weekly digest</span>
                <input type="checkbox" className="toggle" defaultChecked/>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Demo>
  );
}

/* ----------------- BADGES ----------------- */
function BadgesDemo() {
  return (
    <Demo eyebrow="Status" title="Badges">
      <div className="row" style={{ flexWrap: "wrap", gap: 10 }}>
        <span className="badge">Neutral</span>
        <span className="badge badge-accent badge-dot">Live</span>
        <span className="badge badge-success badge-dot">Healthy</span>
        <span className="badge badge-warning badge-dot">Throttled</span>
        <span className="badge badge-danger badge-dot">Failed</span>
        <span className="badge"><Icons.Lock size={11}/> Locked</span>
        <span className="badge badge-accent"><Icons.Sparkle size={11}/> Pro</span>
      </div>
    </Demo>
  );
}

/* ----------------- NAVIGATION ----------------- */
function NavigationDemo() {
  const [tab, setTab] = useState(0);
  return (
    <Demo eyebrow="Navigation" title="Tabs, Breadcrumbs, Pagination">
      <div className="col" style={{ gap: 28 }}>
        <div>
          <div className="tabs">
            {["Overview", "Metrics", "Logs", "Settings"].map((t, i) => (
              <div key={t} className={`tab ${tab === i ? "active" : ""}`} onClick={() => setTab(i)}>{t}</div>
            ))}
          </div>
          <div style={{ padding: "20px 4px", fontSize: 13, color: "var(--fg-muted)" }}>
            Aktiver Tab: <code>{["Overview", "Metrics", "Logs", "Settings"][tab]}</code>
          </div>
        </div>

        <div className="crumb">
          <Icons.Home size={14}/>
          <span className="sep">/</span>
          <span>Projects</span>
          <span className="sep">/</span>
          <span>DRYL.Internal.Tools</span>
          <span className="sep">/</span>
          <b>Settings</b>
        </div>

        <div className="row" style={{ gap: 6 }}>
          <button className="btn btn-secondary btn-sm btn-icon"><Icons.ChevronDown size={13} style={{ transform: "rotate(90deg)" }}/></button>
          {[1, 2, 3].map((n, i) => (
            <button key={n} className={`btn btn-sm ${i === 1 ? "btn-primary" : "btn-secondary"}`} style={{ minWidth: 32, padding: 0 }}>{n}</button>
          ))}
          <span style={{ color: "var(--fg-dim)", padding: "0 6px" }}>…</span>
          <button className="btn btn-secondary btn-sm" style={{ minWidth: 32, padding: 0 }}>24</button>
          <button className="btn btn-secondary btn-sm btn-icon"><Icons.ChevronRight size={13}/></button>
        </div>
      </div>
    </Demo>
  );
}

/* ----------------- DATA DISPLAY ----------------- */
function DataDisplayDemo() {
  const rows = [
    { name: "auth-service",       env: "prod",    status: "healthy",   latency: 24,  ts: "vor 12s" },
    { name: "billing-worker",     env: "prod",    status: "throttled", latency: 142, ts: "vor 1m" },
    { name: "search-index",       env: "staging", status: "healthy",   latency: 38,  ts: "vor 3m" },
    { name: "media-pipeline",     env: "prod",    status: "failed",    latency: 0,   ts: "vor 4m" },
    { name: "report-generator",   env: "dev",     status: "healthy",   latency: 56,  ts: "vor 9m" },
  ];
  const statusMap = {
    healthy:   "badge-success",
    throttled: "badge-warning",
    failed:    "badge-danger",
  };

  return (
    <Demo eyebrow="Data" title="Table, Stats, Lists" fullWidth>
      {/* Metric cards */}
      <div style={{
        display: "grid",
        gridTemplateColumns: "repeat(4, 1fr)",
        gap: 1,
        background: "var(--line)",
        borderTopLeftRadius: "var(--r-lg)",
        borderTopRightRadius: "var(--r-lg)",
        overflow: "hidden",
      }}>
        {[
          { label: "Requests / min", value: "12,847", delta: "+8.2%", up: true, spark: [10,12,11,14,13,17,16,18,21,22] },
          { label: "p95 Latency",   value: "84 ms",  delta: "-12 ms", up: true,  spark: [22,20,21,18,19,16,15,14,13,12] },
          { label: "Error Rate",    value: "0.04%",  delta: "+0.01%", up: false, spark: [4,5,5,6,5,7,6,8,7,9] },
          { label: "Active Users",  value: "1,204",  delta: "+142",   up: true,  spark: [50,52,55,53,58,60,62,65,68,72] },
        ].map((m, i) => (
          <div key={i} className="metric" style={{ background: "rgba(8,8,12,0.8)" }}>
            <div className="label">{m.label}</div>
            <div className="value">{m.value}</div>
            <div className={`delta ${m.up ? "up" : "down"}`}>
              <Icons.ArrowUp size={11} style={{ transform: m.up ? "" : "rotate(180deg)", display: "inline-block", marginRight: 4 }}/>
              {m.delta}
            </div>
            <Sparkline data={m.spark}/>
          </div>
        ))}
      </div>

      {/* Table */}
      <div style={{ padding: "0 8px" }}>
        <table className="tbl">
          <thead>
            <tr>
              <th style={{ width: 32 }}><input type="checkbox" className="checkbox"/></th>
              <th>Service</th>
              <th>Environment</th>
              <th>Status</th>
              <th>Latency</th>
              <th>Last seen</th>
              <th style={{ width: 60 }}></th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.name}>
                <td><input type="checkbox" className="checkbox"/></td>
                <td>
                  <div className="row" style={{ gap: 10 }}>
                    <div style={{
                      width: 26, height: 26, borderRadius: 7,
                      background: "var(--glass-2)",
                      border: "1px solid var(--line-strong)",
                      display: "grid", placeContent: "center",
                      color: "var(--accent-b)",
                    }}><Icons.Server size={13}/></div>
                    <span className="name mono">{r.name}</span>
                  </div>
                </td>
                <td><span className="badge">{r.env}</span></td>
                <td><span className={`badge ${statusMap[r.status]} badge-dot`}>{r.status}</span></td>
                <td className="mono">{r.latency === 0 ? "—" : `${r.latency} ms`}</td>
                <td>{r.ts}</td>
                <td>
                  <button className="btn btn-ghost btn-sm btn-icon"><Icons.Dots size={14}/></button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Demo>
  );
}

/* ----------------- FEEDBACK ----------------- */
function FeedbackDemo() {
  const [openModal, setOpenModal] = useState(false);
  const [toasts, setToasts] = useState([
    { id: 1, kind: "success", title: "Deployment successful", body: "main → production · 0 errors" },
  ]);

  const showToast = (kind, title, body) => {
    const id = Date.now();
    setToasts((t) => [...t, { id, kind, title, body }]);
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 4000);
  };

  const icoFor = {
    success: <Icons.Check size={14}/>,
    error:   <Icons.X size={14}/>,
    warning: <Icons.Alert size={14}/>,
    info:    <Icons.Info size={14}/>,
  };

  return (
    <Demo eyebrow="Feedback" title="Alerts, Toasts, Modals, Tooltips">
      <div className="col" style={{ gap: 18 }}>
        <div className="col" style={{ gap: 10 }}>
          <div className="alert info">
            <div className="ico"><Icons.Info size={14}/></div>
            <div>
              <div className="title">Heads up</div>
              <div className="body">Ein neues Token ist verfügbar: <code>--glow-soft</code> — sanftes Umgebungslicht für ruhige Sektionen.</div>
            </div>
          </div>
          <div className="alert warning">
            <div className="ico"><Icons.Alert size={14}/></div>
            <div>
              <div className="title">Heavy load</div>
              <div className="body">billing-worker meldet erhöhte Latenz. Auto-Scale empfohlen.</div>
            </div>
          </div>
          <div className="alert success">
            <div className="ico"><Icons.Check size={14}/></div>
            <div>
              <div className="title">All green</div>
              <div className="body">Sämtliche Health-Checks erfolgreich. Letzte Prüfung vor 12 Sekunden.</div>
            </div>
          </div>
        </div>

        <div className="divider"/>

        <div className="row" style={{ gap: 12, flexWrap: "wrap" }}>
          <button className="btn btn-secondary" onClick={() => showToast("success", "Saved", "Your changes are live.")}>Toast · Success</button>
          <button className="btn btn-secondary" onClick={() => showToast("error", "Build failed", "Check the latest CI log.")}>Toast · Error</button>
          <button className="btn btn-secondary" onClick={() => showToast("info", "New release", "v0.2 ist verfügbar.")}>Toast · Info</button>
          <button className="btn btn-primary" onClick={() => setOpenModal(true)}><Icons.Plus size={13}/> Open Modal</button>
          <div className="tt-wrap">
            <button className="btn btn-secondary"><Icons.Info size={13}/> Hover for tooltip</button>
            <div className="tt">Indirektes Licht erklärt</div>
          </div>
        </div>
      </div>

      {/* Toast container */}
      <div style={{
        position: "fixed",
        bottom: 24,
        right: 24,
        display: "flex",
        flexDirection: "column",
        gap: 10,
        zIndex: 60,
      }}>
        {toasts.map((t) => (
          <div key={t.id} className={`toast ${t.kind} fade-in`}>
            <div className="ico">{icoFor[t.kind]}</div>
            <div>
              <div className="title">{t.title}</div>
              <div className="body">{t.body}</div>
            </div>
          </div>
        ))}
      </div>

      {openModal && (
        <div className="modal-backdrop" onClick={() => setOpenModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="row" style={{ marginBottom: 18 }}>
              <div style={{
                width: 40, height: 40, borderRadius: 12,
                background: "var(--accent-soft)",
                border: "1px solid var(--accent-line)",
                display: "grid", placeContent: "center",
                color: "#c4b5fd",
                boxShadow: "0 0 24px rgba(124,92,255,0.3)",
              }}><Icons.Sparkle size={18}/></div>
              <div style={{ flex: 1 }}>
                <h3 style={{ marginBottom: 2 }}>Neue Komponente erstellen</h3>
                <div style={{ fontSize: 12, color: "var(--fg-dim)" }}>Razor-Component im aktuellen Projekt</div>
              </div>
              <button className="btn btn-ghost btn-icon btn-sm" onClick={() => setOpenModal(false)}><Icons.X size={14}/></button>
            </div>
            <div className="col" style={{ gap: 14, marginBottom: 22 }}>
              <div>
                <label className="field-label">Name</label>
                <input className="input" placeholder="ServiceStatusCard"/>
              </div>
              <div>
                <label className="field-label">Namespace</label>
                <input className="input" defaultValue="DRYL.Web.Components"/>
              </div>
            </div>
            <div className="row" style={{ justifyContent: "flex-end", gap: 8 }}>
              <button className="btn btn-ghost" onClick={() => setOpenModal(false)}>Cancel</button>
              <button className="btn btn-primary" onClick={() => setOpenModal(false)}>Create</button>
            </div>
          </div>
        </div>
      )}
    </Demo>
  );
}

/* ----------------- CHARTS ----------------- */
function ChartsDemo() {
  const labels = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
  const series = [
    { name: "Requests", color: "#7c5cff", data: [120, 180, 160, 220, 280, 240, 320] },
    { name: "Errors",   color: "#22d3ee", data: [10, 14, 12, 22, 18, 16, 24] },
  ];
  return (
    <Demo eyebrow="Datavis" title="Charts">
      <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr", gap: 16 }}>
        <div>
          <div className="between" style={{ marginBottom: 14 }}>
            <div>
              <h4>Throughput</h4>
              <div style={{ fontSize: 11, color: "var(--fg-dim)" }} className="mono">LAST 7 DAYS</div>
            </div>
            <div className="chart-legend">
              <span><span className="legend-dot" style={{ background: "#7c5cff", boxShadow: "0 0 8px #7c5cff" }}/>Requests</span>
              <span><span className="legend-dot" style={{ background: "#22d3ee", boxShadow: "0 0 8px #22d3ee" }}/>Errors</span>
            </div>
          </div>
          <AreaChart series={series} labels={labels}/>
        </div>
        <div>
          <div style={{ marginBottom: 14 }}>
            <h4>By Service</h4>
            <div style={{ fontSize: 11, color: "var(--fg-dim)" }} className="mono">SHARE</div>
          </div>
          <div className="row" style={{ gap: 18, justifyContent: "center" }}>
            <Donut size={140} segments={[
              { value: 48, color: "#7c5cff" },
              { value: 24, color: "#22d3ee" },
              { value: 16, color: "#34d399" },
              { value: 12, color: "#fbbf24" },
            ]}/>
            <div className="col" style={{ gap: 8, fontSize: 12 }}>
              <div className="row"><span className="legend-dot" style={{ background: "#7c5cff" }}/>auth · 48%</div>
              <div className="row"><span className="legend-dot" style={{ background: "#22d3ee" }}/>search · 24%</div>
              <div className="row"><span className="legend-dot" style={{ background: "#34d399" }}/>billing · 16%</div>
              <div className="row"><span className="legend-dot" style={{ background: "#fbbf24" }}/>other · 12%</div>
            </div>
          </div>
        </div>
        <div style={{ gridColumn: "1 / -1" }}>
          <div className="between" style={{ marginBottom: 14 }}>
            <div>
              <h4>Deployments</h4>
              <div style={{ fontSize: 11, color: "var(--fg-dim)" }} className="mono">PER WEEK</div>
            </div>
          </div>
          <BarChart data={[4, 7, 5, 9, 12, 8, 14, 11, 16, 13, 17, 21]} labels={["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"]}/>
        </div>
      </div>
    </Demo>
  );
}

/* ----------------- LOADING ----------------- */
function LoadingDemo() {
  const [p, setP] = useState(0);
  useEffect(() => {
    const t = setInterval(() => setP((x) => (x + 7) % 110), 480);
    return () => clearInterval(t);
  }, []);
  return (
    <Demo eyebrow="Pending" title="Loading States">
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 24 }}>
        <div>
          <div className="eyebrow" style={{ marginBottom: 14, fontSize: 10 }}>Skeleton</div>
          <div className="col" style={{ gap: 10 }}>
            <span className="skel" style={{ width: "60%", height: 18 }}/>
            <span className="skel" style={{ width: "100%" }}/>
            <span className="skel" style={{ width: "85%" }}/>
            <span className="skel" style={{ width: "92%" }}/>
            <div className="row" style={{ gap: 10, marginTop: 6 }}>
              <span className="skel" style={{ width: 64, height: 32, borderRadius: 8 }}/>
              <span className="skel" style={{ width: 88, height: 32, borderRadius: 8 }}/>
            </div>
          </div>
        </div>
        <div>
          <div className="eyebrow" style={{ marginBottom: 14, fontSize: 10 }}>Spinner & Progress</div>
          <div className="col" style={{ gap: 18 }}>
            <div className="row" style={{ gap: 14 }}>
              <div className="spinner"/>
              <div className="spinner" style={{ width: 26, height: 26, borderWidth: 2.5 }}/>
              <div className="spinner" style={{ width: 36, height: 36, borderWidth: 3 }}/>
              <span style={{ color: "var(--fg-muted)", fontSize: 13 }}>Synchronizing…</span>
            </div>
            <div>
              <div className="between" style={{ fontSize: 11, color: "var(--fg-dim)", marginBottom: 6 }} >
                <span className="mono">UPLOAD</span>
                <span className="mono">{Math.min(p, 100)}%</span>
              </div>
              <div className="progress">
                <div className="progress-bar" style={{ width: Math.min(p, 100) + "%" }}/>
              </div>
            </div>
            <div>
              <div className="between" style={{ fontSize: 11, color: "var(--fg-dim)", marginBottom: 6 }}>
                <span className="mono">BUILD</span>
                <span className="mono">68%</span>
              </div>
              <div className="progress">
                <div className="progress-bar" style={{ width: "68%" }}/>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Demo>
  );
}

window.PageComponents = PageComponents;
