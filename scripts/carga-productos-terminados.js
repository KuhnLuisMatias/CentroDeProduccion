const fs = require('fs');
const path = require('path');
const BASE = 'http://localhost:5000/api';

const insumosDoc = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'docs', 'insumos-extraidos.json'), 'utf8'));
const recetasDoc = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'docs', 'recetas-extraidas.json'), 'utf8'));

// ---------- normalization ----------
function deaccent(s) {
  return s.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
}
function normRaw(s) {
  return deaccent(String(s)).toUpperCase().replace(/[^A-Z0-9]+/g, ' ').trim();
}
const STOP = new Set(['X', 'DE', 'DEL', 'PARA', 'P', 'CON', 'SOLO', 'ST', 'EL', 'LA', 'LOS', 'LAS', 'Y', 'ESTILO', 'GR', 'GRS', 'G', 'KG', 'U', 'UNI']);
function stem(t) {
  if (t.length > 5 && t.endsWith('ES')) return t.slice(0, -2); // MEDALLONES -> MEDALLON
  return t.length > 3 && t.endsWith('S') ? t.slice(0, -1) : t;
}
function keyOf(rawName) {
  const n = normRaw(rawName).replace(/\bCUATRO\b/g, '4');
  const toks = [];
  for (const t of n.split(' ')) {
    if (!t) continue;
    if (/^X?\d+(GR|GRS|G|KG|ML|U|UNI)?$/.test(t)) continue; // weight-like tokens (incl. x100g / X60GR)
    if (STOP.has(t)) continue;
    toks.push(stem(t));
  }
  return toks;
}
function gramsOf(rawName) {
  const m = normRaw(rawName).match(/(\d+)\s*(?:GR|GRS|G)\b/) || normRaw(rawName).match(/\b(\d+)\s*(?:GR|GRS|G)\b/);
  return m ? parseInt(m[1], 10) : null;
}

// Manual alias map: QUE_MILA commercial base tokens (keyOf-joined) -> receta normalized name
const ALIAS = new Map([
  [keyOf('POLLO BROASTER MIX').join(' '), normRaw('POLLO BROASTER - ALITAS POLLO')],
  [keyOf('MINI HAMBURGUESA x 40g').join(' '), normRaw('HAMBURGUESAS 40 GR')],
  [keyOf('PRE PIZZA LENGÜETA').join(' '), normRaw('PIZZA LENGUETAS Mr. y Cristo B')],
  [keyOf('POLLO CORTE MARIPOSA').join(' '), normRaw('POLLO MARIPOSA X 220GRS')],
  [keyOf('MUZZA FRACCIONADA P/MILANESAS').join(' '), normRaw('MUZZARELLA FRACCIONADA P/MILANESAS X 60GRS')],
  [keyOf('SANGUCHERIA- MILA DE CARNE 190 grs').join(' '), normRaw('MILANESAS')],
]);

// ---------- build receta catalog (dedup) ----------
const recetas = [];
const seenRecetaKeys = new Set();
for (const r of recetasDoc.recetas) {
  const nr = normRaw(r.nombre);
  if (/\bCORRIDA\b/.test(nr) || /\bSECCION\b/.test(nr)) continue; // duplicate sheet sections
  const k = keyOf(r.nombre).join('|');
  if (seenRecetaKeys.has(k)) continue;
  seenRecetaKeys.add(k);
  recetas.push({ nombre: r.nombre, norm: nr, key: keyOf(r.nombre), hoja: r.hoja });
}

// hoja -> categoria PT
const HOJA2CAT = {
  'MASA-LEUDADO': 'MASA-LEUDADO',
  'MOLIDA-MEDALLONES': 'MOLIDA-MEDALLONES',
  'MILANESAS 24st': 'MILANESAS',
  'MILANESAS (sang)': 'MILANESAS',
  'LOMO (sang)': 'LOMO-SANGUCHERIA',
  'POLLO NUEVO': 'POLLO FRITO',
  'POLLO FRITO': 'POLLO FRITO',
  'REBOZADOS': 'REBOZADOS',
  'TARTAS': 'TARTAS',
  'FRACCIONADOS': 'FRACCIONADOS',
  'ENCURTIDOS-SALSAS': 'ENCURTIDOS-SALSAS',
  'EVENTOS': 'EVENTOS',
};

