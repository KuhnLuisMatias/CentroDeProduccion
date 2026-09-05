const BASE = "http://localhost:5000/api";

async function req(method, path, body, token) {
  const res = await fetch(BASE + path, {
    method,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  return { status: res.status, ok: res.ok, data };
}

const login = await req("POST", "/auth/login", null ? null : {
  email: "admin@centro.com",
  password: "Centro2026!",
});
const token = login.data.token;
if (!token) throw new Error("Login failed: " + JSON.stringify(login));
console.log("Logged in.");

// ---------- Units ----------
const TIPO = { Masa: 1, Volumen: 2, Conteo: 3 };
const CANONICAL_UNITS = [
  { nombre: "Kilogramo", simbolo: "Kg", tipo: TIPO.Masa },
  { nombre: "Litro", simbolo: "Lt", tipo: TIPO.Volumen },
  { nombre: "Unidad", simbolo: "Un", tipo: TIPO.Conteo },
  { nombre: "Metro", simbolo: "Mt", tipo: TIPO.Conteo },
  { nombre: "Paquete", simbolo: "Paq", tipo: TIPO.Conteo },
];

const existingUnitsRes = await req("GET", "/unidadesmedida", null, token);
const existingUnits = Array.isArray(existingUnitsRes.data) ? existingUnitsRes.data : [];
const unitBySimbolo = new Map(existingUnits.map((u) => [String(u.simbolo).toUpperCase(), u]));
let unitsCreated = 0;
for (const u of CANONICAL_UNITS) {
  if (!unitBySimbolo.has(u.simbolo.toUpperCase())) {
    const r = await req("POST", "/unidadesmedida", u, token);
    if (r.ok) {
      unitBySimbolo.set(u.simbolo.toUpperCase(), r.data);
      unitsCreated++;
    } else {
      console.log(`UNIT FAIL ${u.simbolo}: ${r.status} ${JSON.stringify(r.data)}`);
    }
  }
}
console.log(`Units: ${existingUnits.length} existed, ${unitsCreated} created.`);

function unitFor(raw) {
  const s = String(raw || "").trim().toUpperCase();
  if (["KG", "GR", "G"].includes(s)) return "KG";
  if (["LT", "LTS", "CC", "ML"].includes(s)) return "LT";
  if (["UN", "UNI", "U", "AD"].includes(s)) return "UN";
  if (s === "MT") return "MT";
  if (["PAQ", "ROLLO", "CJ", "BIDON", "LATA", "BOT", "SACHET", "POUCH", "BOLSA", "BALDE", "PILON"].includes(s)) return "PAQ";
  return "UN";
}

// ---------- Categories ----------
const INSUMO_CATS = ["ALMACEN", "CARNES", "LACTEOS", "CONDIMENTOS", "LIQUIDOS", "VERDURAS FRUTAS", "DESCARTABLES", "REVENTA"];
const PRODUCTO_CATS = ["MASA-LEUDADO", "MOLIDA-MEDALLONES", "MILANESAS", "LOMO-SANGUCHERIA", "POLLO FRITO", "REBOZADOS", "TARTAS", "FRACCIONADOS", "ENCURTIDOS-SALSAS", "EVENTOS"];

const catsRes = await req("GET", "/categorias", null, token);
const catMap = new Map();
for (const c of [...(catsRes.data.Insumos || []), ...(catsRes.data.ProductosTerminados || [])]) {
  catMap.set(String(c.nombre).toUpperCase(), c);
}
let catsCreated = 0;
for (const nombre of INSUMO_CATS) {
  if (!catMap.has(nombre)) {
    const r = await req("POST", "/categorias", { nombre, ambito: 1 }, token);
    if (r.ok) { catMap.set(nombre, r.data); catsCreated++; }
    else console.log(`CAT FAIL ${nombre}: ${r.status} ${JSON.stringify(r.data)}`);
  }
}
for (const nombre of PRODUCTO_CATS) {
  if (!catMap.has(nombre)) {
    const r = await req("POST", "/categorias", { nombre, ambito: 2 }, token);
    if (r.ok) { catMap.set(nombre, r.data); catsCreated++; }
    else console.log(`CAT FAIL ${nombre}: ${r.status} ${JSON.stringify(r.data)}`);
  }
}
console.log(`Categories: created ${catsCreated}, total known ${catMap.size}.`);

// ---------- Load extraction ----------
import { readFileSync } from "node:fs";
const doc = JSON.parse(readFileSync("../docs/insumos-extraidos.json", "utf8"));

function norm(s) {
  return String(s || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toUpperCase()
    .replace(/\s+/g, " ")
    .trim();
}

// Known merges -> canonical key
function canonicalKey(n) {
  if (/^QUEPERI\b/.test(n)) return "QUEPERI";
  if (/^PASTA MANI/.test(n) || /^PASTA DE MANI/.test(n)) return "PASTA DE MANI";
  if (/^CERVEZA NAGRA/.test(n)) return "CERVEZA NEGRA";
  if (/^MURARELLA/.test(n) || /^MUZZARELLA BLANDA/.test(n) || /^MUZARELLA BLANDA/.test(n)) return "MUZZARELLA CILINDRO";
  if (/^MUZARELLA DURA/.test(n) || /^MUZZARELLA DURA/.test(n)) return "MUZZARELLA BARRA";
  if (/^ZAPALLO BRASILERITO?/.test(n)) return "ZAPALLO BRASILERO";
  if (/^ZAPALLO COREANITO/.test(n)) return "ZAPALLO COREANO";
  if (/^NUECES?$/.test(n)) return "NUEZ MARIPOSA BCA";
  if (/^TYBO$/.test(n) || /^QUESO TYBO$/.test(n)) return "QUESO DANBO/TYBO";
  return n;
}

const CARNES_RE = /\b(NALGA|TAPA ASADO|AGUJA|PRIMO|PECHO|BIFE|BONDIOLA|FALDA|PANCETA|JAMON|SALAME|CANTIMPALO|QUEPERI|FILET POLLO|CARNE|ASADO|VACIO|MATAMBRE|CHORIZO|MORCILLA|CERDO|POLLO|RIBS|LOMO)\b/;
const LACTEOS_RE = /\b(QUESO|MUZZARELLA|MANTECA|CREMA DE LECHE|DANBO|CHEDDAR|SARDO|TYBO|AZUL)\b/;
const DESCARTABLES_RE = /\b(BALDE|ETIQUETA|CAJITA|BANDEJA|OBLEA|FILM|BOLSA)\b/;

function resolveCategory(cat, name) {
  const c = norm(cat);
  const n = norm(name);
  if (c === "ALMACEN") return "ALMACEN";
  if (c === "CARNES") return "CARNES";
  if (c.startsWith("LACTEOS")) return "LACTEOS";
  if (c === "CONDIMENTOS") return "CONDIMENTOS";
  if (c === "LIQUIDOS") return "LIQUIDOS";
  if (c === "VERDURAS" || c === "VERDURAS/FRUTAS") return "VERDURAS FRUTAS";
  if (c === "DESCARTABLES" || c.startsWith("DESCARTABLES/PACKAGING")) return "DESCARTABLES";
  if (c.startsWith("REVENTA")) return "REVENTA";
  if (c.includes("LISTA PROVEEDOR")) {
    if (DESCARTABLES_RE.test(n)) return "DESCARTABLES";
    if (LACTEOS_RE.test(n)) return "LACTEOS";
    if (CARNES_RE.test(n)) return "CARNES";
    return "REVENTA";
  }
  if (c === "POR CLASIFICAR") {
    if (/^AGUA/.test(n)) return "LIQUIDOS";
    if (/CEBOLLA CRISPY/.test(n) || /NUEZ/.test(n)) return "ALMACEN";
    if (/BOLSITA COND/.test(n)) return "DESCARTABLES";
    if (/PULLED PORK/.test(n)) return "CARNES";
    return "ALMACEN";
  }
  return "ALMACEN";
}

// ---------- Dedupe & merge ----------
const groups = new Map();
const mergesApplied = [];
for (const item of doc.insumos) {
  const key = canonicalKey(norm(item.nombre));
  const isQueMila = /QUE_MILA/i.test(item.fuente || "");
  if (!groups.has(key)) {
    groups.set(key, []);
  }
  groups.get(key).push({ ...item, _key: key, _isQueMila: isQueMila });
}

const finalItems = [];
for (const [key, items] of groups) {
  items.sort((a, b) => (b._isQueMila - a._isQueMila)); // QUE_MILA first
  const best = items[0];
  const mergedFrom = items.length;
  if (mergedFrom > 1) mergesApplied.push(`${items.map(i => i.nombre).join(" + ")} -> ${best.nombre}`);
  finalItems.push({
    nombre: mergedFrom > 1 && canonicalKey(norm(best.nombre)) !== norm(best.nombre)
      ? titleCaseCanonical(key)
      : best.nombre,
    precio: best.precio,
    fuente: best.fuente,
    unidadRaw: best.unidad,
    categoria: resolveCategory(best.categoria_sugerida, best.nombre),
    _mergedCount: mergedFrom,
  });
}

function titleCaseCanonical(key) {
  const overrides = {
    QUEPERI: "QUEPERÍ",
    "PASTA DE MANI": "PASTA DE MANÍ",
    "CERVEZA NEGRA": "CERVEZA NEGRA",
    "MUZZARELLA CILINDRO": "MUZZARELLA CILINDRO",
    "MUZZARELLA BARRA": "MUZZARELLA BARRA",
    "NUEZ MARIPOSA BCA": "NUEZ MARIPOSA BCA",
    "QUESO DANBO/TYBO": "QUESO DANBO/TYBO",
  };
  return overrides[key] || key;
}

console.log(`Extraction: ${doc.insumos.length} raw insumos -> ${finalItems.length} after dedup/merge (${mergesApplied.length} merge rules fired).`);

// ---------- Create insumos ----------
let skuCounter = 0;
const failures = [];
let created = 0;
for (const it of finalItems) {
  skuCounter += 1;
  const codigoSku = `INS-${String(skuCounter).padStart(4, "0")}`;
  const unidad = unitBySimbolo.get(unitFor(it.unidadRaw));
  const cat = catMap.get(it.categoria);
  if (!unidad || !cat) {
    failures.push({ nombre: it.nombre, error: `missing unidad/cat (${it.unidadRaw}/${it.categoria})` });
    continue;
  }
  const obs = it.fuente;
  const payload = {
    nombre: it.nombre,
    codigoSku,
    categoriaId: cat.id,
    unidadCompraId: unidad.id,
    unidadConsumoId: unidad.id,
    factorConversion: 1,
    presentacion: 1,
    stockMinimo: 0,
    proveedorPrincipalId: null,
    observaciones: obs,
    precioUltimaCompra: it.precio ?? 0,
  };
  const r = await req("POST", "/insumos", payload, token);
  if (r.ok || r.status === 201) {
    created++;
  } else {
    failures.push({ nombre: it.nombre, sku: codigoSku, status: r.status, error: JSON.stringify(r.data) });
  }
}

console.log(`Created ${created}/${finalItems.length} insumos.`);
if (failures.length) {
  console.log("FAILURES:");
  for (const f of failures) console.log(JSON.stringify(f));
}

// ---------- Verify ----------
const allInsumos = [];
let page = 1;
while (true) {
  const r = await req("GET", `/insumos?page=${page}&pageSize=100`, null, token);
  const d = r.data;
  const items = d.items || d.Items || [];
  allInsumos.push(...items);
  const total = d.totalCount ?? d.TotalCount ?? d.total ?? allInsumos.length;
  if (allInsumos.length >= total || items.length === 0) break;
  page++;
}
console.log(`VERIFY: GET /insumos total = ${allInsumos.length} (paged through ${page} pages).`);
const finalCats = await req("GET", "/categorias", null, token);
const insCats = finalCats.data?.Insumos?.length ?? "?";
const ptCats = finalCats.data?.ProductosTerminados?.length ?? "?";
console.log(`VERIFY: categorias insumos=${insCats}, productos=${ptCats}`);
const finalUnits = await req("GET", "/unidadesmedida", null, token);
console.log(`VERIFY: unidades=${Array.isArray(finalUnits.data) ? finalUnits.data.length : "?"}`);
