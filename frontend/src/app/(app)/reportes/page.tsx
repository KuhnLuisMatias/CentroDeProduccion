"use client";

import { useCallback, useEffect, useState } from "react";
import type { ColumnDef } from "@tanstack/react-table";
import { FileDown, FileSpreadsheet, Loader2, Search } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import { downloadReportExport } from "@/lib/export";
import type { ReportEnvelope, ReportMetadata } from "@/lib/types";
import { useAuth } from "@/context/AuthContext";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";

type ReportGroup = "produccion" | "stock" | "compras" | "ventas" | "costos" | "rentabilidad";

type ColumnKind = "raw" | "currency" | "number" | "date" | "dateOnly" | "percent";

interface ColumnDefReport {
  key: string;
  header: string;
  kind?: ColumnKind;
}

interface FiltersDef {
  from?: boolean;
  to?: boolean;
  agrupacion?: boolean;
  proveedor?: boolean;
  bar?: boolean;
  empleado?: boolean;
  insumo?: boolean;
  receta?: boolean;
  producto?: boolean;
}

interface ReportDef {
  id: string;
  label: string;
  title: string;
  endpoint: string;
  columns: ColumnDefReport[];
  filters: FiltersDef;
  requiresProveedor?: boolean;
  requiresBar?: boolean;
  requiresReceta?: boolean;
  total: { label: string; field: string; kind?: ColumnKind; envelope?: "totalValorizado" | "saldoFinal" | "totalGeneral" | "totales" } | null;
}

interface GroupDef {
  key: ReportGroup;
  label: string;
  adminOnly?: boolean;
  reports: ReportDef[];
}

const NUM = new Intl.NumberFormat("es-AR");