// ---------- match QUE_MILA item -> receta ----------
function matchReceta(qmNombre) {
  const base = normRaw(qmNombre);
  // strip trailing "(EVENTO)" markers already gone via tokens; alias lookup on key tokens minus weights
  const ktoks = keyOf(qmNombre);
  // try alias by progressively shorter token prefixes
  for (let l = ktoks.length; l >= 1; l--) {
    const probe = ktoks.slice(0, l).join(' ');
    if (ALIAS.has(probe)) {
      const targetNorm = ALIAS.get(probe);
      const target = recetas.find(r => r.norm === targetNorm);
      if (target) return { receta: target, via: 'alias' };
    }
  }
  const kset = new Set(ktoks);
  const g = gramsOf(qmNombre);
  let best = null;
  for (const r of recetas) {
    const rset = new Set(r.key);
    const rg = gramsOf(r.nombre);
    const gramClash = g != null && rg != null && g !== rg;
    let overlap = 0, extra = 0;
    for (const t of kset) { if (rset.has(t)) overlap++; }
    for (const t of rset) { if (!kset.has(t)) extra++; }
    const missing = kset.size - overlap;
    let contained = missing === 0 || overlap === rset.size; // item⊆receta or receta⊆item
    if (!contained || overlap === 0) continue;
    let score = overlap * 10 - Math.abs(rset.size - kset.size);
    if (g != null && rg === g) score += 8;
    if (missing === 0 && extra === 0 && !gramClash) score += 30; // exact token-set
    if (gramClash) score -= 40; // explicit different weights -> not the same product
    if (!best || score > best.score) best = { receta: r, score };
  }
  return best ? { receta: best.receta, via: 'auto' } : null;
}

// ---------- category fallback by keywords ----------
function catFallback(nombre) {
  const n = normRaw(nombre);
  if (/^SALSA\b|PEPINOS/.test(n)) return 'ENCURTIDOS-SALSAS';
  if (/^SANGUCHERIA|^LOMO\b|MEN U?$/.test(n) || /^LOMO MENU/.test(n)) return 'LOMO-SANGUCHERIA';
  if (/^RECORTES.*MILANESA/.test(n)) return 'MILANESAS';
  if (/^PRE PIZZA|^PIZZA|^FOCACCIA|^PAN |^CANAST|^FAJITA|^NACHOS|^BROWNIE|^MINI TORTA|^MASA/.test(n)) return 'MASA-LEUDADO';
  if (/^POLLO/.test(n)) return 'POLLO FRITO';
  if (/^TARTA/.test(n)) return 'TARTAS';
  return 'MOLIDA-MEDALLONES';
}

// ---------- unit selection ----------
function unidadFor(nombre, qmUnidad) {
  const n = normRaw(nombre);
  if (qmUnidad === 'Kg') return 'Kg';
  if (/^(SALSA|MARINADO|PREPARADO|PULLED)|\bMASA TARTA\b|PEPINOS|NACHOS/.test(n)) return 'Kg';
  return 'Un';
}

// ---------- unified list ----------
const unified = []; // {nombre, categoria, unidad, precioVenta, producidoPorReceta}
const seenNames = new Set();
const matchedRecetaNames = new Set();
function push(pt) {
  const k = keyOf(pt.nombre).join('|');
  if (seenNames.has(k)) return false;
  seenNames.add(k);
  unified.push(pt);
  return true;
}

for (const p of insumosDoc.productos_terminados_detectados) {
  const nombre = p.nombre.replace(/\s*\[[^\]]+\]\s*$/, '').replace(/\s{2,}/g, ' ').trim();
  const m = matchReceta(nombre);
  if (m) matchedRecetaNames.add(m.receta.nombre);
  const cat = m ? HOJA2CAT[m.receta.hoja] : catFallback(nombre);
  push({
    nombre,
    fuente: 'QUE_MILA+recetas',
    categoria: cat,
    unidad: unidadFor(nombre, p.unidad),
    precioVenta: p.precio,
    producidoPorReceta: m ? m.receta.nombre : null,
    matchVia: m ? m.via : null,
  });
}

