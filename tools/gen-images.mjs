// Sinh ảnh minh họa SVG cho sản phẩm (mỗi màu một ảnh) và danh mục.
// Chạy: node tools/gen-images.mjs  (từ thư mục gốc D:\DoAnPhuc)
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const OUT = join('src', 'VietThang.Web', 'wwwroot', 'images');
const COLORS = {
  'Trắng': '#FFFFFF', 'Đen': '#2B2B2B', 'Xanh than': '#1F3A5F', 'Xanh mint': '#98D8C8', 'Hồng': '#F4A7B9',
  'Be': '#D9C3A5', 'Đỏ đô': '#7B1E3A', 'Vàng': '#F2C14E', 'Tím': '#8E6C9E', 'Xám': '#9AA0A6'
};

// type: set-shorts | set-capri | set-long | top | top-v | pants-capri ; pattern: flower | dino | none ; kid: bool
const PRODUCTS = [
  { code: 'VT-BL-001', name: 'Bộ lanh nữ quần đùi họa tiết hoa nhí', type: 'set-shorts', pattern: 'flower', colors: ['Xanh mint', 'Hồng', 'Be'] },
  { code: 'VT-BL-002', name: 'Bộ lanh nữ quần lửng cổ tròn', type: 'set-capri', pattern: 'none', colors: ['Xanh than', 'Đỏ đô', 'Trắng'] },
  { code: 'VT-CT-003', name: 'Bộ cotton nữ quần dài tay lỡ', type: 'set-long', pattern: 'none', colors: ['Xám', 'Đen', 'Tím'] },
  { code: 'VT-TL-004', name: 'Áo thun lạnh nữ cổ tim', type: 'top-v', pattern: 'none', colors: ['Trắng', 'Đen', 'Hồng', 'Vàng'] },
  { code: 'VT-TN-005', name: 'Bộ lanh trung niên quần lửng in hoa', type: 'set-capri', pattern: 'flower', colors: ['Xanh than', 'Đỏ đô', 'Tím'] },
  { code: 'VT-TN-006', name: 'Bộ lanh trung niên quần dài cổ sen', type: 'set-long', pattern: 'none', colors: ['Be', 'Xám', 'Xanh than'] },
  { code: 'VT-BT-007', name: 'Bộ cotton bé trai in khủng long', type: 'set-shorts', pattern: 'dino', kid: true, colors: ['Xanh than', 'Xám', 'Vàng'] },
  { code: 'VT-BG-008', name: 'Bộ lanh bé gái hoa nhí cổ bèo', type: 'set-shorts', pattern: 'flower', kid: true, colors: ['Hồng', 'Xanh mint', 'Trắng'] },
  { code: 'VT-NM-009', name: 'Bộ thun nam cổ tròn quần đùi', type: 'set-shorts', pattern: 'none', colors: ['Đen', 'Xám', 'Xanh than'] },
  { code: 'VT-NM-010', name: 'Quần lửng nam lanh lưng thun', type: 'pants-capri', pattern: 'none', colors: ['Be', 'Xám', 'Đen'] }
];

const CATEGORIES = [
  { slug: 'nu', name: 'Nữ', color: '#F4A7B9', type: 'set-capri' },
  { slug: 'trung-nien', name: 'Trung niên', color: '#7B1E3A', type: 'set-long' },
  { slug: 'tre-em', name: 'Trẻ em', color: '#F2C14E', type: 'set-shorts', kid: true },
  { slug: 'nam', name: 'Nam', color: '#1F3A5F', type: 'set-shorts' }
];

const codePart = s => s.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toUpperCase().replace(/[^A-Z0-9]/g, '');
const shade = (hex, f) => { const n = parseInt(hex.slice(1), 16); const r = (n >> 16) & 255, g = (n >> 8) & 255, b = n & 255; const m = v => Math.max(0, Math.min(255, Math.round(v * f))); return `#${[m(r), m(g), m(b)].map(v => v.toString(16).padStart(2, '0')).join('')}`; };
const isLight = hex => { const n = parseInt(hex.slice(1), 16); return ((n >> 16) & 255) * 0.299 + ((n >> 8) & 255) * 0.587 + (n & 255) * 0.114 > 186; };

