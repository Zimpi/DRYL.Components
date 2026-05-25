/* DRYL — small shared primitives + visual helpers */

const { useState, useEffect, useRef, useMemo } = React;

/* ---------- Spotlight that follows the cursor over a card ---------- */
function SpotlightCard({ children, className = "", style, ...rest }) {
  const ref = useRef(null);
  const onMove = (e) => {
    const el = ref.current;
    if (!el) return;
    const r = el.getBoundingClientRect();
    el.style.setProperty("--mx", `${e.clientX - r.left}px`);
    el.style.setProperty("--my", `${e.clientY - r.top}px`);
  };
  return (
    <div
      ref={ref}
      onMouseMove={onMove}
      className={`glass-card spotlight-card ${className}`}
      style={style}
      {...rest}
    >
      <div className="spotlight-fx" />
      <div className="spotlight-content">{children}</div>
    </div>
  );
}

/* ---------- Tiny sparkline ---------- */
function Sparkline({ data, w = 96, h = 32, color = "url(#sparkGrad)", fill = true }) {
  const max = Math.max(...data);
  const min = Math.min(...data);
  const rng = max - min || 1;
  const stepX = w / (data.length - 1);
  const points = data
    .map((d, i) => `${i * stepX},${h - ((d - min) / rng) * (h - 4) - 2}`)
    .join(" ");
  const area = `0,${h} ${points} ${w},${h}`;
  return (
    <svg className="spark" viewBox={`0 0 ${w} ${h}`} preserveAspectRatio="none">
      <defs>
        <linearGradient id="sparkGrad" x1="0" x2="1">
          <stop offset="0%" stopColor="#7c5cff" />
          <stop offset="100%" stopColor="#22d3ee" />
        </linearGradient>
        <linearGradient id="sparkFill" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="rgba(124,92,255,0.35)" />
          <stop offset="100%" stopColor="rgba(34,211,238,0)" />
        </linearGradient>
      </defs>
      {fill && <polygon points={area} fill="url(#sparkFill)" />}
      <polyline points={points} fill="none" stroke={color} strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

/* ---------- AreaChart ---------- */
function AreaChart({ series, height = 220, labels }) {
  // series: [{ name, color, data: number[] }]
  const w = 720;
  const h = height;
  const padL = 36, padR = 12, padT = 14, padB = 26;
  const innerW = w - padL - padR;
  const innerH = h - padT - padB;
  const len = series[0].data.length;
  const all = series.flatMap(s => s.data);
  const max = Math.max(...all);
  const min = 0;
  const stepX = innerW / (len - 1);
  const yTicks = 4;

  const yFor = (v) => padT + innerH - ((v - min) / (max - min || 1)) * innerH;
  const xFor = (i) => padL + i * stepX;

  return (
    <svg viewBox={`0 0 ${w} ${h}`} width="100%" preserveAspectRatio="none" style={{ display: "block" }}>
      <defs>
        {series.map((s, i) => (
          <linearGradient key={i} id={`area-${i}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={s.color} stopOpacity="0.4" />
            <stop offset="100%" stopColor={s.color} stopOpacity="0" />
          </linearGradient>
        ))}
        <filter id="lineGlow" x="-50%" y="-50%" width="200%" height="200%">
          <feGaussianBlur stdDeviation="3" result="b"/>
          <feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge>
        </filter>
      </defs>

      {/* Grid */}
      {Array.from({ length: yTicks + 1 }).map((_, i) => {
        const y = padT + (innerH * i) / yTicks;
        const v = Math.round(max - (max * i) / yTicks);
        return (
          <g key={i}>
            <line x1={padL} x2={w - padR} y1={y} y2={y} stroke="rgba(255,255,255,0.05)" strokeWidth="1"/>
            <text x={padL - 8} y={y + 3} fontSize="10" fill="rgba(255,255,255,0.3)" textAnchor="end" fontFamily="JetBrains Mono">{v}</text>
          </g>
        );
      })}

      {/* X labels */}
      {labels && labels.map((l, i) => (
        <text key={i} x={xFor(i)} y={h - 8} fontSize="10" fill="rgba(255,255,255,0.3)" textAnchor="middle" fontFamily="JetBrains Mono">{l}</text>
      ))}

      {/* Areas */}
      {series.map((s, idx) => {
        const points = s.data.map((d, i) => `${xFor(i)},${yFor(d)}`).join(" ");
        const area = `${padL},${padT + innerH} ${points} ${w - padR},${padT + innerH}`;
        return (
          <g key={idx}>
            <polygon points={area} fill={`url(#area-${idx})`} />
            <polyline points={points} fill="none" stroke={s.color} strokeWidth="2" filter="url(#lineGlow)" strokeLinecap="round" strokeLinejoin="round"/>
            {s.data.map((d, i) => (
              <circle key={i} cx={xFor(i)} cy={yFor(d)} r="2.5" fill={s.color} />
            ))}
          </g>
        );
      })}
    </svg>
  );
}

/* ---------- Bars ---------- */
function BarChart({ data, labels, color = "#7c5cff", height = 180 }) {
  const w = 720, h = height;
  const padL = 30, padR = 10, padT = 14, padB = 26;
  const innerW = w - padL - padR;
  const innerH = h - padT - padB;
  const max = Math.max(...data);
  const bw = innerW / data.length * 0.6;
  const gap = innerW / data.length * 0.4;
  return (
    <svg viewBox={`0 0 ${w} ${h}`} width="100%" preserveAspectRatio="none">
      <defs>
        <linearGradient id="barGrad" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#7c5cff" />
          <stop offset="100%" stopColor="#22d3ee" />
        </linearGradient>
      </defs>
      {[0, 0.5, 1].map((p, i) => {
        const y = padT + innerH * p;
        const v = Math.round(max * (1 - p));
        return (
          <g key={i}>
            <line x1={padL} x2={w - padR} y1={y} y2={y} stroke="rgba(255,255,255,0.05)" />
            <text x={padL - 6} y={y + 3} fontSize="10" fill="rgba(255,255,255,0.3)" textAnchor="end" fontFamily="JetBrains Mono">{v}</text>
          </g>
        );
      })}
      {data.map((d, i) => {
        const bh = (d / max) * innerH;
        const x = padL + i * (bw + gap) + gap / 2;
        const y = padT + innerH - bh;
        return (
          <g key={i}>
            <rect x={x} y={y} width={bw} height={bh} rx="4" fill="url(#barGrad)" opacity="0.95"/>
            <rect x={x} y={y} width={bw} height={bh} rx="4" fill="url(#barGrad)" filter="blur(8px)" opacity="0.6"/>
            {labels && <text x={x + bw / 2} y={h - 8} fontSize="10" fill="rgba(255,255,255,0.4)" textAnchor="middle" fontFamily="JetBrains Mono">{labels[i]}</text>}
          </g>
        );
      })}
    </svg>
  );
}

/* ---------- Donut ---------- */
function Donut({ segments, size = 140 }) {
  const r = size / 2 - 12;
  const c = 2 * Math.PI * r;
  const total = segments.reduce((s, x) => s + x.value, 0);
  let offset = 0;
  return (
    <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
      <circle cx={size/2} cy={size/2} r={r} fill="none" stroke="rgba(255,255,255,0.06)" strokeWidth="14"/>
      {segments.map((s, i) => {
        const len = (s.value / total) * c;
        const dash = `${len} ${c - len}`;
        const el = (
          <circle key={i}
            cx={size/2} cy={size/2} r={r} fill="none"
            stroke={s.color} strokeWidth="14"
            strokeDasharray={dash}
            strokeDashoffset={-offset}
            strokeLinecap="round"
            style={{ filter: `drop-shadow(0 0 6px ${s.color}aa)`, transition: 'stroke-dashoffset 600ms ease' }}
            transform={`rotate(-90 ${size/2} ${size/2})`}
          />
        );
        offset += len + 2;
        return el;
      })}
    </svg>
  );
}

/* ---------- Code Block (placeholder-based highlighter) ----------
   Tokenize → store raw spans → escape remaining text → swap placeholders back.
   This avoids regex cross-contamination from injected markup. */
function escHtml(s) {
  return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

function CodeBlock({ language = "razor", code }) {
  const slots = [];
  const PH = "\u0001";
  const place = (cls, text) => {
    const i = slots.length;
    slots.push(`<span class="${cls}">${escHtml(text)}</span>`);
    return `${PH}${i}${PH}`;
  };

  let s = code;

  if (language === "razor" || language === "html") {
    s = s.replace(/<!--[\s\S]*?-->/g, (m) => place("tok-comment", m));
    s = s.replace(/"[^"]*"/g, (m) => place("tok-string", m));
    s = s.replace(/@(?:code|inject|using|typeparam|page|layout|implements|inherits|attribute|namespace)\b/g, (m) => place("tok-keyword", m));
    s = s.replace(/@\w+(?:\.\w+)*/g, (m) => place("tok-keyword", m));
    s = s.replace(/(<\/?)([A-Za-z][A-Za-z0-9.-]*)/g,
      (_, a, b) => a + place("tok-tag", b));
    s = s.replace(/(\s)([A-Za-z-][A-Za-z0-9-]*)(=)/g,
      (_, sp, n, eq) => sp + place("tok-attr", n) + eq);
  } else if (language === "csharp") {
    s = s.replace(/\/\/.*$/gm, (m) => place("tok-comment", m));
    s = s.replace(/"[^"]*"/g, (m) => place("tok-string", m));
    s = s.replace(/\b(public|private|protected|internal|static|class|sealed|new|return|using|namespace|var|void|async|await|string|int|bool|true|false|if|else|foreach|in|get|set|Task|record)\b/g,
      (m) => place("tok-keyword", m));
  } else if (language === "css") {
    s = s.replace(/\/\*[\s\S]*?\*\//g, (m) => place("tok-comment", m));
    s = s.replace(/(--[a-z][a-z0-9-]*)/g, (m) => place("tok-prop", m));
    s = s.replace(/(:\s*)([^;\n{}]+)(;)/g,
      (_, a, b, c) => a + place("tok-string", b) + c);
  }

  // Escape what's left, then swap placeholders back to their stored HTML.
  let out = escHtml(s);
  out = out.replace(/\u0001(\d+)\u0001/g, (_, i) => slots[parseInt(i, 10)]);

  return (
    <pre className="code-block"><code dangerouslySetInnerHTML={{ __html: out }} /></pre>
  );
}

Object.assign(window, { SpotlightCard, Sparkline, AreaChart, BarChart, Donut, CodeBlock });