const GROUPS: GroupDef[] = [
  {
    key: "produccion",
    label: "Producción",
    reports: [
      {
        id: "produccion-periodo",
        label: "Por período",
        title: "Producción por período",
        endpoint: "/reports/produccion/periodo",
        filters: { from: true, to: true, agrupacion: true },
        columns: [
          { key: "periodoLabel", header: "Período" },
          { key: "cantidadProducciones", header: "Cant. producciones" },
          { key: "cantidadProducida", header: "Cantidad producida" },
          { key: "costoTotal", header: "Costo total", kind: "currency" },
        ],
        total: { label: "Costo total", field: "costoTotal", kind: "currency" },
      },
      {
        id: "produccion-producto",
        label: "Por producto",
        title: "Producción por producto",
        endpoint: "/reports/produccion/producto",
        filters: { from: true, to: true, receta: true },
        columns: [
          { key: "recetaNombre", header: "Receta" },
          { key: "cantidadProducciones", header: "Cant. producciones" },
          { key: "cantidadProducida", header: "Cantidad producida" },
          { key: "costoPromedio", header: "Costo promedio", kind: "currency" },
        ],
        total: { label: "Cantidad producida", field: "cantidadProducida" },
      },
    ],
  },
  {
    key: "stock",
    label: "Stock",
    reports: [
      {
        id: "stock-insumos-valorado",
        label: "Insumos valorizado",
        title: "Stock de insumos valorizado",
        endpoint: "/reports/stock/insumos/valorado",
        filters: {},
        columns: [
          { key: "nombre", header: "Insumo" },
          { key: "unidadMedida", header: "Unidad" },
          { key: "stockActual", header: "Stock actual" },
          { key: "precioUltimaCompra", header: "Precio última compra", kind: "currency" },
          { key: "valorTotal", header: "Valor total", kind: "currency" },
        ],
        total: { label: "Total valorizado", field: "valorTotal", kind: "currency", envelope: "totalValorizado" },
      },
      {
        id: "stock-insumos-bajo-minimo",
        label: "Insumos bajo mínimo",
        title: "Stock de insumos bajo mínimo",
        endpoint: "/reports/stock/insumos/bajo-minimo",
        filters: {},
        columns: [
          { key: "nombre", header: "Insumo" },
          { key: "stockActual", header: "Stock actual" },
          { key: "stockMinimo", header: "Stock mínimo" },
          { key: "diferenciaStock", header: "Diferencia stock" },
        ],
        total: { label: "Insumos", field: "stockActual" },
      },
      {
        id: "stock-pt-proximos-vencer",
        label: "PT próximos a vencer",
        title: "Productos terminados próximos a vencer",
        endpoint: "/reports/stock/pt/proximos-vencer",
        filters: {},
        columns: [
          { key: "nombre", header: "Producto" },
          { key: "stockActual", header: "Stock actual" },
          { key: "fechaVencimiento", header: "Fecha vencimiento", kind: "dateOnly" },
          { key: "diasParaVencer", header: "Días para vencer" },
        ],
        total: { label: "Productos", field: "stockActual" },
      },
      {
        id: "stock-pt-valorado",
        label: "PT valorizado",
        title: "Stock de productos terminados valorizado",
        endpoint: "/reports/stock/pt/valorado",
        filters: {},
        columns: [
          { key: "nombre", header: "Producto" },
          { key: "stockActual", header: "Stock actual" },
          { key: "costoUnitario", header: "Costo unitario", kind: "currency" },
          { key: "valorTotal", header: "Valor total", kind: "currency" },
        ],
        total: { label: "Total valorizado", field: "valorTotal", kind: "currency", envelope: "totalValorizado" },
      },
    ],
  },
  {
    key: "compras",
    label: "Compras",
    reports: [
      {
        id: "compras-proveedor",
        label: "Por proveedor",
        title: "Compras por proveedor",
        endpoint: "/reports/compras/proveedor",
        filters: { from: true, to: true, proveedor: true },
        columns: [
          { key: "proveedorNombre", header: "Proveedor" },
          { key: "ordenesCount", header: "Órdenes" },
          { key: "totalMonto", header: "Total", kind: "currency" },
          { key: "pendientes", header: "Pendientes" },
          { key: "canceladas", header: "Canceladas" },
        ],
        total: { label: "Total", field: "totalMonto", kind: "currency" },
      },
      {
        id: "compras-precios",
        label: "Evolución de precios",
        title: "Evolución de precios",
        endpoint: "/reports/compras/precios",
        filters: { from: true, to: true, insumo: true },
        columns: [
          { key: "insumoNombre", header: "Insumo" },
          { key: "fecha", header: "Fecha", kind: "dateOnly" },
          { key: "precioUnitario", header: "Precio unitario", kind: "currency" },
          { key: "proveedorNombre", header: "Proveedor" },
        ],
        total: { label: "Movimientos", field: "precioUnitario" },
      },
      {
        id: "compras-proveedores-resumen",
        label: "Resumen proveedores",
        title: "Resumen de proveedores",
        endpoint: "/reports/compras/proveedores/resumen",
        filters: { from: true, to: true },
        columns: [
          { key: "proveedorNombre", header: "Proveedor" },
          { key: "ordenesCount", header: "Órdenes" },
          { key: "totalMonto", header: "Total", kind: "currency" },
          { key: "saldoActual", header: "Saldo actual", kind: "currency" },
        ],
        total: { label: "Total", field: "totalMonto", kind: "currency" },
      },
      {
        id: "compras-cta-cte-proveedor",
        label: "Cta. cte. proveedor",
        title: "Cuenta corriente del proveedor",
        endpoint: "/reports/compras/cta-cte/proveedor",
        filters: { from: true, to: true, proveedor: true },
        requiresProveedor: true,
        columns: [
          { key: "fecha", header: "Fecha", kind: "dateOnly" },
          { key: "tipo", header: "Tipo" },
          { key: "referencia", header: "Referencia" },
          { key: "monto", header: "Monto", kind: "currency" },
          { key: "saldo", header: "Saldo", kind: "currency" },
        ],
        total: { label: "Saldo final", field: "saldo", kind: "currency", envelope: "saldoFinal" },
      },
    ],
  },
  {
    key: "ventas",
    label: "Ventas",
    reports: [
      {
        id: "ventas-bar",
        label: "Por bar",
        title: "Ventas por bar",
        endpoint: "/reports/ventas/bar",
        filters: { from: true, to: true, bar: true },
        columns: [
          { key: "barNombre", header: "Bar" },
          { key: "remitosCount", header: "Remitos" },
          { key: "lineasCount", header: "Líneas" },
          { key: "totalSubtotal", header: "Total", kind: "currency" },
        ],
        total: { label: "Total", field: "totalSubtotal", kind: "currency" },
      },
      {
        id: "ventas-periodo",
        label: "Por período",
        title: "Ventas por período",
        endpoint: "/reports/ventas/periodo",
        filters: { from: true, to: true, agrupacion: true },
        columns: [
          { key: "periodoLabel", header: "Período" },
          { key: "remitosCount", header: "Remitos" },
          { key: "cantidadTotal", header: "Cantidad total" },
          { key: "totalSubtotal", header: "Total", kind: "currency" },
        ],
        total: { label: "Total", field: "totalSubtotal", kind: "currency" },
      },
      {
        id: "ventas-devoluciones",
        label: "Devoluciones",
        title: "Devoluciones",
        endpoint: "/reports/ventas/devoluciones",
        filters: { from: true, to: true, bar: true },
        columns: [
          { key: "fecha", header: "Fecha", kind: "dateOnly" },
          { key: "barNombre", header: "Bar" },
          { key: "numeroRemito", header: "Nº remito" },
          { key: "cantidadDevuelta", header: "Cantidad devuelta" },
          { key: "totalDevolucion", header: "Total", kind: "currency" },
        ],
        total: { label: "Total devuelto", field: "totalDevolucion", kind: "currency" },
      },
      {
        id: "ventas-cta-cte-bar",
        label: "Cta. cte. bar",
        title: "Cuenta corriente del bar",
        endpoint: "/reports/ventas/cta-cte/bar",
        filters: { from: true, to: true, bar: true },
        requiresBar: true,
        columns: [
          { key: "fecha", header: "Fecha", kind: "dateOnly" },
          { key: "tipo", header: "Tipo" },
          { key: "referencia", header: "Referencia" },
          { key: "monto", header: "Monto", kind: "currency" },
          { key: "saldo", header: "Saldo", kind: "currency" },
        ],
        total: { label: "Saldo final", field: "saldo", kind: "currency", envelope: "saldoFinal" },
      },
      {
        id: "pedidos-detalle",
        label: "Pedidos — detalle",
        title: "Pedidos por período (detalle)",
        endpoint: "/reports/pedidos/detalle",
        filters: { from: true, to: true, bar: true },
        columns: [
          { key: "fecha", header: "Fecha", kind: "dateOnly" },
          { key: "numeroRemito", header: "Nº pedido" },
          { key: "producto", header: "Producto" },
          { key: "tipoLinea", header: "Tipo" },
          { key: "cantidad", header: "Cantidad", kind: "number" },
          { key: "unidad", header: "Unidad" },
          { key: "precioUnitario", header: "Precio", kind: "currency" },
          { key: "subtotal", header: "Subtotal", kind: "currency" },
          { key: "proveedor", header: "Proveedor" },
          { key: "observaciones", header: "Observaciones" },
        ],
        total: { label: "Total general", field: "subtotal", kind: "currency", envelope: "totalGeneral" },
      },
      {
        id: "pedidos-matriz",
        label: "Matriz semanal",
        title: "Pedidos — matriz semanal",
        endpoint: "/reports/pedidos/matriz",
        filters: { from: true, to: true, bar: true },
        columns: [
          { key: "articulo", header: "Artículo" },
          { key: "lunes", header: "Lunes", kind: "number" },
          { key: "martes", header: "Martes", kind: "number" },
          { key: "miercoles", header: "Miércoles", kind: "number" },
          { key: "jueves", header: "Jueves", kind: "number" },
          { key: "viernes", header: "Viernes", kind: "number" },
          { key: "sabado", header: "Sábado", kind: "number" },
          { key: "domingo", header: "Domingo", kind: "number" },
          { key: "total", header: "Total", kind: "number" },
        ],
        total: { label: "Total general", field: "totalGeneral", kind: "currency", envelope: "totales" },
      },
    ],
  },
  {
    key: "costos",
    label: "Costos",
    adminOnly: true,
    reports: [
      {
        id: "costos-producto",
        label: "Costo por producto",
        title: "Costo por producto",
        endpoint: "/reports/costos/producto",
        filters: { from: true, to: true, producto: true },
        columns: [
          { key: "recetaNombre", header: "Receta" },
          { key: "costoInsumos", header: "Costo insumos", kind: "currency" },
          { key: "costoTotal", header: "Costo total", kind: "currency" },
          { key: "numeroProducciones", header: "Nº producciones" },
          { key: "observacion", header: "Observación" },
        ],
        total: { label: "Costo total", field: "costoTotal", kind: "currency" },
      },
      {
        id: "planilla-costos",
        label: "Planilla de costos",
        title: "Planilla de costos por receta",
        endpoint: "/reports/planilla-costos",
        filters: { receta: true },
        requiresReceta: true,
        columns: [
          { key: "referencia", header: "Referencia" },
          { key: "tipoLinea", header: "Tipo" },
          { key: "cantidadNecesaria", header: "Cantidad", kind: "number" },
          { key: "unidadMedida", header: "Unidad" },
          { key: "precioUnitario", header: "Precio unitario", kind: "currency" },
          { key: "subtotal", header: "Subtotal", kind: "currency" },
        ],
        total: null,
      },
    ],
  },
  {
    key: "rentabilidad",
    label: "Rentabilidad",
    adminOnly: true,
    reports: [
      {
        id: "rentabilidad-producto",
        label: "Por producto",
        title: "Rentabilidad por producto",
        endpoint: "/reports/rentabilidad/producto",
        filters: { from: true, to: true, producto: true },
        columns: [
          { key: "productoTerminadoNombre", header: "Producto" },
          { key: "ingresos", header: "Ingresos", kind: "currency" },
          { key: "costos", header: "Costos", kind: "currency" },
          { key: "rentabilidad", header: "Rentabilidad", kind: "currency" },
          { key: "margenPorcentaje", header: "Margen %", kind: "percent" },
          { key: "observacion", header: "Observación" },
        ],
        total: { label: "Rentabilidad", field: "rentabilidad", kind: "currency" },
      },
      {
        id: "rentabilidad-bar",
        label: "Por bar",
        title: "Rentabilidad por bar",
        endpoint: "/reports/rentabilidad/bar",
        filters: { from: true, to: true, bar: true },
        columns: [
          { key: "barNombre", header: "Bar" },
          { key: "ingresos", header: "Ingresos", kind: "currency" },
          { key: "costos", header: "Costos", kind: "currency" },
          { key: "rentabilidad", header: "Rentabilidad", kind: "currency" },
          { key: "margenPorcentaje", header: "Margen %", kind: "percent" },
        ],
        total: { label: "Rentabilidad", field: "rentabilidad", kind: "currency" },
      },
    ],
  },
];