function garment(type, fill, pattern, kid) {
  const stroke = isLight(fill) ? '#B9C2CF' : shade(fill, 0.7);
  const detail = isLight(fill) ? '#C9D1DC' : shade(fill, 0.8);
  const pat = pattern !== 'none' ? `fill="url(#pat)"` : '';
  const s = kid ? 0.78 : 1;
  const parts = [];
  const shirtNeck = type === 'top-v' ? 'L300,235' : 'Q300,250 400,170';
  const shirt = `M200,170 L150,182 L108,300 L172,322 L182,440 L418,440 L428,322 L492,300 L450,182 L400,170 ${type === 'top-v' ? 'L300,235 L200,170' : 'Q300,250 200,170'} Z`;
  const collar = type === 'top-v' ? `<path d="M200,170 L300,235 L400,170" fill="none" stroke="${detail}" stroke-width="6" stroke-linejoin="round"/>`
    : `<path d="M215,172 Q300,232 385,172" fill="none" stroke="${detail}" stroke-width="7"/>`;
  const hasShirt = type.startsWith('set') || type.startsWith('top');
  const hasPants = type.startsWith('set') || type.startsWith('pants');
  const pantsOnlyOffset = type.startsWith('pants') ? -200 : 0;
  let pants = '';
  if (type.endsWith('shorts')) pants = `M186,458 L414,458 L432,600 L318,600 L300,520 L282,600 L168,600 Z`;
  else if (type.endsWith('capri')) pants = `M186,458 L414,458 L424,672 L322,672 L300,520 L278,672 L176,672 Z`;
  else if (type.endsWith('long')) pants = `M186,458 L414,458 L416,752 L322,752 L300,520 L278,752 L184,752 Z`;
  if (hasShirt) parts.push(`<path d="${shirt}" fill="${fill}" stroke="${stroke}" stroke-width="4" stroke-linejoin="round"/>`, pat ? `<path d="${shirt}" ${pat} opacity="0.85"/>` : '', collar,
    `<path d="M300,250 L300,438" stroke="${detail}" stroke-width="2" opacity="0.5"/>`);
  if (hasPants) parts.push(`<g transform="translate(0,${pantsOnlyOffset})"><path d="${pants}" fill="${fill}" stroke="${stroke}" stroke-width="4" stroke-linejoin="round"/>${pat ? `<path d="${pants}" ${pat} opacity="0.85"/>` : ''}<path d="M186,470 L414,470" stroke="${detail}" stroke-width="5"/></g>`);
  const cx = 300, cy = hasPants && hasShirt ? 460 : (hasShirt ? 305 : 365);
  return `<g transform="translate(${cx},${cy}) scale(${s}) translate(${-cx},${-cy})">${parts.join('')}</g>`;
}

function patternDef(pattern, fill) {
  const c = isLight(fill) ? '#D96C8A' : '#FFFFFF';
  if (pattern === 'flower') return `<pattern id="pat" width="44" height="44" patternUnits="userSpaceOnUse"><g fill="${c}" opacity="0.55"><circle cx="12" cy="12" r="3"/><circle cx="17" cy="9" r="3"/><circle cx="17" cy="15" r="3"/><circle cx="7" cy="9" r="3"/><circle cx="7" cy="15" r="3"/><circle cx="12" cy="12" r="1.6" fill="${isLight(fill) ? '#F2C14E' : '#F2C14E'}"/><circle cx="33" cy="32" r="2.4"/><circle cx="37" cy="29" r="2.4"/><circle cx="37" cy="35" r="2.4"/><circle cx="29" cy="29" r="2.4"/><circle cx="29" cy="35" r="2.4"/></g></pattern>`;
  if (pattern === 'dino') return `<pattern id="pat" width="60" height="50" patternUnits="userSpaceOnUse"><g fill="${c}" opacity="0.6"><path d="M6,30 L14,14 L22,30 Z"/><path d="M14,30 L20,20 L26,30 Z"/><path d="M36,44 L44,28 L52,44 Z"/><circle cx="46" cy="12" r="4"/></g></pattern>`;
  return '';
}

