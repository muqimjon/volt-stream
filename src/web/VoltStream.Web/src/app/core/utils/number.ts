export function parseDecimal(value: unknown): number | null {
  if (typeof value === 'number') return Number.isFinite(value) ? value : null;
  if (value === null || value === undefined) return null;
  const text = String(value).trim().replace(/\s/g, '').replace(',', '.');
  if (!text || !/^[+-]?(\d+\.?\d*|\.\d+)$/.test(text)) return null;
  const n = Number(text);
  return Number.isFinite(n) ? n : null;
}

function decimals(n: number): number {
  const s = String(n);
  const dot = s.indexOf('.');
  if (dot < 0) return 0;
  const e = s.indexOf('e');
  return (e < 0 ? s.length : e) - dot - 1;
}

export function mod(a: number, b: number): number {
  const scale = 10 ** Math.max(decimals(a), decimals(b));
  return (Math.round(a * scale) % Math.round(b * scale)) / scale;
}

export function round(n: number, frac = 2): number {
  const scale = 10 ** frac;
  return Math.round((n + Number.EPSILON) * scale) / scale;
}

export function fmtNum(n: number | null | undefined, frac = 2): string {
  if (n === null || n === undefined || !Number.isFinite(n)) return '';
  const fixed = round(n, frac);
  const [int, dec] = String(Math.abs(fixed)).split('.');
  const grouped = int.replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
  return (fixed < 0 ? '-' : '') + grouped + (dec ? '.' + dec : '');
}