const AGRUPACION_OPTIONS = [
  { value: "dia", label: "Día" },
  { value: "semana", label: "Semana" },
  { value: "mes", label: "Mes" },
];

interface CatalogOption {
  value: string;
  label: string;
}

interface ReportFilters {
  from: string;
  to: string;
  agrupacion: string;
  proveedorId: string;
  barId: string;
  empleadoId: string;
  insumoId: string;
  recetaId: string;
  productoId: string;
}

const EMPTY_FILTERS: ReportFilters = {
  from: "",
  to: "",
  agrupacion: "dia",
  proveedorId: "",
  barId: "",
  empleadoId: "",
  insumoId: "",
  recetaId: "",
  productoId: "",
};

// The backend binds date-only values as midnight and compares fecha <= to,
// which excludes everything created during that day. Always send the upper
// bound as end-of-day so "today" is fully included.
const END_OF_DAY = "T23:59:59";

function toLocalDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function defaultFilters(): ReportFilters {
  const to = new Date();
  const from = new Date(to);
  from.setDate(from.getDate() - 30);
  return { ...EMPTY_FILTERS, from: toLocalDateOnly(from), to: toLocalDateOnly(to) };
}

function startOfWeek(date: Date): Date {
  const d = new Date(date);
  const day = d.getDay();
  d.setDate(d.getDate() + (day === 0 ? -6 : 1 - day));
  d.setHours(0, 0, 0, 0);
  return d;
}

