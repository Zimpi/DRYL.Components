// Validates light-mode semantic + chart colors: contrast >= 3.0 against the
// light elevated surface (--bg-1 #f5f5fa), and the fg scale >= 4.5 for text.
// Values must mirror the LIGHT-TOKEN-SET in dryl.css — update both together.
const surface = "f5f5fa";

const lum = (hex) => {
  const c = [0, 2, 4].map(i => parseInt(hex.slice(i, i + 2), 16) / 255)
    .map(v => v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
};
const ratio = (a, b) => {
  const [l1, l2] = [lum(a), lum(b)].sort((x, y) => y - x);
  return (l1 + 0.05) / (l2 + 0.05);
};

const checks = [
  // name, hex (no #), minimum ratio vs light surface
  ["--fg (text)",      "15151c", 4.5],
  ["--success",        "0e8a4d", 3.0],
  ["--warning",        "b45309", 3.0],
  ["--danger",         "dc2626", 3.0],
  ["--info",           "0e7490", 3.0],
  ["--chart-3",        "96610e", 3.0],
  ["--chart-4",        "1d7f46", 3.0],
  ["--chart-5",        "b0316f", 3.0],
  ["--chart-6",        "3a63c4", 3.0],
  ["--danger-fg",      "b91c1c", 4.5],
];

let failed = false;
for (const [name, hex, min] of checks) {
  const r = ratio(hex, surface);
  const ok = r >= min;
  if (!ok) failed = true;
  console.log(`${ok ? "PASS" : "FAIL"}  ${name.padEnd(18)} ${r.toFixed(2)}:1  (min ${min}:1)`);
}
process.exit(failed ? 1 : 0);
