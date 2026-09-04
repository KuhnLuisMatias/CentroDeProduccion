"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { Check, Info, PackageCheck, Plus, RefreshCw, Search, X } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError, fetchAllPages } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type {
  Insumo,
  Produccion,
  ProduccionInsumoConsumido,
  Receta,
  CreateProduccionCommand,
  UpdateProduccionInsumosCommand,
  ConfirmProduccionCommand,
  CreateProduccionResponse,
  ConfirmProduccionResponse,
} from "@/lib/types";
import { ESTADO_PRODUCCION_LABELS } from "@/lib/types";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import ConfirmDialog from "@/components/shared/ConfirmDialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

const createSchema = z.object({
  recetaId: z.string().min(1, "Seleccioná una receta."),
});

type CreateFormInput = z.input<typeof createSchema>;
type CreateFormValues = z.output<typeof createSchema>;

// Editable consumption line of the production modal (ETAPA B). Cantidad is the
// ONLY editable field; cost is derived client-side.
interface EditorLine {
  key: string;
  insumoId: string;
  nombre: string;
  cantidad: string;
}

// Module-level cache: the insumos list is static within a session and shared
// across modal openings without touching component refs. Provides the unit
// price (precioUltimaCompra) and consumption-unit symbol that the
// production detail payload does NOT include.
let insumosCache: Insumo[] | null = null;

async function loadInsumos(): Promise<Insumo[]> {
  if (!insumosCache) {
    insumosCache = await fetchAllPages<Insumo>("/insumos?pageSize=100");
  }
  return insumosCache;
}

function getInsumoInfo(insumoId: string): Insumo | undefined {
  return insumosCache?.find((i) => i.id === insumoId);
}

function parseCantidad(cantidad: string): number {
  const parsed = Number(cantidad);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 0;
}

function lineCosto(line: ProduccionInsumoConsumido | { insumoId: string; cantidad: number }): number {
  const price = getInsumoInfo(line.insumoId)?.precioUltimaCompra ?? 0;
  return line.cantidad * price;
}

function totalCosto(lines: ProduccionInsumoConsumido[]): number {
  return lines.reduce((acc, l) => acc + lineCosto(l), 0);
}

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

function EstadoBadge({ estado }: { estado: Produccion["estado"] }) {
  const label = ESTADO_PRODUCCION_LABELS[estado] ?? String(estado);
  if (estado === 2) {
    return (
      <Badge variant="outline" className="border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400">
        {label}
      </Badge>
    );
  }
  if (estado === 3) {
    return (
      <Badge variant="outline" className="border-red-600/30 bg-red-500/10 text-red-700 dark:text-red-400">
        {label}
      </Badge>
    );
  }
  if (estado === 1) {
    return (
      <Badge variant="outline" className="border-yellow-600/30 bg-yellow-500/10 text-yellow-700 dark:text-yellow-400">
        {label}
      </Badge>
    );
  }
  return <Badge variant="outline">{label}</Badge>;
}