function defaultFiltersFor(reportId: string): ReportFilters {
  if (reportId === "pedidos-detalle") {
    const to = new Date();
    const from = new Date(to.getFullYear(), to.getMonth(), 1);
    return { ...EMPTY_FILTERS, from: toLocalDateOnly(from), to: toLocalDateOnly(to) };
  }
  if (reportId === "pedidos-matriz") {
    const from = startOfWeek(new Date());
    const to = new Date(from);
    to.setDate(to.getDate() + 6);
    return { ...EMPTY_FILTERS, from: toLocalDateOnly(from), to: toLocalDateOnly(to) };
  }
  return defaultFilters();
}

type ReportRow = Record<string, unknown>;

function formatCell(value: unknown, kind: ColumnKind | undefined): string {
  if (value === null || value === undefined || value === "") return "—";
  switch (kind) {
    case "currency":
      return MONEY.format(Number(value));
    case "number":
      return NUM.format(Number(value));
    case "date":
      return new Date(String(value)).toLocaleString("es-AR");
    case "dateOnly":
      return new Date(String(value)).toLocaleDateString("es-AR");
    case "percent":
      return `${(Number(value) * 100).toFixed(1)}%`;
    default:
      return String(value);
  }
}

export default function ReportesPage() {
  const { user } = useAuth();
  const [group, setGroup] = useState<ReportGroup>("produccion");
  const [report, setReport] = useState<string>(GROUPS[0].reports[0].id);
  const [filters, setFilters] = useState<ReportFilters>(defaultFilters);
  const [runKey, setRunKey] = useState(0);

  const [data, setData] = useState<ReportEnvelope | null>(null);
  const [rows, setRows] = useState<ReportRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState<"excel" | "pdf" | null>(null);

  const [proveedores, setProveedores] = useState<CatalogOption[]>([]);
  const [bares, setBares] = useState<CatalogOption[]>([]);
  const [empleados, setEmpleados] = useState<CatalogOption[]>([]);
  const [insumos, setInsumos] = useState<CatalogOption[]>([]);
  const [recetas, setRecetas] = useState<CatalogOption[]>([]);
  const [productos, setProductos] = useState<CatalogOption[]>([]);

  const activeGroup = GROUPS.find((g) => g.key === group)!;
  const activeReport =
    activeGroup.reports.find((r) => r.id === report) ?? activeGroup.reports[0];
  const isAdmin = user?.rol === "Administrador";

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [prov, bar, emp, ins, rec, prod] = await Promise.all([
          apiClient<{ id: string; nombreRazonSocial: string }[]>("/proveedores"),
          apiClient<{ id: string; nombre: string }[]>("/bares"),
          apiClient<{ id: string; nombre: string; apellido: string }[]>("/empleados?activo=true"),
          apiClient<{ id: string; nombre: string }[]>("/insumos?activo=true"),
          apiClient<{ id: string; nombre: string }[]>("/recetas"),
          apiClient<{ id: string; nombre: string }[]>("/productoterminado"),
        ]);
        if (cancelled) return;
        setProveedores(prov.map((p) => ({ value: p.id, label: p.nombreRazonSocial })));
        setBares(bar.map((b) => ({ value: b.id, label: b.nombre })));
        setEmpleados(emp.map((e) => ({ value: e.id, label: `${e.nombre} ${e.apellido}`.trim() })));
        setInsumos(ins.map((i) => ({ value: i.id, label: i.nombre })));
        setRecetas(rec.map((r) => ({ value: r.id, label: r.nombre })));
        setProductos(prod.map((p) => ({ value: p.id, label: p.nombre })));
      } catch {
        // catalogs are optional; report can still be filtered by date
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const buildQuery = useCallback(
    (def: ReportDef, f: ReportFilters): string => {
      const params = new URLSearchParams();
      if (f.from) params.append("from", f.from);
      if (f.to) params.append("to", `${f.to}${END_OF_DAY}`);
      if (f.agrupacion && def.filters.agrupacion) params.append("agrupacion", f.agrupacion);
      if (def.filters.proveedor && f.proveedorId) params.append("proveedorId", f.proveedorId);
      if (def.filters.bar && f.barId) params.append("barId", f.barId);
      if (def.filters.empleado && f.empleadoId) params.append("empleadoId", f.empleadoId);
      if (def.filters.insumo && f.insumoId) params.append("insumoId", f.insumoId);
      if (def.filters.receta && f.recetaId) params.append("recetaId", f.recetaId);
      if (def.filters.producto && f.productoId) params.append("productoId", f.productoId);
      const qs = params.toString();
      return qs ? `?${qs}` : "";
    },
    [],
  );

  useEffect(() => {
    let cancelled = false;
    async function load() {
      if (
        (activeReport.requiresProveedor && !filters.proveedorId) ||
        (activeReport.requiresBar && !filters.barId) ||
        (activeReport.requiresReceta && !filters.recetaId)
      ) {
        setData(null);
        setRows([]);
        return;
      }
      setLoading(true);
      setError(null);
      try {
        const res = await apiClient<ReportEnvelope>(`${activeReport.endpoint}${buildQuery(activeReport, filters)}`);
        if (cancelled) return;
        setData(res);
        setRows((res.items ?? []) as ReportRow[]);
      } catch (err) {
        if (cancelled) return;
        setData(null);
        setRows([]);
        setError(
          err instanceof ApiError && err.status === 403
            ? "Acceso restringido. No tiene permisos para ver este reporte."
            : err instanceof ApiError
              ? err.message
              : "No se pudo cargar el reporte.",
        );
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    load();
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeReport.id, group, runKey]);

  const selectGroup = (g: ReportGroup) => {
    const def = GROUPS.find((x) => x.key === g)!;
    setGroup(g);
    setReport(def.reports[0].id);
    setFilters(defaultFiltersFor(def.reports[0].id));
    setData(null);
    setRows([]);
    setError(null);
  };

  const selectReport = (id: string) => {
    setReport(id);
    setFilters(defaultFiltersFor(id));
    setData(null);
    setRows([]);
    setError(null);
  };

  const missingRequirements: string[] = [];
  if (activeReport.requiresProveedor && !filters.proveedorId) missingRequirements.push("un proveedor");
  if (activeReport.requiresBar && !filters.barId) missingRequirements.push("un bar");
  if (activeReport.requiresReceta && !filters.recetaId) missingRequirements.push("una receta");
  const ready = missingRequirements.length === 0;

  const totalValue = (): number | null => {
    if (!activeReport.total || !data) return null;
    const t = activeReport.total;
    if (t.envelope === "totalGeneral") return typeof data.totalGeneral === "number" ? data.totalGeneral : null;
    if (t.envelope === "totales") {
      const v = data.totales?.[t.field];
      return typeof v === "number" ? v : null;
    }
    if (t.envelope && data[t.envelope] !== undefined) return data[t.envelope] as number;
    if (t.envelope) return null;
    let sum = 0;
    for (const row of rows) {
      const v = row[t.field];
      if (typeof v === "number") sum += v;
    }
    return sum;
  };

  const handleExport = async (format: "excel" | "pdf") => {
    if (!ready) return;
    setExporting(format);
    setError(null);
    const exportParams: Record<string, string> = {};
    if (filters.from) exportParams.from = filters.from;
    if (filters.to) exportParams.to = `${filters.to}${END_OF_DAY}`;
    if (filters.agrupacion && activeReport.filters.agrupacion) exportParams.agrupacion = filters.agrupacion;
    if (activeReport.filters.proveedor && filters.proveedorId) exportParams.proveedorId = filters.proveedorId;
    if (activeReport.filters.bar && filters.barId) exportParams.barId = filters.barId;
    if (activeReport.filters.empleado && filters.empleadoId) exportParams.empleadoId = filters.empleadoId;
    if (activeReport.filters.insumo && filters.insumoId) exportParams.insumoId = filters.insumoId;
    if (activeReport.filters.receta && filters.recetaId) exportParams.recetaId = filters.recetaId;
    if (activeReport.filters.producto && filters.productoId) exportParams.productoId = filters.productoId;
    try {
      await downloadReportExport(activeReport.id, format, exportParams);
      toast.success("Exportación descargada.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo exportar el reporte.");
    } finally {
      setExporting(null);
    }
  };

  const metadata: ReportMetadata | undefined = data?.metadata;

  const columns: ColumnDef<ReportRow, unknown>[] = activeReport.columns.map((c) => ({
    id: c.key,
    header: c.header,
    cell: ({ row }) => formatCell(row.original[c.key], c.kind),
  }));

  const total = activeReport.total ? totalValue() : null;

  return (
    <div>
      <PageHeader
        title="Reportes"
        description="Consultas y reportes del sistema con exportación a Excel y PDF."
      />

      <Tabs value={group} onValueChange={(value) => selectGroup(value as ReportGroup)} className="mb-4">
        <TabsList className="flex-wrap">
          {GROUPS.filter((g) => !g.adminOnly || isAdmin).map((g) => (
            <TabsTrigger key={g.key} value={g.key}>
              {g.label}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      <Tabs value={report} onValueChange={selectReport} className="mb-4">
        <TabsList variant="line" className="flex-wrap">
          {activeGroup.reports.map((r) => (
            <TabsTrigger key={r.id} value={r.id}>
              {r.label}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      <div className="mb-4 flex flex-wrap items-center gap-2">
        {activeReport.filters.from && (
          <Input
            type="date"
            className="w-[150px]"
            aria-label="Fecha desde"
            value={filters.from}
            onChange={(e) => setFilters((f) => ({ ...f, from: e.target.value }))}
          />
        )}
        {activeReport.filters.to && (
          <Input
            type="date"
            className="w-[150px]"
            aria-label="Fecha hasta"
            value={filters.to}
            onChange={(e) => setFilters((f) => ({ ...f, to: e.target.value }))}
          />
        )}
        {activeReport.filters.agrupacion && (
          <Select
            value={filters.agrupacion}
            onValueChange={(value) => setFilters((f) => ({ ...f, agrupacion: value }))}
          >
            <SelectTrigger className="w-[130px]" aria-label="Agrupación">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {AGRUPACION_OPTIONS.map((opt) => (
                <SelectItem key={opt.value} value={opt.value}>
                  {opt.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
        {activeReport.filters.proveedor && (
          <CatalogFilter
            options={proveedores}
            emptyLabel="Todos los proveedores"
            value={filters.proveedorId}
            onChange={(value) => setFilters((f) => ({ ...f, proveedorId: value }))}
          />
        )}
        {activeReport.filters.bar && (
          <CatalogFilter
            options={bares}
            emptyLabel="Todos los bares"
            value={filters.barId}
            onChange={(value) => setFilters((f) => ({ ...f, barId: value }))}
          />
        )}
        {activeReport.filters.empleado && (
          <CatalogFilter
            options={empleados}
            emptyLabel="Todos los empleados"
            value={filters.empleadoId}
            onChange={(value) => setFilters((f) => ({ ...f, empleadoId: value }))}
          />
        )}
        {activeReport.filters.insumo && (
          <CatalogFilter
            options={insumos}
            emptyLabel="Todos los insumos"
            value={filters.insumoId}
            onChange={(value) => setFilters((f) => ({ ...f, insumoId: value }))}
          />
        )}
        {activeReport.filters.receta && (
          <CatalogFilter
            options={recetas}
            emptyLabel="Todas las recetas"
            value={filters.recetaId}
            onChange={(value) => setFilters((f) => ({ ...f, recetaId: value }))}
          />
        )}
        {activeReport.filters.producto && (
          <CatalogFilter
            options={productos}
            emptyLabel="Todos los productos"
            value={filters.productoId}
            onChange={(value) => setFilters((f) => ({ ...f, productoId: value }))}
          />
        )}
        <Button size="sm" onClick={() => setRunKey((k) => k + 1)} disabled={!ready || loading}>
          {loading ? (
            <Loader2 className="size-4 animate-spin" aria-hidden="true" />
          ) : (
            <Search className="size-4" aria-hidden="true" />
          )}
          Consultar
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => void handleExport("excel")}
          disabled={!ready || exporting !== null}
        >
          {exporting === "excel" ? (
            <Loader2 className="size-4 animate-spin" aria-hidden="true" />
          ) : (
            <FileSpreadsheet className="size-4" aria-hidden="true" />
          )}
          {exporting === "excel" ? "Exportando…" : "Excel"}
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => void handleExport("pdf")}
          disabled={!ready || exporting !== null}
        >
          {exporting === "pdf" ? (
            <Loader2 className="size-4 animate-spin" aria-hidden="true" />
          ) : (
            <FileDown className="size-4" aria-hidden="true" />
          )}
          {exporting === "pdf" ? "Exportando…" : "PDF"}
        </Button>
      </div>

      {!ready && (
        <p className="mb-4 rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Debe seleccionar {missingRequirements.join(" y ")} para este reporte.
        </p>
      )}

      {metadata && (
        <p className="mb-3 text-sm text-muted-foreground">
          <span className="font-medium text-foreground">{metadata.reportTitle ?? activeReport.title}</span>
          {metadata.filterDescription ? ` — ${metadata.filterDescription}` : ""}
          {metadata.dateRangeFrom || metadata.dateRangeTo
            ? ` (${metadata.dateRangeFrom ? new Date(metadata.dateRangeFrom).toLocaleDateString("es-AR") : "…"} → ${metadata.dateRangeTo ? new Date(metadata.dateRangeTo).toLocaleDateString("es-AR") : "…"})`
            : ""}
        </p>
      )}

      {activeReport.id === "planilla-costos" && data?.receta && (
        <p className="mb-3 text-sm text-muted-foreground">
          <span className="font-medium text-foreground">{data.receta.nombre}</span>
          {" — "}{data.receta.categoria}
        </p>
      )}

      {data?.costos && (
        <div className="mb-4 flex flex-wrap gap-2">
          {([
            ["Costo unitario", data.costos.costoUnitario],
          ] as const).map(([label, value]) => (
            <Card key={label} className="w-fit min-w-56">
              <CardHeader className="pb-1">
                <CardTitle className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  {label}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-xl font-semibold tabular-nums">{MONEY.format(value)}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {total !== null && (
        <Card className="mb-4 w-fit min-w-56">
          <CardHeader className="pb-1">
            <CardTitle className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              {activeReport.total?.label}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xl font-semibold tabular-nums">
              {formatCell(total, activeReport.total?.kind)}
            </p>
          </CardContent>
        </Card>
      )}

      <DataTable
        columns={columns}
        data={rows}
        loading={loading}
        error={error}
        emptyMessage="No hay datos para este reporte."
      />
    </div>
  );
}

interface CatalogFilterProps {
  options: CatalogOption[];
  emptyLabel: string;
  value: string;
  onChange: (value: string) => void;
}

function CatalogFilter({ options, emptyLabel, value, onChange }: CatalogFilterProps) {
  return (
    <Select value={value || "all"} onValueChange={(v) => onChange(v === "all" ? "" : v)}>
      <SelectTrigger className="w-[190px]" aria-label={emptyLabel}>
        <SelectValue placeholder={emptyLabel} />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="all">{emptyLabel}</SelectItem>
        {options.map((opt) => (
          <SelectItem key={opt.value} value={opt.value}>
            {opt.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