for (const r of recetas) {
  if (matchedRecetaNames.has(r.nombre)) continue; // absorbed by its QUE_MILA commercial counterpart
  const k = keyOf(r.nombre).join('|');
  if (seenNames.has(k)) continue;
  push({
    nombre: r.nombre,
    fuente: 'recetas-only',
    categoria: HOJA2CAT[r.hoja],
    unidad: unidadFor(r.nombre, null),
    precioVenta: null,
    producidoPorReceta: r.nombre,
    matchVia: null,
  });
}

console.log('Unified PT count:', unified.length);

// ---------- API ----------
async function main() {
  const login = await post('/auth/login', { email: 'admin@centro.com', password: 'Centro2026!' });
  TOKEN = login.token;

  const cats = await get('/categorias?ambito=2');
  const catByName = new Map(cats.map(c => [normRaw(c.nombre), c.id]));
  const ums = await get('/unidadesmedida');
  const umBySym = new Map(ums.map(u => [u.simbolo, u.id]));

  const existing = await get('/productoterminado');
  const existingKeys = new Set(existing.map(p => keyOf(p.nombre).join('|')));
  console.log('PT already in DB:', existing.length);

  let skuCounter = 1;
  const created = [], failed = [], skipped = [];
  for (const pt of unified) {
    const k = keyOf(pt.nombre).join('|');
    if (existingKeys.has(k)) { skipped.push(pt.nombre); continue; }
    const categoriaId = catByName.get(normRaw(pt.categoria));
    const unidadMedidaId = umBySym.get(pt.unidad) || umBySym.get('Un');
    if (!categoriaId) { failed.push({ nombre: pt.nombre, error: 'categoria no encontrada: ' + pt.categoria }); continue; }
    const codigoSku = 'PT-' + String(skuCounter).padStart(4, '0');
    skuCounter++;
    try {
      const res = await post('/productoterminado', { nombre: pt.nombre, codigoSku, categoriaId, unidadMedidaId }, true);
      created.push({ ...pt, id: res.id ?? res.Id, codigoSku });
    } catch (e) {
      skuCounter--; // reuse sku next attempt
      failed.push({ nombre: pt.nombre, codigoSku, error: String(e.message).slice(0, 300) });
    }
  }

  // verify
  const after = await get('/productoterminado');
  const byCat = {};
  for (const c of cats) byCat[c.nombre] = 0;
  for (const p of after) {
    const cid = p.categoriaId || (p.categoria && p.categoria.id);
    const cn = cats.find(c => c.id === cid);
    if (cn) byCat[cn.nombre]++;
  }
  const report = {
    unifiedCount: unified.length,
    createdCount: created.length,
    failedCount: failed.length,
    skippedAlreadyInDb: skipped.length,
    totalAfter: after.length,
    conPrecioVentaQueMila: unified.filter(u => u.precioVenta > 0).length,
    conReceta: unified.filter(u => u.producidoPorReceta).length,
    byCat,
    matches: unified.filter(u => u.producidoPorReceta).map(u => `${u.nombre} => ${u.producidoPorReceta}${u.matchVia === 'alias' ? ' [alias]' : ''}`),
    sinReceta: unified.filter(u => !u.producidoPorReceta).map(u => u.nombre),
    failures: failed,
  };
  fs.writeFileSync(path.join(__dirname, '..', 'docs', 'pt-carga-reporte.json'), JSON.stringify(report, null, 2));
  console.log(JSON.stringify(report, null, 2));
}

let TOKEN = '';
async function post(path_, body, expectBody) {
  const res = await fetch(BASE + path_, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + TOKEN },
    body: JSON.stringify(body),
  });
  const text = await res.text();
  if (!res.ok) throw new Error(res.status + ' ' + text.slice(0, 250));
  return text ? JSON.parse(text) : null;
}
async function get(path_) {
  const res = await fetch(BASE + path_, { headers: { Authorization: 'Bearer ' + TOKEN } });
  if (!res.ok) throw new Error('GET ' + path_ + ' -> ' + res.status);
  return res.json();
}

main().catch(e => { console.error(e); process.exit(1); });
