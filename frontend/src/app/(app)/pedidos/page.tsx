"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useForm, Controller, useFieldArray, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { Plus, Printer, RefreshCw, Trash2, Truck } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError, fetchAllPages } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type {
  Remito,
  RemitoListItem,
  BarListItem,
  Insumo,
  ProductoTerminado,
    CreateRemitoCommand,
    UpdateRemitoCommand,
    CancelarRemitoCommand,
    ConfirmRemitoCommand,
    TipoLineaRemito,
    EstadoRemito,
  } from "@/lib/types";
import { ESTADO_REMITO_LABELS, TIPO_LINEA_REMITO_LABELS } from "@/lib/types";
import { openHtmlInNewTab } from "@/lib/print";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import ConfirmDialog from "@/components/shared/ConfirmDialog";
import SearchCombobox from "@/components/shared/SearchCombobox";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

const lineaSchema = z.object({
  id: z.string(),
  tipoLinea: z.string(),
  productoTerminadoId: z.string(),
  insumoId: z.string(),
  cantidad: z.coerce
    .number({ message: "Ingresá un número válido." })
    .positive("Debe ser mayor a 0."),
  lote: z.string().max(50, "Máximo 50 caracteres."),
});

const remitoSchema = z
  .object({
    barId: z.string().min(1, "Seleccioná un bar."),
    fecha: z.string().min(1, "Seleccioná una fecha."),
    observaciones: z.string().max(500, "Máximo 500 caracteres."),
    entregadoPor: z.string().max(200, "Máximo 200 caracteres."),
    recibidoPor: z.string().max(200, "Máximo 200 caracteres."),
    lineas: z.array(lineaSchema).min(1, "Agregá al menos una línea."),
  })
  .superRefine((values, ctx) => {
    values.lineas.forEach((l, index) => {
      if (Number(l.tipoLinea) === 1 && !l.productoTerminadoId) {
        ctx.addIssue({
          code: "custom",
          path: ["lineas", index, "productoTerminadoId"],
          message: "Seleccioná un producto terminado.",
        });
      }
      if (Number(l.tipoLinea) === 2 && !l.insumoId) {
        ctx.addIssue({
          code: "custom",
          path: ["lineas", index, "insumoId"],
          message: "Seleccioná un insumo.",
        });
      }
    });
  });

type RemitoFormInput = z.input<typeof remitoSchema>;
type RemitoFormValues = z.output<typeof remitoSchema>;

const EMPTY_LINE: RemitoFormInput["lineas"][number] = {
  id: "",
  tipoLinea: "1",
  productoTerminadoId: "",
  insumoId: "",
  cantidad: "1",
  lote: "",
};

function todayISO(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function fechaCorta(iso: string | null | undefined): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? "—" : d.toLocaleDateString("es-AR");
}

const EMPTY_FORM: RemitoFormInput = {
  barId: "",
  fecha: "",
  observaciones: "",
  entregadoPor: "",
  recibidoPor: "",
  lineas: [],
};

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

type EstadoAction = "cancelar";

const CONCURRENCY_MESSAGE =
  "El registro fue modificado por otro usuario. Recargá la lista para ver la versión más reciente y volvé a intentar.";

