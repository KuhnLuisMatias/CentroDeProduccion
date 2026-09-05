// Seed consistent fictitious initial stock for ALL insumos via the API.
// Every entry is a real Compra movement (TipoMovimientoStock.Compra = 1) so ledgers,
// weighted averages and traceability stay coherent.
//
// Usage: node seed-stock.mjs [--dry-run]
const BASE = 'http://localhost:5000';
const EMAIL = 'admin@centro.com';
const PASSWORD = 'Centro2026!';
const DRY_RUN = process.argv.includes('--dry-run');

async function api(path, options = {}, token) {
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers || {}),
    },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`${options.method || 'GET'} ${path} -> ${res.status}: ${body.slice(0, 300)}`);
  }
  return res.status === 204 ? null : res.json();
}

async function login() {
  const j = await api('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email: EMAIL, password: PASSWORD }),
  });
  if (!j.token) throw new Error('Login succeeded but no token returned');
  return j.token;
}

async function fetchAllInsumos(token) {
  const all = [];
  let page = 1;
  while (true) {
    const data = await api(`/api/insumos?page=${page}&pageSize=100`, {}, token);
    const items = data.items || data.data || [];
    all.push(...items);
    const total = data.totalCount ?? all.length;
    console.log(`Fetched page ${page}: ${items.length} items (collected ${all.length}/${total})`);
    if (all.length >= total || items.length === 0) break;
    page++;
    if (page > 20) throw new Error('Pagination runaway guard');
  }
  return all;
}

function hashName(name) {
  let h = 0;
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0;
  return h;
}

// Deterministic pseudo-random in [0,1) from name hash.
function rand01(name) {
  return (hashName(name.toUpperCase()) % 1000) / 1000;
}

function round05(x) {
  // Round to nearest integer or .5
  return Math.round(x * 2) / 2;
}

// Quantity rules per unit + price band, with deterministic +/-15% variation.
function computeQty(insumo) {
  const nombre = (insumo.nombre || '').toUpperCase();
  const simbolo = (insumo.unidadConsumo?.simbolo || insumo.unidadConsumo?.nombre || '').toUpperCase();
  const categoria = (insumo.categoria?.nombre || '').toUpperCase();
  const price = Number(insumo.precioUltimaCompra ?? insumo.precioPromedioPonderado ?? 0);
  const r = rand01(nombre); // 0..1 -> variation factor 0.85..1.15
  const v = 0.85 + r * 0.3;

  let base;
  if (simbolo === 'KG') {
    if (price < 1500) base = 45;
    else if (price <= 5000) base = 27.5;
    else if (price <= 12000) base = 14;
    else base = 7;
  } else if (simbolo === 'LT') {
    base = price < 1500 ? 25 : 12;
  } else if (simbolo === 'UN') {
    if (nombre.includes('HUEVO') || nombre.includes('MAPLE')) base = 150;
    else if (categoria.includes('DESCARTABLE')) base = 275;
    else base = 75;
  } else if (simbolo === 'MT') {
    base = 50;
  } else {
    // Paq / Cj / Bidon / other package units
    base = 20;
  }

  // Special demo case
  if (nombre.includes('NALGA') && nombre.includes('SIN TAPA')) return 8;

  return round05(base * v);
}

function targetMinimo(insumo, qty) {
  const nombre = (insumo.nombre || '').toUpperCase();
  if (nombre.includes('NALGA') && nombre.includes('SIN TAPA')) return 20;
  return Math.round(qty * 0.3);
}

// Fallback fictitious price when the insumo has no recorded price (API rejects precioUnitario<=0).
function fallbackPrice(insumo) {
  const categoria = (insumo.categoria?.nombre || '').toUpperCase();
  const simbolo = (insumo.unidadConsumo?.simbolo || '').toUpperCase();
  if (categoria.includes('CARNE')) return 9500;
  if (simbolo === 'KG') return 3000;
  if (simbolo === 'UN') return 800;
  return 2500;
}

