import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { parseColor, composite, contrast, validate } from '../../scripts/validate-light-contrast.mjs';

const css = readFileSync(new URL('../../code/DRYL.Components/wwwroot/dryl.css', import.meta.url), 'utf8');
test('reference ratios are 21 for black/white and one for equal colors', () => {
  assert.equal(contrast(parseColor('#000000'), parseColor('#ffffff')), 21);
  assert.equal(contrast(parseColor('#07070a'), parseColor('#07070a')), 1);
});
test('transparent tints preserve hue and composite over the actual surface', () => {
  const tint = parseColor('color-mix(in srgb, var(--semantic) 10%, transparent)', { '--semantic': '#ff0000' });
  assert.deepEqual(tint, [255, 0, 0, .1]);
  assert.deepEqual(composite(tint, parseColor('#000000')), [25.5, 0, 0, 1]);
  assert.deepEqual(composite(tint, parseColor('#ffffff')), [255, 229.5, 229.5, 1]);
});
test('mixed alpha colors use premultiplied interpolation and nested token resolution', () => {
  assert.deepEqual(parseColor('color-mix(in srgb, rgba(255, 0, 0, 0.5) 50%, #0000ff)'), [85, 0, 170, .75]);
  assert.deepEqual(parseColor('var(--a)', { '--a': 'var(--b)', '--b': '#abc' }), [170, 187, 204, 1]);
});
test('unrecognized expressions and cyclic/missing tokens fail closed', () => {
  for (const value of ['color(display-p3 1 0 0)', 'rgba(0,0,0,2)', 'var(--missing)']) assert.throws(() => parseColor(value));
  assert.throws(() => parseColor('var(--a)', { '--a': 'var(--a)' }));
});
test('fifty actual CSS checks pass including every badge label at 4.5', () => {
  const results = validate(css);
  assert.equal(results.length, 50);
  assert.deepEqual(results.filter(result => !result.pass), []);
});
test('the old semantic foreground on tinted light badges is caught', () => {
  const old = css.replace(/(\.badge-success\s*\{\s*color:)\s*var\(--fg\)/, '$1 var(--success)');
  assert.ok(validate(old).some(result => result.name.startsWith('light badge success') && !result.pass));
});
test('CSS token changes cannot retain a stale passing palette', () => {
  const faded = css.replace(/--fg:\s*#[0-9a-f]{6};/gi, '--fg: #808080;');
  assert.ok(validate(faded).some(result => !result.pass));
  assert.throws(() => validate(css.replace('.badge-warning {', '.renamed-warning {')), /Missing CSS rule/);
});