export default function RemitosPage() {
  const [rows, setRows] = useState<RemitoListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [bares, setBares] = useState<BarListItem[]>([]);
  const [insumos, setInsumos] = useState<Insumo[]>([]);
  const [productos, setProductos] = useState<ProductoTerminado[]>([]);

  // Filters
  const [filtroBar, setFiltroBar] = useState("all");
  const [filtroEstado, setFiltroEstado] = useState("all");
  const [filtroDesde, setFiltroDesde] = useState("");
  const [filtroHasta, setFiltroHasta] = useState("");
  // Cambia en cada guardado para remontar la tabla en página 1.
  const [tableKey, setTableKey] = useState(0);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<{ row: RemitoListItem; rowVersion: string } | null>(null);

  const [detail, setDetail] = useState<Remito | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const [confirmState, setConfirmState] = useState<{
    action: EstadoAction;
    row: RemitoListItem;
  } | null>(null);
  const [actionBusy, setActionBusy] = useState(false);

  const [resumenOpen, setResumenOpen] = useState(false);
  const [resumen, setResumen] = useState<Remito | null>(null);
  const [resumenLoading, setResumenLoading] = useState(false);
  const [resumenBusy, setResumenBusy] = useState(false);

  const buildQuery = useCallback(() => {
    const params = new URLSearchParams();
    if (filtroBar && filtroBar !== "all") params.set("barId", filtroBar);
    if (filtroEstado && filtroEstado !== "all") params.set("estado", filtroEstado);
    if (filtroDesde) params.set("fechaDesde", filtroDesde);
    if (filtroHasta) params.set("fechaHasta", filtroHasta);
    const qs = params.toString();
    return `/remitos${qs ? `?${qs}` : ""}`;
  }, [filtroBar, filtroEstado, filtroDesde, filtroHasta]);

  const load = useCallback(async () => {
    try {
      const result = await apiClient<RemitoListItem[]>(buildQuery());
      setRows(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar los remitos.");
    } finally {
      setLoading(false);
    }
  }, [buildQuery]);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      try {
        const [remitos, barList, prodList] = await Promise.all([
          apiClient<RemitoListItem[]>(buildQuery()),
          apiClient<BarListItem[]>("/bares"),
          apiClient<ProductoTerminado[]>("/productoterminado"),
        ]);
        if (cancelled) return;
        setRows(remitos);
        setBares(barList);
        setProductos(prodList);
        setError(null);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof ApiError ? err.message : "No se pudieron cargar los remitos.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, [buildQuery]);

  useEffect(() => {
    let cancelled = false;
    async function loadInsumos() {
      try {
        const result = await fetchAllPages<Insumo>("/insumos");
        if (!cancelled) setInsumos(result);
      } catch {
        // ignore selector load errors
      }
    }
    loadInsumos();
    return () => {
      cancelled = true;
    };
  }, []);

  const form = useForm<RemitoFormInput, unknown, RemitoFormValues>({
    resolver: zodResolver(remitoSchema),
    defaultValues: EMPTY_FORM,
  });

  const { fields, append, remove } = useFieldArray({ control: form.control, name: "lineas" });

  const openCreate = () => {
    setEditing(null);
    form.reset({ ...EMPTY_FORM, fecha: todayISO(), lineas: [] });
    setDialogOpen(true);
  };

  const openEdit = async (row: RemitoListItem) => {
    let det: Remito;
    try {
      det = await apiClient<Remito>(`/remitos/${row.id}`);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo cargar el remito.");
      return;
    }
    setEditing({ row, rowVersion: det.rowVersion });
    form.reset({
      barId: det.barId,
      fecha: (det.fecha ?? "").slice(0, 10) || todayISO(),
      observaciones: det.observaciones ?? "",
      entregadoPor: det.entregadoPor ?? "",
      recibidoPor: det.recibidoPor ?? "",
      lineas: det.lineas.map((l) => ({
        id: l.id,
        tipoLinea: String(l.tipoLinea),
        productoTerminadoId: l.productoTerminadoId ?? "",
        insumoId: l.insumoId ?? "",
        cantidad: String(l.cantidad),
        lote: l.lote ?? "",
      })),
    });
    setDialogOpen(true);
  };

  const handleSave = form.handleSubmit(async (values) => {
    const lineas = values.lineas.map((l) => ({
      tipoLinea: Number(l.tipoLinea) as TipoLineaRemito,
      productoTerminadoId: Number(l.tipoLinea) === 1 ? l.productoTerminadoId : null,
      insumoId: Number(l.tipoLinea) === 2 ? l.insumoId : null,
      cantidad: l.cantidad,
      lote: l.lote.trim() || null,
    }));
    const base = {
      barId: values.barId,
      fecha: values.fecha || null,
      observaciones: values.observaciones.trim() || null,
      entregadoPor: values.entregadoPor.trim() || null,
      recibidoPor: values.recibidoPor.trim() || null,
      lineas,
    };
    try {
      if (editing) {
        if (!editing.rowVersion) {
          toast.error(
            "No se pudo obtener la versión del registro. Recargá la página e intentá de nuevo.",
          );
          return;
        }
        const payload: UpdateRemitoCommand = {
          ...base,
          id: editing.row.id,
          rowVersion: editing.rowVersion,
        };
        await apiClient<unknown>(`/remitos/${editing.row.id}`, { method: "PUT", body: payload });
        toast.success(`Remito N° ${editing.row.numeroRemito} actualizado.`);
      } else {
        const payload: CreateRemitoCommand = base;
        await apiClient<unknown>("/remitos", { method: "POST", body: payload });
        toast.success("Remito creado.");
      }
      setDialogOpen(false);
      setEditing(null);
      // El recién creado debe verse arriba: se limpian los filtros (el efecto
      // recarga sin recortes) y se resetea la paginación a la página 1.
      setFiltroBar("all");
      setFiltroEstado("all");
      setFiltroDesde("");
      setFiltroHasta("");
      setTableKey((k) => k + 1);
      await load();
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        toast.error(`${err.message} ${CONCURRENCY_MESSAGE}`);
      } else {
        toast.error(err instanceof ApiError ? err.message : "No se pudo guardar el remito.");
      }
    }
  });

  const openDetail = async (row: RemitoListItem) => {
    setDetail(row as unknown as Remito);
    setDetailLoading(true);
    try {
      const det = await apiClient<Remito>(`/remitos/${row.id}`);
      setDetail(det);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo cargar el detalle.");
    } finally {
      setDetailLoading(false);
    }
  };

  const runEstadoAction = async () => {
    if (!confirmState) return;
    setActionBusy(true);
    try {
      const { row } = confirmState;
      const det = await apiClient<Remito>(`/remitos/${row.id}`);
      const rowVersion = det.rowVersion;
      const payload: CancelarRemitoCommand = { remitoId: row.id, rowVersion };
      await apiClient<unknown>(`/remitos/${row.id}/cancelar`, { method: "POST", body: payload });
      toast.success(`Remito N° ${row.numeroRemito} cancelado.`);
      setConfirmState(null);
      await load();
      if (detail && detail.id === row.id) await openDetail(row);
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        toast.error(`${err.message} ${CONCURRENCY_MESSAGE}`);
      } else {
        toast.error(err instanceof ApiError ? err.message : "No se pudo actualizar el remito.");
      }
    } finally {
      setActionBusy(false);
    }
  };

  const openResumen = async (row: RemitoListItem) => {
    setResumenOpen(true);
    setResumen(null);
    setResumenLoading(true);
    try {
      const det = await apiClient<Remito>(`/remitos/${row.id}`);
      setResumen(det);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo cargar el resumen.");
      setResumenOpen(false);
    } finally {
      setResumenLoading(false);
    }
  };

  const closeResumen = () => {
    setResumenOpen(false);
    setResumen(null);
  };

  const confirmResumen = async () => {
    if (!resumen) return;
    setResumenBusy(true);
    try {
      const fresh = await apiClient<Remito>(`/remitos/${resumen.id}`);
      const payload: ConfirmRemitoCommand = { remitoId: fresh.id, rowVersion: fresh.rowVersion };
      await apiClient<unknown>(`/remitos/${fresh.id}/confirmar`, { method: "POST", body: payload });
      toast.success(`Remito N° ${fresh.numeroRemito} confirmado. Se descontó el stock.`);
      closeResumen();
      await load();
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        toast.error(`${err.message} ${CONCURRENCY_MESSAGE}`);
      } else {
        toast.error(err instanceof ApiError ? err.message : "No se pudo confirmar el remito.");
      }
    } finally {
      setResumenBusy(false);
    }
  };

  const imprimir = async (row: RemitoListItem, format: string) => {
    try {
      const res = await apiClient<string>(`/remitos/${row.id}/imprimir?format=${format}`);
      openHtmlInNewTab(res);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo generar la impresión.");
    }
  };

  const ordenCarga = async (row: RemitoListItem, format: string) => {
    try {
      const res = await apiClient<string>(`/remitos/${row.id}/orden-carga?format=${format}`);
      openHtmlInNewTab(res);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo generar la orden de carga.");
    }
  };

  const canMutate = (r: RemitoListItem) => r.estado === 1 || r.estado === 2;

  const columns: ColumnDef<RemitoListItem, unknown>[] = [
    { accessorKey: "numeroRemito", header: "N°" },
    {
      id: "bar",
      header: "Bar",
      cell: ({ row }) => row.original.barNombre || "—",
    },
    {
      id: "fecha",
      header: "Fecha",
      cell: ({ row }) => new Date(row.original.fecha).toLocaleDateString("es-AR"),
    },
    {
      accessorKey: "total",
      header: "Total",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
  ];

  const {
    register,
    control,
    formState: { errors, isSubmitting },
  } = form;

  const watchedLineas = useWatch({ control, name: "lineas" });

  // Productos terminados agrupados por receta: cada grupo es un producto y sus
  // filas son sus lotes (lote + fecha de elaboración + stock).
  const gruposProducto = useMemo(() => {
    const map = new Map<string, { key: string; nombre: string; filas: ProductoTerminado[] }>();
    for (const p of productos) {
      const key = p.recetaId ?? p.id;
      const g = map.get(key);
      if (g) g.filas.push(p);
      else map.set(key, { key, nombre: p.nombre, filas: [p] });
    }
    return [...map.values()].sort((a, b) => a.nombre.localeCompare(b.nombre, "es"));
  }, [productos]);

  const grupoDeFila = useCallback(
    (productoTerminadoId: string | undefined) => {
      if (!productoTerminadoId) return undefined;
      return gruposProducto.find((g) => g.filas.some((f) => f.id === productoTerminadoId));
    },
    [gruposProducto],
  );

  // Display fijo: una sola definición de columnas por sección, compartida entre
  // encabezado y filas. Piso minmax(0,…) para que el contenido nunca ensanche la
  // pista, y columna de acciones fija (botón 36px + aire) con celda vacía.
  const INSUMO_COLS = "minmax(0,1.6fr) minmax(0,0.7fr) minmax(0,0.5fr) 2.5rem";
  const PT_COLS = "minmax(0,1.4fr) minmax(0,1.2fr) minmax(0,0.6fr) minmax(0,0.5fr) 2.5rem";

  const insumoOptions = useMemo(
    () =>
      insumos.map((i) => {
        const simbolo = i.unidadConsumo?.simbolo ?? "";
        const pres =
          i.presentacion != null
            ? `Pres.: ${i.presentacion}${simbolo ? ` ${simbolo}` : ""}`
            : null;
        return {
          id: i.id,
          label: i.nombre,
          sublabel: pres ?? (i.codigoSku ? `SKU: ${i.codigoSku}` : null),
          meta: null as string | null,
          keywords: i.codigoSku ?? null,
        };
      }),
    [insumos],
  );

  const printFormats = [
    { value: "a4", label: "A4" },
    { value: "ticket", label: "Ticket" },
  ];

  // Cuerpo compartido de Ver y Confirmar: cabecera + sectores PT/Insumos + total.
  // Solo difieren el título del diálogo y el footer.
  const renderResumenContenido = (remito: Remito) => (
    <div className="flex flex-col gap-4">
      <div className="grid grid-cols-1 gap-x-4 gap-y-2 text-sm sm:grid-cols-2">
        <div>
          <span className="font-medium">Bar:</span> {remito.barNombre || "—"}
        </div>
        <div>
          <span className="font-medium">Fecha:</span>{" "}
          {new Date(remito.fecha).toLocaleString("es-AR")}
        </div>
        {remito.entregadoPor && (
          <div>
            <span className="font-medium">Entregado por:</span> {remito.entregadoPor}
          </div>
        )}
        {remito.recibidoPor && (
          <div>
            <span className="font-medium">Recibido por:</span> {remito.recibidoPor}
          </div>
        )}
        {remito.observaciones && (
          <div className="sm:col-span-2">
            <span className="font-medium">Observaciones:</span> {remito.observaciones}
          </div>
        )}
      </div>

      {remito.lineas.some((l) => l.tipoLinea === 1) && (
        <div className="flex flex-col gap-2">
          <h3 className="rounded-lg bg-muted px-3 py-1.5 text-sm font-semibold tracking-tight">Productos terminados</h3>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Producto</TableHead>
                <TableHead>Lote</TableHead>
                <TableHead className="text-right">Cantidad</TableHead>
                <TableHead className="text-right">P. unitario</TableHead>
                <TableHead className="text-right">Subtotal</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {remito.lineas
                .filter((l) => l.tipoLinea === 1)
                .map((l) => (
                  <TableRow key={l.id}>
                    <TableCell>{l.productoTerminadoNombre}</TableCell>
                    <TableCell>{l.lote ?? "—"}</TableCell>
                    <TableCell className="text-right">{l.cantidad}</TableCell>
                    <TableCell className="text-right">{MONEY.format(l.precioUnitario)}</TableCell>
                    <TableCell className="text-right">{MONEY.format(l.subtotal)}</TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </div>
      )}
      {remito.lineas.some((l) => l.tipoLinea === 2) && (
        <div className="flex flex-col gap-2">
          <h3 className="rounded-lg bg-muted px-3 py-1.5 text-sm font-semibold tracking-tight">Insumos</h3>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Insumo</TableHead>
                <TableHead className="text-right">Cantidad</TableHead>
                <TableHead className="text-right">P. unitario</TableHead>
                <TableHead className="text-right">Subtotal</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {remito.lineas
                .filter((l) => l.tipoLinea === 2)
                .map((l) => (
                  <TableRow key={l.id}>
                    <TableCell>{l.insumoNombre}</TableCell>
                    <TableCell className="text-right">{l.cantidad}</TableCell>
                    <TableCell className="text-right">{MONEY.format(l.precioUnitario)}</TableCell>
                    <TableCell className="text-right">{MONEY.format(l.subtotal)}</TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </div>
      )}
      <p className="text-right text-sm font-medium">Total: {MONEY.format(remito.total)}</p>
    </div>
  );



  // Fila de línea (sin título: la sección ya indica Insumo / Producto Terminado
  // y el combobox lleva su placeholder). Usa el índice original del array para
  // que validaciones, errores y remove() sigan funcionando al filtrar por sección.
  const renderLinea = (field: (typeof fields)[number], index: number) => {
    const tipoLinea = Number(watchedLineas?.[index]?.tipoLinea);
    const insumoValue = watchedLineas?.[index]?.insumoId ?? "";
    const ptValue = watchedLineas?.[index]?.productoTerminadoId ?? "";
    const grupo = tipoLinea === 1 ? grupoDeFila(ptValue || undefined) : undefined;
    const loteRow = grupo?.filas.find((f) => f.id === ptValue);
    const insumoSel = tipoLinea === 2 ? insumos.find((i) => i.id === insumoValue) : undefined;
    const unidadSimbolo =
      tipoLinea === 1
        ? (loteRow?.unidadMedida?.simbolo ?? "—")
        : (insumoSel?.unidadConsumo?.simbolo ?? "—");
    return (
      <div
        key={field.id}
        className="grid items-center gap-2 border-t border-border px-4 py-2"
        style={{ gridTemplateColumns: tipoLinea === 1 ? PT_COLS : INSUMO_COLS }}
      >
                    {tipoLinea === 1 ? (
                      <div className="flex min-w-0 flex-col gap-1">
                        <SearchCombobox
                          options={gruposProducto.map((g) => ({
                id: g.key,
                label: g.nombre,
                sublabel: null,
                meta: null,
                keywords: null,
              }))}
              value={grupo?.key ?? ""}
              onChange={(groupKey) => {
                const g = gruposProducto.find((x) => x.key === groupKey);
                if (g && g.filas.length === 1 && g.filas[0]) {
                  form.setValue(`lineas.${index}.productoTerminadoId`, g.filas[0].id);
                  form.setValue(`lineas.${index}.lote`, g.filas[0].lote ?? "");
                } else {
                  form.setValue(`lineas.${index}.productoTerminadoId`, "");
                  form.setValue(`lineas.${index}.lote`, "");
                }
              }}
              placeholder="Buscar producto…"
              ariaLabel="Buscar producto terminado"
            />
            <FieldError
              message={errors.lineas?.[index]?.productoTerminadoId?.message}
            />
          </div>
        ) : (
                      <div className="flex min-w-0 flex-col gap-1">
                        <SearchCombobox
                          options={insumoOptions}
              value={insumoValue}
              onChange={(id) => form.setValue(`lineas.${index}.insumoId`, id)}
              placeholder="Buscar insumo…"
              ariaLabel="Buscar insumo"
            />
            <FieldError message={errors.lineas?.[index]?.insumoId?.message} />
          </div>
        )}
                    {tipoLinea === 1 ? (
                      <div className="flex min-w-0 flex-col gap-1">
                        {!grupo ? (
              <Input disabled placeholder="Elegí un producto" />
            ) : ptValue && !loteRow ? (
              <Input placeholder="Opcional" {...register(`lineas.${index}.lote`)} />
            ) : (
              <SearchCombobox
                options={(grupo?.filas ?? []).map((r) => ({
                  id: r.id,
                  label: r.lote || "(sin lote)",
                  sublabel: `Elab. ${fechaCorta(r.fechaProduccion)}`,
                  meta: `Stock ${r.stockActual}`,
                  keywords: r.lote,
                }))}
                value={ptValue}
                onChange={(rowId) => {
                  const row = grupo?.filas.find((f) => f.id === rowId);
                  form.setValue(`lineas.${index}.productoTerminadoId`, rowId);
                  form.setValue(`lineas.${index}.lote`, row?.lote ?? "");
                }}
                onFreeText={(text) => form.setValue(`lineas.${index}.lote`, text)}
                placeholder="Lote…"
                ariaLabel="Buscar lote"
              />
            )}
            <FieldError message={errors.lineas?.[index]?.lote?.message} />
          </div>
        ) : null}
        <div className="flex min-w-0 flex-col gap-1">
          <Input type="number" step="any" min="0" placeholder="0" {...register(`lineas.${index}.cantidad`)} />
          <FieldError message={errors.lineas?.[index]?.cantidad?.message} />
        </div>
        <div className="min-w-0">
          <p className="truncate text-sm text-muted-foreground">{unidadSimbolo}</p>
        </div>
        <div className="flex justify-center">
          <Button
            type="button"
            variant="destructive"
            size="icon"
            onClick={() => remove(index)}
            aria-label="Eliminar línea"
          >
            <Trash2 className="size-4" />
          </Button>
        </div>
      </div>
    );
  };

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nuevo remito" title="Nuevo remito">
              <Plus className="size-5" />
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>          </>
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Select value={filtroBar} onValueChange={setFiltroBar}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Todos los bares" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los bares</SelectItem>
            {bares.map((b) => (
              <SelectItem key={b.id} value={b.id}>
                {b.nombre}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={filtroEstado} onValueChange={setFiltroEstado}>
          <SelectTrigger className="w-[150px]">
            <SelectValue placeholder="Todos los estados" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los estados</SelectItem>
            {(Object.keys(ESTADO_REMITO_LABELS) as unknown as string[]).map((v) => (
              <SelectItem key={v} value={v}>
                {ESTADO_REMITO_LABELS[Number(v) as EstadoRemito]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <div className="flex flex-col gap-1">
          <Label className="text-xs text-muted-foreground">Desde</Label>
          <Input
            type="date"
            className="w-[150px]"
            value={filtroDesde}
            onChange={(e) => setFiltroDesde(e.target.value)}
            aria-label="Fecha desde"
          />
        </div>
        <div className="flex flex-col gap-1">
          <Label className="text-xs text-muted-foreground">Hasta</Label>
          <Input
            type="date"
            className="w-[150px]"
            value={filtroHasta}
            onChange={(e) => setFiltroHasta(e.target.value)}
            aria-label="Fecha hasta"
          />
        </div>
      </div>

      <DataTable
        key={tableKey}
        columns={columns}
        data={rows}
        loading={loading}
        error={error}
        emptyMessage="No hay remitos."
        actions={(row) => (
          <>
            {canMutate(row) && (
              <Button variant="outline" size="sm" onClick={() => void openEdit(row)}>
                Editar
              </Button>
            )}
            {canMutate(row) && (
              <Button size="sm" onClick={() => void openResumen(row)}>
                Confirmar
              </Button>
            )}
            {canMutate(row) && (
              <Button
                variant="destructive"
                size="sm"
                onClick={() => setConfirmState({ action: "cancelar", row })}
              >
                Cancelar
              </Button>
            )}
            {row.estado === 3 && (
              <Button variant="outline" size="sm" onClick={() => void openDetail(row)}>
                Ver
              </Button>
            )}
            {row.estado === 3 && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm">
                    <Printer className="size-4" />
                    Imprimir
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  {printFormats.map((f) => (
                    <DropdownMenuItem key={f.value} onClick={() => void imprimir(row, f.value)}>
                      Imprimir {f.label}
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>
            )}
            {row.estado === 3 && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm">
                    <Truck className="size-4" />
                    Orden carga
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  {printFormats.map((f) => (
                    <DropdownMenuItem key={f.value} onClick={() => void ordenCarga(row, f.value)}>
                      Orden de carga {f.label}
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </>
        )}
      />

      <Dialog
        open={dialogOpen}
        onOpenChange={(open) => {
          setDialogOpen(open);
          if (!open) setEditing(null);
        }}
      >
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>{editing ? `Editar remito N° ${editing.row.numeroRemito}` : "Nuevo remito"}</DialogTitle>
            <DialogDescription>
              {editing
                ? "Modificá los datos del remito y guardá los cambios."
                : "Completá los datos para crear un nuevo remito."}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="remito-bar">Bar</Label>
              <Controller
                control={control}
                name="barId"
                render={({ field }) => (
                  <Select value={field.value || undefined} onValueChange={field.onChange}>
                    <SelectTrigger id="remito-bar" className="w-full">
                      <SelectValue placeholder="Seleccionar…" />
                    </SelectTrigger>
                    <SelectContent>
                      {bares.map((b) => (
                        <SelectItem key={b.id} value={b.id}>
                          {b.nombre}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              <FieldError message={errors.barId?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="remito-fecha">Fecha</Label>
              <Input id="remito-fecha" type="date" {...register("fecha")} />
              <FieldError message={errors.fecha?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="remito-entregadoPor">Entregado por</Label>
              <Input id="remito-entregadoPor" {...register("entregadoPor")} />
              <FieldError message={errors.entregadoPor?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="remito-recibidoPor">Recibido por</Label>
              <Input id="remito-recibidoPor" {...register("recibidoPor")} />
              <FieldError message={errors.recibidoPor?.message} />
            </div>

            <div className="flex flex-col gap-4 sm:col-span-2">
              <section className="flex flex-col gap-2">
                <h3 className="text-sm font-semibold tracking-tight">Insumo</h3>
                <div className="rounded-xl border border-border bg-card shadow-sm">
                  <div
                    className="grid items-center gap-2 rounded-t-xl bg-muted/60 px-4 py-2.5 text-[11px] font-semibold uppercase tracking-[0.04em] text-muted-foreground"
                    style={{ gridTemplateColumns: INSUMO_COLS }}
                  >
                    <span className="pl-8">Insumo</span>
                    <span className="pl-3">Cantidad</span>
                    <span>Unidad</span>
                    <span />
                  </div>
                  {fields.every((_, i) => Number(watchedLineas?.[i]?.tipoLinea) !== 2) && (
                    <p className="border-t border-border px-4 py-6 text-center text-xs text-muted-foreground">
                      Sin insumos — agregá con el botón.
                    </p>
                  )}
                  {fields.map((field, index) =>
                    Number(watchedLineas?.[index]?.tipoLinea) === 2 ? renderLinea(field, index) : null,
                  )}
                </div>
                <div>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => append({ ...EMPTY_LINE, id: `line-${Date.now()}`, tipoLinea: "2" })}
                  >
                    <Plus className="size-4" />
                    Agregar Insumo
                  </Button>
                </div>
              </section>
              <section className="flex flex-col gap-2">
                <h3 className="text-sm font-semibold tracking-tight">Producto Terminado</h3>
                <div className="rounded-xl border border-border bg-card shadow-sm">
                  <div
                    className="grid items-center gap-2 rounded-t-xl bg-muted/60 px-4 py-2.5 text-[11px] font-semibold uppercase tracking-[0.04em] text-muted-foreground"
                    style={{ gridTemplateColumns: PT_COLS }}
                  >
                    <span className="pl-8">Producto terminado</span>
                    <span className="pl-8">Lote</span>
                    <span className="pl-3">Cantidad</span>
                    <span>Unidad</span>
                    <span />
                  </div>
                  {fields.every((_, i) => Number(watchedLineas?.[i]?.tipoLinea) !== 1) && (
                    <p className="border-t border-border px-4 py-6 text-center text-xs text-muted-foreground">
                      Sin productos — agregá con el botón.
                    </p>
                  )}
                  {fields.map((field, index) =>
                    Number(watchedLineas?.[index]?.tipoLinea) === 1 ? renderLinea(field, index) : null,
                  )}
                </div>
                <div>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => append({ ...EMPTY_LINE, id: `line-${Date.now()}`, tipoLinea: "1" })}
                  >
                    <Plus className="size-4" />
                    Agregar Producto Terminado
                  </Button>
                </div>
              </section>
              <FieldError message={errors.lineas?.root?.message ?? errors.lineas?.message} />
            </div>

            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label htmlFor="remito-observaciones">Observaciones</Label>
              <Input id="remito-observaciones" {...register("observaciones")} />
              <FieldError message={errors.observaciones?.message} />
            </div>

            <DialogFooter className="sm:col-span-2">
              <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Guardando…" : "Guardar"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={detail !== null} onOpenChange={(open) => !open && setDetail(null)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>Remito N° {detail?.numeroRemito}</DialogTitle>
            <DialogDescription>Detalle del remito y sus líneas.</DialogDescription>
          </DialogHeader>

          {detailLoading ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Cargando detalle…</p>
          ) : detail ? (
            renderResumenContenido(detail)
          ) : null}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setDetail(null)}>
              Cerrar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={resumenOpen} onOpenChange={(open) => !open && closeResumen()}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>
              {resumen ? `Resumen del pedido N° ${resumen.numeroRemito}` : "Resumen del pedido"}
            </DialogTitle>
            <DialogDescription>
              Revisá el pedido antes de confirmar. Al confirmar se descuentan los insumos y productos terminados del stock.
            </DialogDescription>
          </DialogHeader>

          {resumenLoading ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Cargando resumen…</p>
          ) : resumen ? (
            renderResumenContenido(resumen)
          ) : null}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={closeResumen}>
              Cerrar
            </Button>
            <Button type="button" onClick={() => void confirmResumen()} disabled={!resumen || resumenLoading || resumenBusy}>
              {resumenBusy ? "Confirmando…" : "Confirmar"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmState?.action === "cancelar"}
        onOpenChange={(open) => {
          if (!open) setConfirmState(null);
        }}
        title="Cancelar remito"
        message={`¿Seguro que querés cancelar el remito N° ${confirmState?.row.numeroRemito ?? ""}?`}
        confirmLabel="Cancelar remito"
        destructive
        busy={actionBusy && confirmState?.action === "cancelar"}
        onConfirm={() => void runEstadoAction()}
      />
    </div>
  );
}
