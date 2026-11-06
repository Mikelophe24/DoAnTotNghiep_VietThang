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
  { code: 'VT-NM-010', name: 'Quần lửng nam lanh lưng thun', type: 'pants-capri', pattern: 'none', colors: ['Be', 'Xám', 'Đen'] },
  // Sản phẩm bổ sung cho dữ liệu demo (DemoDataSeeder)
  { code: 'VT-BL-011', name: 'Bộ lanh nữ quần dài cổ vuông', type: 'set-long', pattern: 'none', colors: ['Be', 'Xanh mint', 'Hồng'] },
  { code: 'VT-BD-012', name: 'Bộ đũi nữ quần đùi tay cánh tiên', type: 'set-shorts', pattern: 'flower', colors: ['Trắng', 'Vàng', 'Tím'] },
  { code: 'VT-TE-013', name: 'Bộ thun teen in chữ quần lửng', type: 'set-capri', pattern: 'none', colors: ['Đen', 'Trắng', 'Hồng'] },
  { code: 'VT-QN-014', name: 'Quần lanh nữ ống rộng lưng thun', type: 'pants-long', pattern: 'none', colors: ['Be', 'Đen', 'Xanh than'] },
  { code: 'VT-TN-015', name: 'Áo lanh trung niên cổ tàu tay lỡ', type: 'top', pattern: 'flower', colors: ['Đỏ đô', 'Tím', 'Xanh than'] },
  { code: 'VT-TN-016', name: 'Chân váy lanh trung niên xếp ly', type: 'skirt', pattern: 'none', colors: ['Đen', 'Be', 'Xanh than'] },
  { code: 'VT-BT-017', name: 'Bộ thun bé trai sọc ngang quần đùi', type: 'set-shorts', pattern: 'stripe', kid: true, colors: ['Xanh than', 'Đỏ đô', 'Xám'] },
  { code: 'VT-BG-018', name: 'Váy lanh bé gái hai dây hoa nhí', type: 'dress', pattern: 'flower', kid: true, colors: ['Hồng', 'Vàng', 'Trắng'] },
  { code: 'VT-NM-019', name: 'Bộ lanh nam cổ tàu quần dài', type: 'set-long', pattern: 'none', colors: ['Be', 'Xám', 'Xanh than'] },
  { code: 'VT-NM-020', name: 'Áo thun lạnh nam cổ tròn', type: 'top', pattern: 'none', colors: ['Trắng', 'Đen', 'Xám', 'Xanh than'] }
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
  const stroke = isLight(fill) ? '#D9CFC5' : shade(fill, 0.7);
  const detail = isLight(fill) ? '#E3DAD0' : shade(fill, 0.8);
  const pat = pattern !== 'none' ? `fill="url(#pat)"` : '';
  const s = kid ? 0.78 : 1;
  // Chân váy và váy liền: một khối hình thang, không dùng dáng áo + quần
  if (type === 'skirt' || type === 'dress') {
    const shape = type === 'skirt'
      ? 'M205,300 L395,300 L455,580 L145,580 Z'
      : 'M238,170 L262,170 Q300,215 338,170 L362,170 L384,320 L468,640 L132,640 L216,320 Z';
    const extra = type === 'skirt'
      ? `<path d="M205,312 L395,312" stroke="${detail}" stroke-width="6"/><path d="M255,320 L225,575 M300,320 L300,578 M345,320 L375,575" stroke="${detail}" stroke-width="2" opacity="0.6"/>`
      : `<path d="M216,320 L384,320" stroke="${detail}" stroke-width="5"/>`;
    return `<g transform="translate(300,420) scale(${s}) translate(-300,-420)"><path d="${shape}" fill="${fill}" stroke="${stroke}" stroke-width="4" stroke-linejoin="round"/>${pat ? `<path d="${shape}" ${pat} opacity="0.85"/>` : ''}${extra}</g>`;
  }
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
  if (pattern === 'stripe') return `<pattern id="pat" width="40" height="36" patternUnits="userSpaceOnUse"><rect width="40" height="12" fill="${c}" opacity="0.5"/></pattern>`;
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
  const bg1 = isLight(fill) ? '#FAF7F2' : shade(fill, 1.0) + '22';
  const lines = wrap(p.name);
  return `<svg xmlns="http://www.w3.org/2000/svg" width="600" height="800" viewBox="0 0 600 800">
<defs>
  <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFFFFF"/><stop offset="1" stop-color="#F3EDE6"/></linearGradient>
  ${patternDef(p.pattern, fill)}
</defs>
<rect width="600" height="800" fill="url(#bg)"/>
<circle cx="300" cy="400" r="250" fill="${fill}" opacity="${isLight(fill) ? 0.25 : 0.12}"/>
${garment(p.type, fill, p.pattern, p.kid)}
<rect x="0" y="690" width="600" height="110" fill="#FFFFFF" opacity="0.9"/>
<text x="300" y="728" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="24" font-weight="700" fill="#1F1B18">${lines[0]}</text>
${lines[1] ? `<text x="300" y="756" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="22" font-weight="600" fill="#1F1B18">${lines[1]}</text>` : ''}
<g transform="translate(300,${lines[1] ? 782 : 758})"><rect x="-70" y="-13" width="140" height="22" rx="11" fill="${fill}" stroke="#D9CFC5" stroke-width="${isLight(fill) ? 1 : 0}"/><text y="4" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="13" font-weight="600" fill="${isLight(fill) ? '#1F1B18' : '#FFFFFF'}">${colorName}</text></g>
<text x="24" y="40" font-family="Segoe UI, Arial, sans-serif" font-size="16" font-weight="800" fill="#1F1B18" opacity="0.7">VT · Việt Thắng</text>
<text x="576" y="40" text-anchor="end" font-family="Segoe UI, Arial, sans-serif" font-size="13" fill="#7D746E">${p.code}</text>
</svg>`;
}