export default function ProduccionPage() {
  const [rows, setRows] = useState<Produccion[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [recetas, setRecetas] = useState<Receta[]>([]);

  // ETAPA A — create modal (compact)
  const [dialogOpen, setDialogOpen] = useState(false);
  const [recetaSearch, setRecetaSearch] = useState("");

  // ETAPA B — production modal (editable quantities, Borrador only)
  const [editorOpen, setEditorOpen] = useState(false);
  const [detail, setDetail] = useState<Produccion | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [lines, setLines] = useState<EditorLine[]>([]);
  const [savingLines, setSavingLines] = useState(false);

  // Terminar modal — minimal: only the "Cantidad producida" input.
  const [closingOpen, setClosingOpen] = useState(false);
  const [closing, setClosing] = useState<Produccion | null>(null);
  const [cantidadProducida, setCantidadProducida] = useState("");
  const [confirmBusy, setConfirmBusy] = useState(false);
  // Unit symbol of the receta's "unidad de medida resultante" (fetched from the detail response).
  const [closingUnidadSimbolo, setClosingUnidadSimbolo] = useState("Uni");

  // Resumen modal — shown after a successful confirm, with fresh confirmed values.
  const [resumenOpen, setResumenOpen] = useState(false);
  const [resumen, setResumen] = useState<Produccion | null>(null);
  const [resumenLoading, setResumenLoading] = useState(false);

  // Cancel flow (unchanged)
  const [cancelling, setCancelling] = useState<Produccion | null>(null);
  const [cancelBusy, setCancelBusy] = useState(false);
  const [cancelMotivo, setCancelMotivo] = useState("");

  const load = useCallback(async () => {
    try {
      const result = await apiClient<Produccion[]>("/produccion");
      setRows(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar las producciones.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      try {
        const [prods, rec] = await Promise.all([
          apiClient<Produccion[]>("/produccion"),
          apiClient<Receta[]>("/recetas"),
        ]);
        if (cancelled) return;
        setRows(prods);
        setRecetas(rec);
        setError(null);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof ApiError ? err.message : "No se pudieron cargar las producciones.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, []);

  const form = useForm<CreateFormInput, unknown, CreateFormValues>({
    resolver: zodResolver(createSchema),
    defaultValues: { recetaId: "" },
  });

  const applyDetail = useCallback((det: Produccion) => {
    setDetail(det);
    setLines(
      det.insumosConsumidos.map((l) => ({
        key: l.id,
        insumoId: l.insumoId,
        nombre: l.insumo?.nombre ?? l.insumoId,
        cantidad: String(l.cantidad),
      })),
    );
  }, []);

  // ETAPA B opener — also warms the insumos cache so line costs render correctly.
  const openEditor = useCallback(
    async (id: string) => {
      setEditorOpen(true);
      setDetail(null);
      setLines([]);
      setDetailLoading(true);
      try {
        const [det] = await Promise.all([apiClient<Produccion>(`/produccion/${id}`), loadInsumos()]);
        applyDetail(det);
      } catch (err) {
        setEditorOpen(false);
        setError(err instanceof ApiError ? err.message : "No se pudo cargar el detalle.");
        toast.error("No se pudo cargar el detalle.");
      } finally {
        setDetailLoading(false);
      }
    },
    [applyDetail],
  );

  const closeEditor = useCallback(() => {
    setEditorOpen(false);
    setDetail(null);
    setLines([]);
  }, []);

  // Terminar modal opener — fetches the fresh detail to resolve the receta's
  // "unidad de medida resultante" symbol shown next to the "Cantidad producida" input.
  const openClosing = useCallback(async (row: Produccion) => {
    setClosing(row);
    setCantidadProducida("");
    setClosingUnidadSimbolo("Uni");
    setClosingOpen(true);
    try {
      const det = await apiClient<Produccion>(`/produccion/${row.id}`);
      if (det.receta?.unidadMedidaSimbolo) {
        setClosingUnidadSimbolo(det.receta.unidadMedidaSimbolo);
      }
    } catch {
      // Non-blocking: the modal keeps the "Uni" default if the lookup fails.
    }
  }, []);

  const closeClosing = useCallback(() => {
    setClosingOpen(false);
    setClosing(null);
    setCantidadProducida("");
  }, []);

  // Resumen modal opener — fetches the FRESH production detail so the resumen
  // shows the CONFIRMED values (costos recalculados server-side + lote final).
  const openResumen = useCallback(async (id: string, loteFallback: string) => {
    setResumenOpen(true);
    setResumen(null);
    setResumenLoading(true);
    try {
      const det = await apiClient<Produccion>(`/produccion/${id}`);
      setResumen({ ...det, lote: det.lote || loteFallback });
    } catch (err) {
      setResumenOpen(false);
      toast.error(err instanceof ApiError ? err.message : "No se pudo cargar el resumen.");
    } finally {
      setResumenLoading(false);
    }
  }, []);

  const closeResumen = useCallback(() => {
    setResumenOpen(false);
    setResumen(null);
  }, []);

  const openCreate = () => {
    form.reset({ recetaId: "" });
    setRecetaSearch("");
    setDialogOpen(true);
  };

  const filteredRecetas = useMemo(() => {
    const q = recetaSearch.trim().toLowerCase();
    if (!q) return recetas;
    return recetas.filter((r) => r.nombre.toLowerCase().includes(q));
  }, [recetas, recetaSearch]);

  const handleCreate = form.handleSubmit(async (values) => {
    try {
      const payload: CreateProduccionCommand = {
        recetaId: values.recetaId,
        observaciones: null,
      };
      const created = await apiClient<CreateProduccionResponse>("/produccion", {
        method: "POST",
        body: payload,
      });
      toast.success("Producción creada en borrador.");
      setDialogOpen(false);
      await load();
      await openEditor(created.id);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo crear la producción.");
    }
  });

  const isBorrador = detail?.estado === 1;

  const updateLine = (key: string, patch: Partial<EditorLine>) => {
    setLines((prev) => prev.map((l) => (l.key === key ? { ...l, ...patch } : l)));
  };

  // Local (pre-save) computed costs for ETAPA B — live-update as cantidades change.
  const editorLinesWithCost = lines.map((l) => ({
    ...l,
    costo: parseCantidad(l.cantidad) * (getInsumoInfo(l.insumoId)?.precioUltimaCompra ?? 0),
    unidad: getInsumoInfo(l.insumoId)?.unidadConsumo?.simbolo ?? "",
  }));
  const editorTotal = editorLinesWithCost.reduce((acc, l) => acc + l.costo, 0);

  // Informational-only stock warnings: production may proceed even when stock
  // goes negative (backend allows it), so the Confirmar button stays enabled.
  const stockWarnings = isBorrador
    ? editorLinesWithCost.flatMap((l) => {
        const stock = getInsumoInfo(l.insumoId)?.stockActual;
        const cantidad = parseCantidad(l.cantidad);
        return stock !== undefined && cantidad > stock
          ? [`${l.nombre} (requiere ${cantidad}, disponible ${stock})`]
          : [];
      })
    : [];

  const saveLines = useCallback(async (): Promise<boolean> => {
    if (!detail) return false;
    if (lines.length === 0) {
      toast.error("Agregá al menos un insumo al consumo.");
      return false;
    }
    const cantidades = lines.map((l) => Number(l.cantidad));
    if (cantidades.some((c) => !Number.isFinite(c) || c <= 0)) {
      toast.error("Las cantidades deben ser mayores a 0.");
      return false;
    }

    setSavingLines(true);
    try {
      const payload: UpdateProduccionInsumosCommand = {
        produccionId: detail.id,
        lineas: lines.map((l) => ({
          insumoId: l.insumoId,
          cantidad: Number(l.cantidad),
          observaciones: detail.insumosConsumidos.find((d) => d.id === l.key)?.observaciones ?? null,
        })),
      };
      await apiClient(`/produccion/${detail.id}/insumos`, { method: "PUT", body: payload });
      toast.success("Consumo guardado.");
      return true;
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo guardar el consumo.");
      return false;
    } finally {
      setSavingLines(false);
    }
  }, [detail, lines]);

  // ETAPA B [Guardar]: PUT the lines, close this modal and reload the table.
  // The production stays in Borrador — the row then offers [Editar] / [Confirmar].
  const handleConfirmFromEditor = async () => {
    if (!detail) return;
    const saved = await saveLines();
    if (!saved) return;
    setDialogOpen(false);
    closeEditor();
    await load();
  };

  const handleConfirmTerminacion = async () => {
    if (!closing) return;
    const qty = Number(cantidadProducida);
    if (!Number.isFinite(qty) || qty <= 0) {
      toast.error("Ingresá una cantidad producida mayor a 0.");
      return;
    }

    setConfirmBusy(true);
    try {
      // Fetch a FRESH rowVersion right before submitting to avoid concurrency conflicts.
      const fresh = await apiClient<Produccion>(`/produccion/${closing.id}`);
      const body: ConfirmProduccionCommand = {
        produccionId: fresh.id,
        cantidadProducida: qty,
        rowVersion: fresh.rowVersion,
      };
      const res = await apiClient<ConfirmProduccionResponse>(`/produccion/${fresh.id}/confirm`, {
        method: "POST",
        body,
      });
      toast.success(`Producción confirmada. Lote ${res.lote}.`);
      closeClosing();
      await load();
      await openResumen(fresh.id, res.lote);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo confirmar la producción.");
    } finally {
      setConfirmBusy(false);
    }
  };

  // Resumen modal values — from the CONFIRMED detail fetched fresh after the POST.
  const resumenCostoInsumos = resumen
    ? resumen.costoTotalInsumos > 0
      ? resumen.costoTotalInsumos
      : totalCosto(resumen.insumosConsumidos)
    : 0;

  // Default counting unit fallback ("Uni") — overridden by openClosing's detail fetch.
  const handleCancel = async () => {
    if (!cancelling) return;
    setCancelBusy(true);
    try {
      const payload = {
        produccionId: cancelling.id,
        motivo: cancelMotivo.trim() || null,
      };
      await apiClient(`/produccion/${cancelling.id}/cancel`, { method: "POST", body: payload });
      toast.success("Producción cancelada.");
      setCancelling(null);
      setCancelMotivo("");
      await load();
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : "No se pudo cancelar la producción.";
      setError(msg);
      toast.error(msg);
      setCancelling(null);
      setCancelMotivo("");
    } finally {
      setCancelBusy(false);
    }
  };

  const columns: ColumnDef<Produccion, unknown>[] = [
    {
      accessorKey: "lote",
      header: "Lote",
      cell: ({ row }) => row.original.lote || "—",
    },
    {
      id: "receta",
      header: "Receta",
      cell: ({ row }) => row.original.receta?.nombre ?? "—",
    },
    {
      id: "fecha",
      header: "Fecha",
      cell: ({ row }) => new Date(row.original.fecha).toLocaleDateString("es-AR"),
    },
    {
      accessorKey: "costoTotal",
      header: "Costo total",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
    {
      id: "estado",
      header: "Estado",
      cell: ({ row }) => <EstadoBadge estado={row.original.estado} />,
    },
  ];

  const {
    control,
    formState: { errors, isSubmitting },
  } = form;

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nueva producción" title="Nueva producción">
              <Plus className="size-5" />
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>          </>
        }
      />

      <DataTable
        columns={columns}
        data={rows}
        loading={loading}
        error={error}
        emptyMessage="No hay producciones."
        actions={(row) => (
          <>
            {row.estado === 1 ? (
              <>
                <Button variant="outline" size="sm" onClick={() => void openEditor(row.id)}>
                  Editar
                </Button>
                <Button size="sm" onClick={() => void openClosing(row)}>
                  Confirmar
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => {
                    setCancelMotivo("");
                    setCancelling(row);
                  }}
                >
                  Cancelar
                </Button>
              </>
            ) : (
              <Button variant="outline" size="sm" onClick={() => void openEditor(row.id)}>
                Ver
              </Button>
            )}
          </>
        )}
      />

      {/* ETAPA A — Nueva producción (compact) */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Nueva producción</DialogTitle>
            <DialogDescription>
              Se creará un borrador con el consumo precargado desde la receta.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleCreate} className="flex flex-col gap-3" noValidate>
            <Controller
              control={control}
              name="recetaId"
              render={({ field }) => (
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="produccion-receta-search">Receta</Label>
                  <div className="relative">
                    <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      id="produccion-receta-search"
                      className="pl-8"
                      placeholder="Buscar receta por nombre…"
                      value={recetaSearch}
                      onChange={(e) => setRecetaSearch(e.target.value)}
                      autoComplete="off"
                    />
                  </div>
                  <div className="max-h-48 overflow-y-auto rounded-md border border-border">
                    {filteredRecetas.length === 0 ? (
                      <p className="px-3 py-4 text-center text-sm text-muted-foreground">
                        Sin resultados.
                      </p>
                    ) : (
                      filteredRecetas.map((r) => {
                        const selected = field.value === r.id;
                        return (
                          <button
                            key={r.id}
                            type="button"
                            role="option"
                            aria-selected={selected}
                            onClick={() => field.onChange(r.id)}
                            className={`flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-sm ${
                              selected
                                ? "bg-accent text-accent-foreground"
                                : "hover:bg-muted/60"
                            }`}
                          >
                            <span className="truncate">{r.nombre}</span>
                            {selected && <Check className="size-4 shrink-0" />}
                          </button>
                        );
                      })
                    )}
                  </div>
                  <FieldError message={errors.recetaId?.message} />
                </div>
              )}
            />

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Creando…" : "Crear"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ETAPA B — Producción (header + insumos; cantidades editables solo Borrador) */}
      <Dialog
        open={editorOpen}
        onOpenChange={(open) => {
          if (!open) closeEditor();
        }}
      >
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>Producción · {detail?.receta?.nombre || detail?.id || ""}</DialogTitle>
            <DialogDescription>
              {isBorrador
                ? "Ajustá las cantidades y guardá el consumo. Podés seguir editando después desde la fila."
                : "Detalle del consumo de insumos y costos."}
            </DialogDescription>
          </DialogHeader>

          {detailLoading ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Cargando detalle…</p>
          ) : detail ? (
            <div className="flex flex-col gap-4">
              <Card>
                <CardHeader>
                  <CardTitle className="text-sm">Datos</CardTitle>
                </CardHeader>
                <CardContent className="grid grid-cols-1 gap-x-4 gap-y-2 text-sm sm:grid-cols-2">
                  <div>
                    <span className="font-medium">Receta:</span> {detail.receta?.nombre ?? "—"}
                  </div>
                  <div className="flex items-center gap-1.5">
                    <span className="font-medium">Estado:</span>
                    <EstadoBadge estado={detail.estado} />
                  </div>
                  <div>
                    <span className="font-medium">Fecha:</span>{" "}
                    {new Date(detail.fecha).toLocaleDateString("es-AR")}
                  </div>
                  <div>
                    <span className="font-medium">Lote:</span> {detail.lote || "—"}
                  </div>
                  {detail.cantidadProducida > 0 && (
                    <div>
                      <span className="font-medium">Cantidad producida:</span>{" "}
                      {detail.cantidadProducida}
                    </div>
                  )}
                  {detail.observaciones && (
                    <div className="sm:col-span-2">
                      <span className="font-medium">Observaciones:</span> {detail.observaciones}
                    </div>
                  )}
                </CardContent>
              </Card>

              {isBorrador && stockWarnings.length > 0 && (
                <div className="rounded-md border border-amber-600/30 bg-amber-500/10 px-3 py-2 text-sm text-amber-700 dark:text-amber-400">
                  ⚠ Stock insuficiente: {stockWarnings.join(" · ")}
                </div>
              )}

              <Card>
                <CardHeader>
                  <CardTitle className="text-sm">Insumos consumidos</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="overflow-x-auto rounded-lg border border-border">
                    <table className="w-full min-w-[36rem] text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-left text-xs uppercase tracking-wide text-muted-foreground">
                          <th className="px-3 py-2 font-semibold">Insumo</th>
                          <th className="w-40 px-3 py-2 font-semibold">Cantidad</th>
                          <th className="w-20 px-3 py-2 font-semibold">Unidad</th>
                          <th className="w-28 px-3 py-2 font-semibold">Costo</th>
                        </tr>
                      </thead>
                      <tbody>
                        {editorLinesWithCost.length === 0 ? (
                          <tr>
                            <td colSpan={4} className="px-3 py-6 text-center text-muted-foreground">
                              No hay insumos consumidos.
                            </td>
                          </tr>
                        ) : (
                          editorLinesWithCost.map((line) => (
                            <tr key={line.key} className="border-b border-border/50 last:border-0">
                              <td className="px-3 py-2">{line.nombre}</td>
                              <td className="px-3 py-2">
                                <Input
                                  type="number"
                                  step="any"
                                  min="0"
                                  value={line.cantidad}
                                  disabled={!isBorrador}
                                  onChange={(e) =>
                                    updateLine(line.key, { cantidad: e.target.value })
                                  }
                                  aria-label={`Cantidad de ${line.nombre}`}
                                />
                              </td>
                              <td className="px-3 py-2 text-muted-foreground">{line.unidad || "—"}</td>
                              <td className="px-3 py-2 tabular-nums">
                                {MONEY.format(line.costo)}
                              </td>
                            </tr>
                          ))
                        )}
                      </tbody>
                      {editorLinesWithCost.length > 0 && (
                        <tfoot>
                          <tr className="border-t border-border bg-muted/30">
                            <td className="px-3 py-2 font-semibold" colSpan={3}>
                              Total de costos
                            </td>
                            <td className="px-3 py-2 font-semibold tabular-nums">
                              {MONEY.format(editorTotal)}
                            </td>
                          </tr>
                        </tfoot>
                      )}
                    </table>
                  </div>
                </CardContent>
              </Card>
            </div>
          ) : null}

          <DialogFooter>
            {isBorrador && (
              <Button
                type="button"
                onClick={() => void handleConfirmFromEditor()}
                disabled={savingLines || detailLoading}
              >
                {savingLines ? "Guardando…" : "Guardar"}
              </Button>
            )}
            <Button type="button" variant="ghost" onClick={closeEditor}>
              Cerrar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Confirmar producción — minimal: solo cantidad producida. El resumen se
          muestra DESPUÉS de confirmar, en su propio modal. */}
      <Dialog
        open={closingOpen}
        onOpenChange={(open) => {
          if (!open) closeClosing();
        }}
      >
        <DialogContent className="sm:max-w-md" showCloseButton={false}>
          {/* Header row: icon + close */}
          <div className="flex items-start justify-between">
            <div className="flex size-12 items-center justify-center rounded-full bg-blue-50">
              <PackageCheck className="size-6 text-blue-500" />
            </div>
            <DialogClose asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="rounded-full"
                aria-label="Cerrar"
              >
                <X className="size-4" />
              </Button>
            </DialogClose>
          </div>

          <DialogTitle>Confirmar producción</DialogTitle>

          {/* Recipe highlight */}
          <div className="rounded-lg border border-blue-100 bg-blue-50 p-4">
            <span className="inline-block rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-semibold text-blue-600">
              RECETA
            </span>
            <p className="mt-1.5 text-sm font-semibold text-blue-700">
              {closing?.receta?.nombre ?? "—"}
            </p>
          </div>

          <DialogDescription>
            Al confirmar se descontarán los insumos de la receta y se incrementará el stock del
            producto terminado.
          </DialogDescription>

          <div className="my-4 border-t border-gray-100" />

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="produccion-cantidad-producida">Cantidad producida</Label>
            <div className="flex items-center gap-2">
              <Input
                id="produccion-cantidad-producida"
                type="number"
                step="any"
                min="0"
                className="flex-1"
                value={cantidadProducida}
                onChange={(e) => setCantidadProducida(e.target.value)}
                placeholder="Ingrese la cantidad producida"
                disabled={confirmBusy}
              />
              <span className="shrink-0 text-sm text-muted-foreground">
                {closingUnidadSimbolo}
              </span>
            </div>
          </div>

          {/* Info alert */}
          <div className="flex items-start gap-2 rounded-lg border border-blue-200 bg-blue-50 p-3">
            <Info className="mt-0.5 size-4 shrink-0 text-blue-500" />
            <p className="text-sm text-blue-700">
              Esta acción no se puede deshacer. Verifique la cantidad antes de confirmar.
            </p>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={closeClosing} disabled={confirmBusy}>
              Cancelar
            </Button>
            <Button
              type="button"
              onClick={() => void handleConfirmTerminacion()}
              disabled={confirmBusy}
            >
              {confirmBusy ? "Confirmando…" : "Confirmar"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Resumen post-confirmación — valores CONFIRMADOS (detalle fresco del server) */}
      <Dialog
        open={resumenOpen}
        onOpenChange={(open) => {
          if (!open) closeResumen();
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Producción confirmada</DialogTitle>
            <DialogDescription>
              Se generó el producto terminado y se descontó el consumo de stock.
            </DialogDescription>
          </DialogHeader>

          {resumenLoading ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Cargando resumen…</p>
          ) : resumen ? (
            <div className="grid grid-cols-1 gap-x-4 gap-y-2 text-sm sm:grid-cols-2">
              <div className="sm:col-span-2">
                <span className="font-medium">Receta:</span> {resumen.receta?.nombre ?? "—"}
              </div>
              <div>
                <span className="font-medium">Cantidad producida:</span> {resumen.cantidadProducida}
              </div>
              <div>
                <span className="font-medium">Lote:</span> {resumen.lote || "—"}
              </div>
              <div>
                <span className="font-medium">Costo total de insumos:</span>{" "}
                {MONEY.format(resumenCostoInsumos)}
              </div>
            </div>
          ) : null}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={closeResumen}>
              Cerrar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={cancelling !== null}
        onOpenChange={(open) => {
          if (!open) {
            setCancelling(null);
            setCancelMotivo("");
          }
        }}
        title="Cancelar producción"
        message={`¿Seguro que querés cancelar la producción de "${cancelling?.receta?.nombre ?? ""}"?`}
        confirmLabel="Cancelar producción"
        destructive
        busy={cancelBusy}
        onConfirm={() => void handleCancel()}
      >
        <div className="flex flex-col gap-1.5 px-6">
          <Label htmlFor="produccion-cancel-motivo">Motivo (opcional)</Label>
          <Input
            id="produccion-cancel-motivo"
            value={cancelMotivo}
            onChange={(e) => setCancelMotivo(e.target.value)}
          />
        </div>
      </ConfirmDialog>
    </div>
  );
}
