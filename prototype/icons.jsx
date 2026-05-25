/* DRYL Icon Set — minimal line icons, currentColor */
const Icon = ({ d, size = 16, stroke = 1.6, fill = "none", children, ...rest }) => (
  <svg
    viewBox="0 0 24 24"
    width={size}
    height={size}
    fill={fill}
    stroke="currentColor"
    strokeWidth={stroke}
    strokeLinecap="round"
    strokeLinejoin="round"
    {...rest}
  >
    {d ? <path d={d} /> : children}
  </svg>
);

const Icons = {
  Home:        (p) => <Icon {...p} d="M3 12L12 4l9 8M5 10v9a1 1 0 001 1h3v-6h6v6h3a1 1 0 001-1v-9" />,
  Layers:     (p) => <Icon {...p}><polyline points="12 2 22 7 12 12 2 7 12 2"/><polyline points="2 17 12 22 22 17"/><polyline points="2 12 12 17 22 12"/></Icon>,
  Palette:    (p) => <Icon {...p}><path d="M12 3a9 9 0 109 9c0-1.7-1.3-3-3-3h-2a2 2 0 010-4 2 2 0 00-2-2h-2z"/><circle cx="7.5" cy="10.5" r="1"/><circle cx="12" cy="7.5" r="1"/><circle cx="16.5" cy="10.5" r="1"/></Icon>,
  Type:       (p) => <Icon {...p} d="M4 7V5h16v2M9 5v14m6-14v14M7 19h4m2 0h4" />,
  Box:        (p) => <Icon {...p}><path d="M21 7.5L12 3 3 7.5v9L12 21l9-4.5v-9z"/><path d="M3 7.5L12 12l9-4.5M12 12v9"/></Icon>,
  Grid:       (p) => <Icon {...p}><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></Icon>,
  Bell:       (p) => <Icon {...p} d="M6 8a6 6 0 0112 0c0 7 3 9 3 9H3s3-2 3-9M10 21a2 2 0 004 0" />,
  Bolt:       (p) => <Icon {...p} d="M13 2L4 14h7l-1 8 9-12h-7l1-8z" />,
  Search:     (p) => <Icon {...p}><circle cx="11" cy="11" r="7"/><path d="M21 21l-4.3-4.3"/></Icon>,
  Settings:   (p) => <Icon {...p}><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 01-2.83 2.83l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09a1.65 1.65 0 00-1-1.51 1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 11-2.83-2.83l.06-.06a1.65 1.65 0 00.33-1.82 1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09a1.65 1.65 0 001.51-1 1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 112.83-2.83l.06.06a1.65 1.65 0 001.82.33H9a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 112.83 2.83l-.06.06a1.65 1.65 0 00-.33 1.82V9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"/></Icon>,
  User:       (p) => <Icon {...p}><circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0116 0"/></Icon>,
  Users:      (p) => <Icon {...p}><circle cx="9" cy="8" r="4"/><path d="M2 21a7 7 0 0114 0M17 11a4 4 0 100-8M22 21a7 7 0 00-5-6.7"/></Icon>,
  Chart:      (p) => <Icon {...p}><path d="M3 3v18h18"/><path d="M7 14l4-4 3 3 5-6"/></Icon>,
  Activity:   (p) => <Icon {...p} d="M3 12h4l3-9 4 18 3-9h4" />,
  Server:     (p) => <Icon {...p}><rect x="3" y="4" width="18" height="6" rx="2"/><rect x="3" y="14" width="18" height="6" rx="2"/><path d="M7 7h.01M7 17h.01"/></Icon>,
  Database:   (p) => <Icon {...p}><ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M3 5v6c0 1.7 4 3 9 3s9-1.3 9-3V5M3 11v6c0 1.7 4 3 9 3s9-1.3 9-3v-6"/></Icon>,
  Code:       (p) => <Icon {...p} d="M16 18l6-6-6-6M8 6L2 12l6 6" />,
  Check:      (p) => <Icon {...p} d="M5 12l5 5L20 7" />,
  X:          (p) => <Icon {...p} d="M6 6l12 12M18 6L6 18" />,
  Plus:       (p) => <Icon {...p} d="M12 5v14M5 12h14" />,
  Minus:      (p) => <Icon {...p} d="M5 12h14" />,
  ChevronDown:(p) => <Icon {...p} d="M6 9l6 6 6-6" />,
  ChevronRight:(p) => <Icon {...p} d="M9 6l6 6-6 6" />,
  ArrowUp:    (p) => <Icon {...p} d="M12 19V5M5 12l7-7 7 7" />,
  ArrowRight: (p) => <Icon {...p} d="M5 12h14M13 5l7 7-7 7" />,
  Info:       (p) => <Icon {...p}><circle cx="12" cy="12" r="9"/><path d="M12 8h.01M11 12h1v4h1"/></Icon>,
  Alert:      (p) => <Icon {...p} d="M12 9v4m0 4h.01M10.3 3.86l-8.18 14a2 2 0 001.71 3h16.34a2 2 0 001.71-3l-8.18-14a2 2 0 00-3.4 0z" />,
  Sparkle:    (p) => <Icon {...p} d="M12 3l1.5 4.5L18 9l-4.5 1.5L12 15l-1.5-4.5L6 9l4.5-1.5L12 3zM19 14l.8 2.2L22 17l-2.2.8L19 20l-.8-2.2L16 17l2.2-.8L19 14z" />,
  Flame:      (p) => <Icon {...p} d="M12 2c2 4 6 6 6 11a6 6 0 01-12 0c0-3 2-4 3-7 1 2 3 3 3 5" />,
  Rocket:     (p) => <Icon {...p}><path d="M5 19l-2 2M4.5 16.5l3-3M9 13l-3 3M14 4l6 6-9 9-3-1-2-2-1-3 9-9zM14 10l-1-1"/></Icon>,
  Folder:     (p) => <Icon {...p} d="M3 6a2 2 0 012-2h4l2 2h8a2 2 0 012 2v10a2 2 0 01-2 2H5a2 2 0 01-2-2V6z" />,
  Mail:       (p) => <Icon {...p}><rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 7l9 6 9-6"/></Icon>,
  Calendar:   (p) => <Icon {...p}><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M16 3v4M8 3v4M3 11h18"/></Icon>,
  Star:       (p) => <Icon {...p} d="M12 3l2.7 5.5 6 .9-4.4 4.3 1 6L12 17l-5.3 2.8 1-6L3.3 9.4l6-.9L12 3z" />,
  Filter:     (p) => <Icon {...p} d="M3 4h18l-7 9v6l-4 2v-8L3 4z" />,
  Download:   (p) => <Icon {...p} d="M12 3v12m-5-5l5 5 5-5M5 21h14" />,
  Moon:       (p) => <Icon {...p} d="M21 12.8A9 9 0 1111.2 3a7 7 0 009.8 9.8z" />,
  Globe:      (p) => <Icon {...p}><circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a14 14 0 010 18M12 3a14 14 0 000 18"/></Icon>,
  Dots:       (p) => <Icon {...p}><circle cx="5" cy="12" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/></Icon>,
  Logout:     (p) => <Icon {...p} d="M15 4h4a2 2 0 012 2v12a2 2 0 01-2 2h-4M10 17l-5-5 5-5M5 12h12" />,
  Lock:       (p) => <Icon {...p}><rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 018 0v4"/></Icon>,
};

window.Icons = Icons;
window.Icon = Icon;
