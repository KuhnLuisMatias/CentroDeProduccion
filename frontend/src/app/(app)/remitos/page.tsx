"use client";

import { useCallback, useEffect, useState } from "react";
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
  UpdateEstadoRemitoCommand,
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
import { Badge } from "@/components/ui/badge";
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

const EMPTY_FORM: RemitoFormInput = {
  barId: "",
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

function estadoBadgeClass(estado: EstadoRemito) {
  if (estado === 2)
    return "border-sky-600/30 bg-sky-500/10 text-sky-700 dark:text-sky-400";
  if (estado === 3)
    return "border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400";
  if (estado === 4)
    return "border-red-600/30 bg-red-500/10 text-red-700 dark:text-red-400";
  if (estado === 1)
    return "border-amber-600/30 bg-amber-500/10 text-amber-700 dark:text-amber-400";
  return undefined;
}

type EstadoAction = "estado" | "cancelar" | "confirmar";

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

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<{ row: RemitoListItem; rowVersion: string } | null>(null);

  const [detail, setDetail] = useState<Remito | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const [confirmState, setConfirmState] = useState<{
    action: EstadoAction;
    row: RemitoListItem;
  } | null>(null);
  const [actionBusy, setActionBusy] = useState(false);

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
    form.reset({ ...EMPTY_FORM, lineas: [{ ...EMPTY_LINE, id: `line-${Date.now()}` }] });
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
      const { action, row } = confirmState;
      const det = await apiClient<Remito>(`/remitos/${row.id}`);
      const rowVersion = det.rowVersion;
      if (action === "estado") {
        const target: EstadoRemito = row.estado === 1 ? 2 : 1;
        const payload: UpdateEstadoRemitoCommand = { remitoId: row.id, estado: target, rowVersion };
        await apiClient<unknown>(`/remitos/${row.id}/estado`, { method: "PUT", body: payload });
        toast.success(`Remito N° ${row.numeroRemito}: estado actualizado.`);
      } else if (action === "cancelar") {
        const payload: CancelarRemitoCommand = { remitoId: row.id, rowVersion };
        await apiClient<unknown>(`/remitos/${row.id}/cancelar`, { method: "POST", body: payload });
        toast.success(`Remito N° ${row.numeroRemito} cancelado.`);
      } else {
        const payload: ConfirmRemitoCommand = { remitoId: row.id, rowVersion };
        await apiClient<unknown>(`/remitos/${row.id}/confirmar`, { method: "POST", body: payload });
        toast.success(`Remito N° ${row.numeroRemito} confirmado.`);
      }
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
      accessorKey: "estado",
      header: "Estado",
      cell: ({ row }) => (
        <Badge variant="outline" className={estadoBadgeClass(row.original.estado)}>
          {ESTADO_REMITO_LABELS[row.original.estado] ?? String(row.original.estado)}
        </Badge>
      ),
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

  const printFormats = [
    { value: "a4", label: "A4" },
    { value: "ticket", label: "Ticket" },
  ];

  return (
    <div>
      <PageHeader
        title="Pedidos y Remitos"
        description="Envíos de productos a los bares."
        actions={
          <>
            <Button size="sm" onClick={openCreate}>
              <Plus className="size-4" />
              Nuevo remito
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-4 ${loading ? "animate-spin" : ""}`} />
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
        <Input
          type="date"
          className="w-[150px]"
          value={filtroDesde}
          onChange={(e) => setFiltroDesde(e.target.value)}
          aria-label="Fecha desde"
        />
        <Input
          type="date"
          className="w-[150px]"
          value={filtroHasta}
          onChange={(e) => setFiltroHasta(e.target.value)}
          aria-label="Fecha hasta"
        />
      </div>

      <DataTable
        columns={columns}
        data={rows}
        loading={loading}
        error={error}
        emptyMessage="No hay remitos."
        actions={(row) => (
          <>
            <Button variant="outline" size="sm" onClick={() => void openDetail(row)}>
              Ver
            </Button>
            {canMutate(row) && (
              <Button variant="outline" size="sm" onClick={() => void openEdit(row)}>
                Editar
              </Button>
            )}
            {canMutate(row) && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => setConfirmState({ action: "estado", row })}
              >
                {row.estado === 1 ? "→ En proceso" : "→ Pendiente"}
              </Button>
            )}
            {canMutate(row) && (
              <Button size="sm" onClick={() => setConfirmState({ action: "confirmar", row })}>
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
              <Label htmlFor="remito-entregadoPor">Entregado por</Label>
              <Input id="remito-entregadoPor" {...register("entregadoPor")} />
              <FieldError message={errors.entregadoPor?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="remito-recibidoPor">Recibido por</Label>
              <Input id="remito-recibidoPor" {...register("recibidoPor")} />
              <FieldError message={errors.recibidoPor?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="remito-observaciones">Observaciones</Label>
              <Input id="remito-observaciones" {...register("observaciones")} />
              <FieldError message={errors.observaciones?.message} />
            </div>

            <div className="flex flex-col gap-2 sm:col-span-2">
              <Label>Líneas</Label>
              {fields.map((field, index) => {
                const tipoLinea = Number(watchedLineas?.[index]?.tipoLinea);
                return (
                  <div key={field.id} className="grid grid-cols-[1fr_1.4fr_0.8fr_0.8fr_auto] items-start gap-2">
                    <div className="flex flex-col gap-1">
                      <Label className="text-xs text-muted-foreground">Tipo</Label>
                      <Controller
                        control={control}
                        name={`lineas.${index}.tipoLinea`}
                        render={({ field: f }) => (
                          <Select
                            value={f.value}
                            onValueChange={(v) => {
                              f.onChange(v);
                              form.setValue(`lineas.${index}.productoTerminadoId`, "");
                              form.setValue(`lineas.${index}.insumoId`, "");
                            }}
                          >
                            <SelectTrigger className="w-full">
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              {(Object.keys(TIPO_LINEA_REMITO_LABELS) as unknown as string[]).map((v) => (
                                <SelectItem key={v} value={v}>
                                  {TIPO_LINEA_REMITO_LABELS[Number(v) as TipoLineaRemito]}
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        )}
                      />
                    </div>
                    {tipoLinea === 1 ? (
                      <div className="flex flex-col gap-1">
                        <Label className="text-xs text-muted-foreground">Producto terminado</Label>
                        <Controller
                          control={control}
                          name={`lineas.${index}.productoTerminadoId`}
                          render={({ field: f }) => (
                            <Select value={f.value || undefined} onValueChange={f.onChange}>
                              <SelectTrigger className="w-full">
                                <SelectValue placeholder="Seleccionar…" />
                              </SelectTrigger>
                              <SelectContent>
                                {productos.map((p) => (
                                  <SelectItem key={p.id} value={p.id}>
                                    {p.nombre}
                                  </SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                          )}
                        />
                        <FieldError
                          message={errors.lineas?.[index]?.productoTerminadoId?.message}
                        />
                      </div>
                    ) : (
                      <div className="flex flex-col gap-1">
                        <Label className="text-xs text-muted-foreground">Insumo</Label>
                        <Controller
                          control={control}
                          name={`lineas.${index}.insumoId`}
                          render={({ field: f }) => (
                            <Select value={f.value || undefined} onValueChange={f.onChange}>
                              <SelectTrigger className="w-full">
                                <SelectValue placeholder="Seleccionar…" />
                              </SelectTrigger>
                              <SelectContent>
                                {insumos.map((i) => (
                                  <SelectItem key={i.id} value={i.id}>
                                    {i.nombre}
                                  </SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                          )}
                        />
                        <FieldError message={errors.lineas?.[index]?.insumoId?.message} />
                      </div>
                    )}
                    <div className="flex flex-col gap-1">
                      <Label className="text-xs text-muted-foreground">Cantidad</Label>
                      <Input type="number" step="any" min="0" {...register(`lineas.${index}.cantidad`)} />
                      <FieldError message={errors.lineas?.[index]?.cantidad?.message} />
                    </div>
                    <div className="flex flex-col gap-1">
                      <Label className="text-xs text-muted-foreground">Lote</Label>
                      <Input placeholder="Opcional" {...register(`lineas.${index}.lote`)} />
                      <FieldError message={errors.lineas?.[index]?.lote?.message} />
                    </div>
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
                );
              })}
              <div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => append({ ...EMPTY_LINE, id: `line-${Date.now()}` })}
                >
                  <Plus className="size-4" />
                  Agregar línea
                </Button>
              </div>
              <FieldError message={errors.lineas?.root?.message ?? errors.lineas?.message} />
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
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Remito N° {detail?.numeroRemito}</DialogTitle>
            <DialogDescription>Detalle del remito y sus líneas.</DialogDescription>
          </DialogHeader>

          {detailLoading ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Cargando detalle…</p>
          ) : detail ? (
            <div className="flex flex-col gap-4">
              <div className="grid grid-cols-1 gap-x-4 gap-y-2 text-sm sm:grid-cols-2">
                <div>
                  <span className="font-medium">Bar:</span> {detail.barNombre || "—"}
                </div>
                <div>
                  <span className="font-medium">Fecha:</span>{" "}
                  {new Date(detail.fecha).toLocaleString("es-AR")}
                </div>
                <div>
                  <span className="font-medium">Estado:</span>{" "}
                  <Badge variant="outline" className={estadoBadgeClass(detail.estado)}>
                    {ESTADO_REMITO_LABELS[detail.estado] ?? String(detail.estado)}
                  </Badge>
                </div>
                <div>
                  <span className="font-medium">Total:</span> {MONEY.format(detail.total)}
                </div>
                {detail.observaciones && (
                  <div className="sm:col-span-2">
                    <span className="font-medium">Observaciones:</span> {detail.observaciones}
                  </div>
                )}
                {detail.entregadoPor && (
                  <div>
                    <span className="font-medium">Entregado por:</span> {detail.entregadoPor}
                  </div>
                )}
                {detail.recibidoPor && (
                  <div>
                    <span className="font-medium">Recibido por:</span> {detail.recibidoPor}
                  </div>
                )}
              </div>

              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Tipo</TableHead>
                    <TableHead>Detalle</TableHead>
                    <TableHead className="text-right">Cantidad</TableHead>
                    <TableHead className="text-left">P. unitario</TableHead>
                    <TableHead className="text-left">Subtotal</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detail.lineas.map((l) => (
                    <TableRow key={l.id}>
                      <TableCell>{TIPO_LINEA_REMITO_LABELS[l.tipoLinea] ?? l.tipoLinea}</TableCell>
                      <TableCell>{l.tipoLinea === 1 ? l.productoTerminadoNombre : l.insumoNombre}</TableCell>
                      <TableCell className="text-right">{l.cantidad}</TableCell>
                      <TableCell className="text-left">{MONEY.format(l.precioUnitario)}</TableCell>
                      <TableCell className="text-left">{MONEY.format(l.subtotal)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <p className="text-right text-sm font-medium">Total: {MONEY.format(detail.total)}</p>
            </div>
          ) : null}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setDetail(null)}>
              Cerrar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmState?.action === "estado"}
        onOpenChange={(open) => {
          if (!open) setConfirmState(null);
        }}
        title="Cambiar estado"
        message={`¿Cambiar el estado del remito N° ${confirmState?.row.numeroRemito ?? ""} entre Pendiente y En Proceso?`}
        confirmLabel="Aceptar"
        busy={actionBusy && confirmState?.action === "estado"}
        onConfirm={() => void runEstadoAction()}
      />

      <ConfirmDialog
        open={confirmState?.action === "confirmar"}
        onOpenChange={(open) => {
          if (!open) setConfirmState(null);
        }}
        title="Confirmar remito"
        message={`¿Confirmar el envío del remito N° ${confirmState?.row.numeroRemito ?? ""}? El estado pasará a Enviado.`}
        confirmLabel="Aceptar"
        busy={actionBusy && confirmState?.action === "confirmar"}
        onConfirm={() => void runEstadoAction()}
      />

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
