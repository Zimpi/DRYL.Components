// Read the built-in CSS tokens and badge rules instead of maintaining a second
// palette. Browser tests independently check rendered colors and ancestor layers.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

function split(value) {
  let depth = 0, start = 0;
  const parts = [];
  for (let i = 0; i < value.length; i++) {
    if (value[i] === '(') depth++;
    if (value[i] === ')') depth--;
    if (value[i] === ',' && depth === 0) { parts.push(value.slice(start, i).trim()); start = i + 1; }
  }
  parts.push(value.slice(start).trim());
  return parts;
}

export function parseColor(expression, tokens = {}, resolving = new Set()) {
  const value = expression.trim();
  const variable = value.match(/^var\((--[\w-]+)\)$/);
  if (variable) {
    const name = variable[1];
    if (!(name in tokens) || resolving.has(name)) throw new Error(`Missing or cyclic color token ${name}`);
    return parseColor(tokens[name], tokens, new Set([...resolving, name]));
  }
  if (value === 'transparent') return [0, 0, 0, 0];
  if (/^#[0-9a-f]{6}$/i.test(value)) return [1, 3, 5].map(i => parseInt(value.slice(i, i + 2), 16)).concat(1);
  if (/^#[0-9a-f]{3}$/i.test(value)) return [...value.slice(1)].map(c => parseInt(c + c, 16)).concat(1);
  const rgb = value.match(/^rgba?\(([^)]+)\)$/);
  if (rgb) {
    const values = rgb[1].split(/[\s,/]+/).filter(Boolean).map(Number);
    if (values.length === 3) values.push(1);
    if (values.length !== 4 || values.some((n, i) => !Number.isFinite(n) || n < 0 || n > (i === 3 ? 1 : 255)))
      throw new Error(`Unsupported RGB color ${value}`);
    return values;
  }
  const mix = value.match(/^color-mix\((.*)\)$/);
  if (mix) {
    const parts = split(mix[1]);
    if (parts.length !== 3 || parts[0] !== 'in srgb') throw new Error(`Unsupported color mix ${value}`);
    const stops = parts.slice(1).map(part => {
      const weight = part.match(/\s+([\d.]+)%$/);
      return { color: parseColor(weight ? part.slice(0, weight.index) : part, tokens, resolving), weight: weight ? Number(weight[1]) / 100 : null };
    });
    const w0 = stops[0].weight ?? (stops[1].weight === null ? .5 : 1 - stops[1].weight);
    const w1 = stops[1].weight ?? 1 - w0;
    const total = w0 + w1;
    if (!(total > 0) || w0 < 0 || w1 < 0) throw new Error(`Invalid color weights ${value}`);
    const a = stops[0].color, b = stops[1].color;
    const alpha = (a[3] * w0 + b[3] * w1) / total;
    return [0, 1, 2].map(i => alpha ? (a[i] * a[3] * w0 + b[i] * b[3] * w1) / total / alpha : 0)
      .concat(alpha * Math.min(total, 1));
  }
  throw new Error(`Unsupported color expression: ${value}`);
}

export function composite(foreground, background) {
  const alpha = foreground[3] + background[3] * (1 - foreground[3]);
  return [0, 1, 2].map(i => alpha ? (foreground[i] * foreground[3] + background[i] * background[3] * (1 - foreground[3])) / alpha : 0).concat(alpha);
}
export function contrast(a, b) {
  const luminance = color => color.slice(0, 3).map(n => n / 255)
    .map(n => n <= .04045 ? n / 12.92 : ((n + .055) / 1.055) ** 2.4)
    .reduce((sum, n, i) => sum + n * [.2126, .7152, .0722][i], 0);
  const l1 = luminance(a), l2 = luminance(b);
  return (Math.max(l1, l2) + .05) / (Math.min(l1, l2) + .05);
}
function declarations(body) {
  return Object.fromEntries([...body.matchAll(/([\w-]+)\s*:\s*([^;]+);/g)].map(([, key, value]) => [key, value.trim()]));
}
export function validate(css) {
  css = css.replace(/\/\*[\s\S]*?\*\//g, '');
  const body = selector => {
    const escaped = selector.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const matches = [...css.matchAll(new RegExp(`(?:^|[}\\s])${escaped}\\s*\\{([^{}]*)\\}`, 'g'))];
    if (!matches.length) throw new Error(`Missing CSS rule ${selector}`);
    return Object.assign({}, ...matches.map(match => declarations(match[1])));
  };
  const dark = body(':root');
  const light = { ...dark, ...body(':root[data-dryl-mode="light"]') };
  const results = [];
  const record = (name, foreground, background, minimum) => {
    const ratio = contrast(composite(foreground, background), background);
    results.push({ name, ratio, minimum, pass: ratio >= minimum });
  };
  for (const name of ['--fg', '--success', '--warning', '--danger', '--info', '--chart-3', '--chart-4', '--chart-5', '--chart-6', '--danger-fg'])
    record(`light ${name}`, parseColor(`var(${name})`, light), parseColor('var(--bg-1)', light), ['--fg', '--danger-fg'].includes(name) ? 4.5 : 3);
  for (const [mode, tokens] of Object.entries({ dark, light })) {
    const ground = parseColor('var(--bg-0)', tokens);
    for (const surface of ['--bg-0', '--bg-1', '--glass-1', '--panel-float']) {
      const base = composite(parseColor(`var(${surface})`, tokens), ground);
      for (const kind of ['neutral', 'accent', 'success', 'warning', 'danger']) {
        const rule = { ...body('.badge'), ...(kind === 'neutral' ? {} : body(`.badge-${kind}`)) };
        const background = composite(parseColor(rule.background, tokens), base);
        record(`${mode} badge ${kind} on ${surface}`, parseColor(rule.color, tokens), background, 4.5);
      }
    }
  }
  return results;
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  try {
    const results = validate(readFileSync(new URL('../code/DRYL.Components/wwwroot/dryl.css', import.meta.url), 'utf8'));
    for (const { name, ratio, minimum, pass } of results)
      console.log(`${pass ? 'PASS' : 'FAIL'} ${name.padEnd(43)} ${ratio.toFixed(2)}:1 (min ${minimum}:1)`);
    process.exitCode = results.every(result => result.pass) ? 0 : 1;
  } catch (error) { console.error(error.message); process.exitCode = 1; }
}