function wrap(text, max = 26) {
  const words = text.split(' '); const lines = []; let cur = '';
  for (const w of words) { if ((cur + ' ' + w).trim().length > max) { lines.push(cur.trim()); cur = w; } else cur += ' ' + w; }
  if (cur.trim()) lines.push(cur.trim());
  return lines.slice(0, 2);
}

function productSvg(p, colorName) {
  const fill = COLORS[colorName];
  const bg1 = isLight(fill) ? '#F3F5F8' : shade(fill, 1.0) + '22';
  const lines = wrap(p.name);
  return `<svg xmlns="http://www.w3.org/2000/svg" width="600" height="800" viewBox="0 0 600 800">
<defs>
  <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFFFFF"/><stop offset="1" stop-color="#EEF2F8"/></linearGradient>
  ${patternDef(p.pattern, fill)}
</defs>
<rect width="600" height="800" fill="url(#bg)"/>
<circle cx="300" cy="400" r="250" fill="${fill}" opacity="${isLight(fill) ? 0.25 : 0.12}"/>
${garment(p.type, fill, p.pattern, p.kid)}
<rect x="0" y="690" width="600" height="110" fill="#FFFFFF" opacity="0.9"/>
<text x="300" y="728" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="24" font-weight="700" fill="#1F3A5F">${lines[0]}</text>
${lines[1] ? `<text x="300" y="756" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="22" font-weight="600" fill="#1F3A5F">${lines[1]}</text>` : ''}
<g transform="translate(300,${lines[1] ? 782 : 758})"><rect x="-70" y="-13" width="140" height="22" rx="11" fill="${fill}" stroke="#B9C2CF" stroke-width="${isLight(fill) ? 1 : 0}"/><text y="4" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="13" font-weight="600" fill="${isLight(fill) ? '#1F3A5F' : '#FFFFFF'}">${colorName}</text></g>
<text x="24" y="40" font-family="Segoe UI, Arial, sans-serif" font-size="16" font-weight="800" fill="#1F3A5F" opacity="0.7">VT · Việt Thắng</text>
<text x="576" y="40" text-anchor="end" font-family="Segoe UI, Arial, sans-serif" font-size="13" fill="#6b7280">${p.code}</text>
</svg>`;
}

function categorySvg(c) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600" viewBox="0 0 800 600">
<defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${shade(c.color, 1.15)}" stop-opacity="0.35"/><stop offset="1" stop-color="${c.color}" stop-opacity="0.75"/></linearGradient></defs>
<rect width="800" height="600" fill="#F3F5F8"/><rect width="800" height="600" fill="url(#bg)"/>
<g transform="translate(400,0) scale(0.72) translate(-300,20)">${garment(c.type, c.color, 'none', c.kid)}</g>
</svg>`;
}

mkdirSync(join(OUT, 'products'), { recursive: true });
mkdirSync(join(OUT, 'categories'), { recursive: true });
let count = 0;
for (const p of PRODUCTS) for (const color of p.colors) {
  writeFileSync(join(OUT, 'products', `${p.code}-${codePart(color)}.svg`), productSvg(p, color), 'utf8'); count++;
}
for (const c of CATEGORIES) { writeFileSync(join(OUT, 'categories', `${c.slug}.svg`), categorySvg(c), 'utf8'); count++; }
console.log(`Đã tạo ${count} ảnh trong ${OUT}`);