async function main() {
  console.log(`Seeding initial stock at ${BASE}${DRY_RUN ? ' (DRY RUN)' : ''}`);
  const token = await login();
  console.log('Logged in.');

  const insumosRaw = await fetchAllInsumos(token);
  const insumos = insumosRaw.filter((i) => i.activo !== false);
  console.log(`Active insumos: ${insumos.length} (of ${insumosRaw.length} total)`);

  const failures = [];
  const skipped = [];
  const registered = [];
  const minimaFailures = [];
  let minimaUpdated = 0;

  for (let idx = 0; idx < insumos.length; idx++) {
    const ins = insumos[idx];
    const qty = computeQty(ins);
    let price = Number(ins.precioUltimaCompra ?? ins.precioPromedioPonderado ?? 0);

    try {
      if (Number(ins.stockActual) !== 0 || Number(ins.cantidadAcumuladaCompras ?? 0) !== 0) {
        skipped.push({ nombre: ins.nombre, stockActual: ins.stockActual });
        continue;
      }
      if (!DRY_RUN) {
        const post = (p) =>
          api(
            '/api/stock/movement',
            {
              method: 'POST',
              body: JSON.stringify({
                insumoId: ins.id,
                productoTerminadoId: null,
                tipo: 1,
                cantidad: qty,
                unidadOriginalId: ins.unidadConsumoId,
                precioUnitario: p,
                motivo: 'Inventario inicial',
                documentoOrigen: null,
              }),
            },
            token,
          );
        try {
          await post(price);
        } catch (e) {
          if (!/PrecioUnitario/.test(e.message)) throw e;
          price = fallbackPrice(ins); // retry with plausible fictitious price
          await post(price);
        }
      }
      registered.push({ id: ins.id, nombre: ins.nombre, qty, price });
      process.stdout.write(`[${idx + 1}/${insumos.length}] ${ins.nombre}: +${qty}\n`);
    } catch (e) {
      failures.push({ nombre: ins.nombre, error: e.message });
      console.error(`FAIL ${ins.nombre}: ${e.message}`);
      continue;
    }

    // Set StockMinimo via UpdateInsumoCommand (fresh rowVersion after the movement).
    if (!DRY_RUN) {
      try {
        const fresh = await api(`/api/insumos/${ins.id}`, {}, token);
        await api(
          `/api/insumos/${ins.id}`,
          {
            method: 'PUT',
            body: JSON.stringify({
              id: ins.id,
              nombre: fresh.nombre,
              codigoSku: fresh.codigoSku,
              categoriaId: fresh.categoriaId,
              unidadCompraId: fresh.unidadCompraId,
              unidadConsumoId: fresh.unidadConsumoId,
              factorConversion: fresh.factorConversion,
              presentacion: fresh.presentacion ?? 1,
              stockMinimo: targetMinimo(ins, qty),
              proveedorPrincipalId: fresh.proveedorPrincipalId,
              observaciones: fresh.observaciones,
              rowVersion: fresh.rowVersion,
              precioUltimaCompra: fresh.precioUltimaCompra,
            }),
          },
          token,
        );
        minimaUpdated++;
      } catch (e) {
        minimaFailures.push({ nombre: ins.nombre, error: e.message });
        console.error(`MINIMO FAIL ${ins.nombre}: ${e.message}`);
      }
    }
  }

  console.log('\n=== SUMMARY ===');
  console.log(`Registered: ${registered.length}, Skipped (already had stock): ${skipped.length}, Failed: ${failures.length}`);
  if (failures.length) {
    console.log('Failures:');
    for (const f of failures) console.log(` - ${f.nombre}: ${f.error}`);
  }
  if (minimaFailures.length) {
    console.log('Minima update failures:');
    for (const f of minimaFailures) console.log(` - ${f.nombre}: ${f.error}`);
  }
  if (skipped.length) {
    console.log('Skipped:');
    for (const s of skipped) console.log(` - ${s.nombre} (stockActual=${s.stockActual})`);
  }

  // Valuation across all active insumos (server-side truth).
  const final = await fetchAllInsumos(token);
  let valuation = 0;
  const belowMinimum = [];
  for (const i of final.filter((x) => x.activo !== false)) {
    valuation += Number(i.stockActual) * Number(i.precioPromedioPonderado ?? 0);
    if (Number(i.stockActual) <= Number(i.stockMinimo)) belowMinimum.push(i);
  }
  console.log(`Total valuation (sum stockActual x precioPromedioPonderado): $${valuation.toFixed(2)}`);
  console.log(`Below/equal minimum (${belowMinimum.length}):`);
  for (const b of belowMinimum) {
    console.log(` - ${b.nombre}: stock=${b.stockActual} min=${b.stockMinimo}`);
  }

  // Spot-checks: pick varied items by keyword.
  const spotKeywords = [
    { label: 'bulk dry', test: (n) => /HARINA|AZUCAR|AZÚCAR|SAL GRUESA|POLENTA/.test(n) },
    { label: 'expensive meat', test: (n, c) => c.includes('CARNE') && /NALGA|LOMO|ASADO/.test(n) },
    { label: 'spice', test: (n) => /LAUREL|PIMIENTA NEGRA/.test(n) },
    { label: 'egg', test: (n) => /HUEVO|MAPLE/.test(n) },
    { label: 'disposable', test: (n, c) => c.includes('DESCARTABLE') },
    { label: 'liquid', test: (n, s) => s === 'LT' },
  ];
  console.log('\n=== SPOT CHECKS ===');
  for (const k of spotKeywords) {
    const found = final.find(
      (i) =>
        i.activo !== false &&
        k.test((i.nombre || '').toUpperCase(), (i.categoria?.nombre || '').toUpperCase(), (i.unidadConsumo?.simbolo || '').toUpperCase()),
    );
    if (!found) {
      console.log(` - [${k.label}] no matching insumo found`);
      continue;
    }
    const detail = await api(`/api/insumos/${found.id}`, {}, token);
    const posted = registered.find((r) => r.id === found.id);
    const match =
      posted != null &&
      Math.abs(Number(detail.stockActual) - posted.qty) < 0.001 &&
      Math.abs(Number(detail.precioPromedioPonderado ?? 0) - posted.price) < 0.01;
    console.log(
      ` - [${k.label}] ${detail.nombre}: stockActual=${detail.stockActual} ${detail.unidadConsumo?.simbolo}, PrecioPromPond=${detail.precioPromedioPonderado}, postedQty=${posted?.qty ?? 'n/a'}, match=${match}`,
    );
  }

  const overview = await api('/api/stock/overview', {}, token);
  console.log('\n=== OVERVIEW ===');
  console.log(JSON.stringify(overview));
  const alerts = await api('/api/stock/alerts', {}, token);
  console.log(`Alerts count: ${Array.isArray(alerts) ? alerts.length : 'n/a'}`);
  const nalga = (Array.isArray(alerts) ? alerts : []).find((a) => (a.nombre || '').toUpperCase().includes('NALGA'));
  console.log(`NALGA alert present: ${!!nalga}${nalga ? ` (${JSON.stringify(nalga)})` : ''}`);
}

main().catch((e) => {
  console.error('FATAL:', e.message);
  process.exit(1);
});