// Ảnh đại diện danh mục: vẽ hình người minh họa (cô gái, phụ nữ trung niên, em bé, chàng trai) mặc đồ màu chủ đạo của nhóm.
const PERSONS = {
  'nu':         { skin: '#F3C9A8', hair: '#4A2E22', style: 'long',  build: 'slim',  bottom: 'capri',  blush: true },
  'trung-nien': { skin: '#EBC3A2', hair: '#6E5B52', style: 'bun',   build: 'full',  bottom: 'long',   glasses: true },
  'tre-em':     { skin: '#F6D2B5', hair: '#3B2A20', style: 'kid',   build: 'kid',   bottom: 'shorts', blush: true },
  'nam':        { skin: '#E8B48C', hair: '#2B2B2B', style: 'short', build: 'broad', bottom: 'shorts' }
};
const BUILDS = { slim: { sw: 88, hw: 76, hr: 56, hry: 62 }, full: { sw: 100, hw: 96, hr: 58, hry: 62 }, broad: { sw: 118, hw: 90, hr: 58, hry: 62 }, kid: { sw: 82, hw: 74, hr: 68, hry: 70 } };

function person(p, fill) {
  const { sw, hw, hr, hry } = BUILDS[p.build];
  const X = 320, HY = 185;
  const stroke = isLight(fill) ? '#D9CFC5' : shade(fill, 0.7);
  const detail = isLight(fill) ? '#E3DAD0' : shade(fill, 0.8);
  const pantsFill = isLight(fill) ? shade(fill, 0.96) : shade(fill, 0.9);
  const hairDark = shade(p.hair, 0.8);
  const bottomY = { shorts: 625, capri: 700, long: 760 }[p.bottom];
  const legW = Math.round(hw * 0.7), legX = Math.round(hw * 0.5);
  const g = [];
  // Tóc phía sau (tóc dài) và búi tóc
  if (p.style === 'long') g.push(`<path d="M${X - hr - 24},${HY - 20} Q${X - hr - 34},330 ${X - hr + 4},345 L${X + hr - 4},345 Q${X + hr + 34},330 ${X + hr + 24},${HY - 20} Q${X},${HY - 120} ${X - hr - 24},${HY - 20} Z" fill="${p.hair}"/>`);
  if (p.style === 'bun') g.push(`<circle cx="${X}" cy="${HY - hry - 14}" r="30" fill="${p.hair}"/><path d="M${X - 18},${HY - hry - 24} Q${X},${HY - hry - 40} ${X + 18},${HY - hry - 24}" stroke="#B8AAA2" stroke-width="4" fill="none" opacity="0.8"/>`);
  // Tay (da) – vẽ trước áo để tay áo đè lên
  g.push(`<path d="M${X - sw + 8},300 L${X - sw - 24},548" stroke="${p.skin}" stroke-width="32" stroke-linecap="round"/>`,
    `<path d="M${X + sw - 8},300 L${X + sw + 24},548" stroke="${p.skin}" stroke-width="32" stroke-linecap="round"/>`);
  // Chân (da) và giày – vẽ trước quần
  g.push(`<path d="M${X - legX},520 L${X - legX - 6},768" stroke="${p.skin}" stroke-width="${legW - 10}" stroke-linecap="round"/>`,
    `<path d="M${X + legX},520 L${X + legX + 6},768" stroke="${p.skin}" stroke-width="${legW - 10}" stroke-linecap="round"/>`,
    `<ellipse cx="${X - legX - 12}" cy="782" rx="36" ry="14" fill="#3A2F2A"/><ellipse cx="${X + legX + 12}" cy="782" rx="36" ry="14" fill="#3A2F2A"/>`);
  // Quần
  g.push(`<path d="M${X - legX},510 L${X - legX - 6},${bottomY}" stroke="${pantsFill}" stroke-width="${legW}" stroke-linecap="round"/>`,
    `<path d="M${X + legX},510 L${X + legX + 6},${bottomY}" stroke="${pantsFill}" stroke-width="${legW}" stroke-linecap="round"/>`,
    `<rect x="${X - hw}" y="490" width="${hw * 2}" height="${legW * 0.9}" fill="${pantsFill}"/>`);
  // Cổ + áo
  g.push(`<rect x="${X - 22}" y="${HY + hry - 16}" width="44" height="52" rx="10" fill="${p.skin}"/>`);
  const shirt = `M${X - sw},276 Q${X},256 ${X + sw},276 L${X + sw + 38},392 L${X + sw - 2},412 L${X + hw + 6},500 L${X - hw - 6},500 L${X - sw + 2},412 L${X - sw - 38},392 Z`;
  g.push(`<path d="${shirt}" fill="${fill}" stroke="${stroke}" stroke-width="4" stroke-linejoin="round"/>`,
    `<path d="M${X - 28},274 Q${X},302 ${X + 28},274" fill="none" stroke="${detail}" stroke-width="7"/>`,
    `<path d="M${X},300 L${X},494" stroke="${detail}" stroke-width="2" opacity="0.5"/>`,
    `<path d="M${X - hw - 2},500 L${X + hw + 2},500" stroke="${detail}" stroke-width="5"/>`);
  // Đầu, tóc trước, mặt
  g.push(`<ellipse cx="${X}" cy="${HY}" rx="${hr}" ry="${hry}" fill="${p.skin}"/>`);
  const fringeY = p.style === 'short' ? HY - 34 : HY - 22;
  g.push(`<path d="M${X - hr - 3},${HY} A${hr + 3},${hry + 3} 0 0 1 ${X + hr + 3},${HY} L${X + hr - 8},${fringeY} Q${X + hr - 30},${fringeY - 22} ${X},${fringeY - 10} Q${X - hr + 20},${fringeY - 6} ${X - hr + 8},${fringeY + 8} Z" fill="${p.hair}"/>`);
  if (p.style === 'kid') g.push(`<path d="M${X - 10},${HY - hry - 2} Q${X - 4},${HY - hry - 34} ${X + 14},${HY - hry - 28} Q${X + 6},${HY - hry - 18} ${X + 12},${HY - hry - 2} Z" fill="${p.hair}"/>`);
  if (p.style === 'long') g.push(`<path d="M${X - hr - 3},${HY} Q${X - hr - 6},${HY + 60} ${X - hr + 14},${HY + 90}" stroke="${hairDark}" stroke-width="3" fill="none" opacity="0.5"/>`);
  const ey = HY + 6, ex = p.build === 'kid' ? 26 : 22;
  g.push(`<ellipse cx="${X - ex}" cy="${ey}" rx="5" ry="7" fill="#2B2B2B"/><ellipse cx="${X + ex}" cy="${ey}" rx="5" ry="7" fill="#2B2B2B"/>`);
  const browW = p.build === 'broad' ? 4 : 2.5;
  g.push(`<path d="M${X - ex - 10},${ey - 18} Q${X - ex},${ey - 24} ${X - ex + 10},${ey - 18}" stroke="${hairDark}" stroke-width="${browW}" fill="none"/><path d="M${X + ex - 10},${ey - 18} Q${X + ex},${ey - 24} ${X + ex + 10},${ey - 18}" stroke="${hairDark}" stroke-width="${browW}" fill="none"/>`);
  g.push(`<path d="M${X - 16},${HY + 30} Q${X},${HY + 46} ${X + 16},${HY + 30}" stroke="#B05C5C" stroke-width="3.5" fill="none" stroke-linecap="round"/>`);
  if (p.blush) g.push(`<circle cx="${X - hr + 18}" cy="${HY + 24}" r="9" fill="#F09A9A" opacity="0.45"/><circle cx="${X + hr - 18}" cy="${HY + 24}" r="9" fill="#F09A9A" opacity="0.45"/>`);
  if (p.glasses) g.push(`<circle cx="${X - ex}" cy="${ey}" r="17" fill="none" stroke="#4A3F3A" stroke-width="3"/><circle cx="${X + ex}" cy="${ey}" r="17" fill="none" stroke="#4A3F3A" stroke-width="3"/><path d="M${X - ex + 17},${ey} L${X + ex - 17},${ey}" stroke="#4A3F3A" stroke-width="3"/>`);
  const scale = p.build === 'kid' ? 0.8 : 1;
  return `<g transform="translate(${X},792) scale(${scale}) translate(${-X},-792)">${g.join('')}</g>`;
}

function categorySvg(c) {
  const p = PERSONS[c.slug];
  return `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="800" viewBox="0 0 640 800">
<defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${shade(c.color, 1.15)}" stop-opacity="0.35"/><stop offset="1" stop-color="${c.color}" stop-opacity="0.75"/></linearGradient></defs>
<rect width="640" height="800" fill="#FAF7F2"/><rect width="640" height="800" fill="url(#bg)"/>
<circle cx="320" cy="330" r="250" fill="#FFFFFF" opacity="0.28"/>
${p ? person(p, c.color) : `<g transform="translate(320,0) scale(0.72) translate(-300,20)">${garment(c.type, c.color, 'none', c.kid)}</g>`}
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
