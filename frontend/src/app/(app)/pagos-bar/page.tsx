"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useForm, Controller, useFieldArray, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { Plus, RefreshCw, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type {
  PagoBar,
  PagoBarList,
  BarListItem,
  RemitoListItem,
  MetodoPago,
  CreatePagoBarCommand,
} from "@/lib/types";
import { METODO_PAGO_LABELS, ESTADO_REMITO_LABELS } from "@/lib/types";
import type { AllocationEntity } from "@/components/shared/AllocationRows";
import {
  AllocationEntityCard,
  AllocationSummaryStrip,
  allocationAmount,
} from "@/components/shared/AllocationRows";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
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

const metodoSchema = z.object({
  id: z.string(),
  tipo: z.string(),
  monto: z.coerce
    .number({ message: "Ingresá un número válido." })
    .min(0, "No puede ser negativo."),
  referencia: z.string().max(500, "Máximo 500 caracteres."),
});

const itemSchema = z.object({
  id: z.string(),
  remitoId: z.string().min(1, "Seleccioná un remito."),
  montoAplicado: z.coerce
    .number({ message: "Ingresá un número válido." })
    .positive("Debe ser mayor a 0."),
});

const pagoBarSchema = z
  .object({
    barId: z.string().min(1, "Seleccioná un bar."),
    fechaPago: z.string().min(1, "La fecha de pago es obligatoria."),
    montoTotal: z.coerce
      .number({ message: "Ingresá un número válido." })
      .positive("Debe ser mayor a 0."),
    observaciones: z.string().max(1000, "Máximo 1000 caracteres."),
    metodos: z.array(metodoSchema),
    items: z.array(itemSchema),
  })
  .superRefine((values, ctx) => {
    const total = values.montoTotal;
    const sumMetodos = values.metodos.reduce((s, m) => s + (Number(m.monto) || 0), 0);
    const sumItems = values.items.reduce((s, a) => s + (Number(a.montoAplicado) || 0), 0);
    if (sumMetodos !== total) {
      ctx.addIssue({
        code: "custom",
        path: ["metodos"],
        message: "La suma de los métodos de pago debe ser igual al monto total.",
      });
    }
    if (sumItems !== total) {
      ctx.addIssue({
        code: "custom",
        path: ["items"],
        message: "La suma de las asignaciones por remito debe ser igual al monto total.",
      });
    }
  });

type PagoBarFormInput = z.input<typeof pagoBarSchema>;
type PagoBarFormValues = z.output<typeof pagoBarSchema>;

interface FieldErrorProps {
  message?: string;
}

const REMITO_ESTADO_TONES: Record<number, AllocationEntity["estadoTone"]> = {
  1: "neutral",
  2: "warning",
  3: "info",
  4: "danger",
};

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function PagosBarPage() {
  const [rows, setRows] = useState<PagoBarList[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [bares, setBares] = useState<BarListItem[]>([]);
  const [remitos, setRemitos] = useState<RemitoListItem[]>([]);

  // Filters
  const [filtroBar, setFiltroBar] = useState("all");
  const [filtroDesde, setFiltroDesde] = useState("");
  const [filtroHasta, setFiltroHasta] = useState("");

  const [dialogOpen, setDialogOpen] = useState(false);

  const [detail, setDetail] = useState<PagoBar | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const buildQuery = useCallback(() => {
    const params = new URLSearchParams();
    if (filtroBar && filtroBar !== "all") params.set("barId", filtroBar);
    if (filtroDesde) params.set("fechaDesde", filtroDesde);
    if (filtroHasta) params.set("fechaHasta", filtroHasta);
    const qs = params.toString();
    return `/pagos-bar${qs ? `?${qs}` : ""}`;
  }, [filtroBar, filtroDesde, filtroHasta]);

  const load = useCallback(async () => {
    try {
      const result = await apiClient<PagoBarList[]>(buildQuery());
      setRows(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar los pagos.");
    } finally {
      setLoading(false);
    }
  }, [buildQuery]);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      try {
        const [pagos, barList, remitoList] = await Promise.all([
          apiClient<PagoBarList[]>(buildQuery()),
          apiClient<BarListItem[]>("/bares"),
          apiClient<RemitoListItem[]>("/remitos"),
        ]);
        if (cancelled) return;
        setRows(pagos);
        setBares(barList);
        setRemitos(remitoList);
        setError(null);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof ApiError ? err.message : "No se pudieron cargar los pagos.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, [buildQuery]);

  const remitosEnviados = remitos.filter((r) => r.estado === 3);

  const form = useForm<PagoBarFormInput, unknown, PagoBarFormValues>({
    resolver: zodResolver(pagoBarSchema),
    defaultValues: {
      barId: "",
      fechaPago: new Date().toISOString().slice(0, 10),
      montoTotal: "",
      observaciones: "",
      metodos: [],
      items: [],
    },
  });

  const metodosArray = useFieldArray({ control: form.control, name: "metodos" });
  const itemsArray = useFieldArray({ control: form.control, name: "items" });

  const openCreate = () => {
    form.reset({
      barId: "",
      fechaPago: new Date().toISOString().slice(0, 10),
      montoTotal: "",
      observaciones: "",
      metodos: [{ id: "", tipo: "1", monto: "", referencia: "" }],
      items: [],
    });
    setDialogOpen(true);
  };

  const handleSave = form.handleSubmit(async (values) => {
    const payload: CreatePagoBarCommand = {
      barId: values.barId,
      fechaPago: values.fechaPago || null,
      montoTotal: values.montoTotal,
      observaciones: values.observaciones.trim() || null,
      metodos: values.metodos.map((m) => ({
        tipo: Number(m.tipo) as MetodoPago,
        monto: m.monto,
        referencia: m.referencia.trim() || null,
      })),
      items: values.items.map((a) => ({
        remitoId: a.remitoId,
        montoAplicado: a.montoAplicado,
      })),
    };
    try {
      await apiClient<unknown>("/pagos-bar", { method: "POST", body: payload });
      toast.success("Pago creado.");
      setDialogOpen(false);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo crear el pago.");
    }
  });

  const openDetail = async (row: PagoBarList) => {
    setDetail(row as unknown as PagoBar);
    setDetailLoading(true);
    try {
      const det = await apiClient<PagoBar>(`/pagos-bar/${row.id}`);
      setDetail(det);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo cargar el detalle.");
    } finally {
      setDetailLoading(false);
    }
  };

  const columns: ColumnDef<PagoBarList, unknown>[] = [
    { accessorKey: "numero", header: "N°" },
    {
      id: "bar",
      header: "Bar",
      cell: ({ row }) => row.original.barNombre || "—",
    },
    {
      id: "fecha",
      header: "Fecha",
      cell: ({ row }) => new Date(row.original.fechaPago).toLocaleDateString("es-AR"),
    },
    {
      accessorKey: "montoTotal",
      header: "Monto",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
    { accessorKey: "metodoCount", header: "Métodos" },
  ];

  const {
    register,
    watch,
    control,
    formState: { errors, isSubmitting },
  } = form;

  const watchedMetodos = useWatch({ control, name: "metodos" }) ?? [];
  const watchedItems = useWatch({ control, name: "items" }) ?? [];
  const sumMetodos = watchedMetodos.reduce((s, m) => s + (parseFloat(String(m?.monto)) || 0), 0);
  const sumItems = watchedItems.reduce(
    (s, a) => s + (parseFloat(String(a?.montoAplicado)) || 0),
    0,
  );

  const remitoById = useMemo(
    () => new Map(remitosEnviados.map((r) => [r.id, r])),
    [remitosEnviados],
  );

  const buildRemitoEntity = (remitoId: string): AllocationEntity | null => {
    const r = remitoById.get(remitoId);
    if (!r) return null;
    return {
      id: r.id,
      numero: r.numeroRemito,
      nombre: r.barNombre,
      estadoLabel: ESTADO_REMITO_LABELS[r.estado] ?? String(r.estado),
      estadoTone: REMITO_ESTADO_TONES[r.estado] ?? "neutral",
      total: r.total,
      // Prior payments per remito are not available in the list endpoint.
      yaPagado: null,
    };
  };

  const watchedMontoTotal = allocationAmount(watch("montoTotal"));

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nuevo pago" title="Nuevo pago">
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
        emptyMessage="No hay pagos."
        actions={(row) => (
          <Button variant="outline" size="sm" onClick={() => void openDetail(row)}>
            Ver
          </Button>
        )}
      />

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>Nuevo pago de bar</DialogTitle>
            <DialogDescription>
              Los métodos de pago y las asignaciones deben sumar el monto total.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="pago-bar">Bar</Label>
              <Controller
                control={control}
                name="barId"
                render={({ field }) => (
                  <Select value={field.value || undefined} onValueChange={field.onChange}>
                    <SelectTrigger id="pago-bar" className="w-full">
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
              <Label htmlFor="pago-bar-fecha">Fecha de pago</Label>
              <Input id="pago-bar-fecha" type="date" {...register("fechaPago")} />
              <FieldError message={errors.fechaPago?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="pago-bar-montoTotal">Monto total</Label>
              <Input id="pago-bar-montoTotal" type="number" step="any" min="0" {...register("montoTotal")} />
              <FieldError message={errors.montoTotal?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="pago-bar-observaciones">Observaciones</Label>
              <Input id="pago-bar-observaciones" {...register("observaciones")} />
              <FieldError message={errors.observaciones?.message} />
            </div>

            <div className="flex flex-col gap-2 sm:col-span-2">
              <Label>Métodos de pago (suma: {MONEY.format(sumMetodos)})</Label>
              {metodosArray.fields.map((field, index) => (
                <div key={field.id} className="grid grid-cols-[1fr_0.8fr_1fr_auto] items-start gap-2">
                  <div className="flex flex-col gap-1">
                    <Label className="text-xs text-muted-foreground">Tipo</Label>
                    <Controller
                      control={control}
                      name={`metodos.${index}.tipo`}
                      render={({ field: f }) => (
                        <Select value={f.value} onValueChange={f.onChange}>
                          <SelectTrigger className="w-full">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            {(Object.keys(METODO_PAGO_LABELS) as unknown as string[]).map((v) => (
                              <SelectItem key={v} value={v}>
                                {METODO_PAGO_LABELS[Number(v) as MetodoPago]}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )}
                    />
                  </div>
                  <div className="flex flex-col gap-1">
                    <Label className="text-xs text-muted-foreground">Monto</Label>
                    <Input type="number" step="any" min="0" {...register(`metodos.${index}.monto`)} />
                  </div>
                  <div className="flex flex-col gap-1">
                    <Label className="text-xs text-muted-foreground">Referencia</Label>
                    <Input placeholder="Opcional" {...register(`metodos.${index}.referencia`)} />
                  </div>
                  <Button
                    type="button"
                    variant="destructive"
                    size="icon"
                    onClick={() => metodosArray.remove(index)}
                    aria-label="Eliminar método"
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </div>
              ))}
              <div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() =>
                    metodosArray.append({ id: "", tipo: "1", monto: "", referencia: "" })
                  }
                >
                  <Plus className="size-4" />
                  Agregar método
                </Button>
              </div>
              <FieldError message={errors.metodos?.root?.message ?? errors.metodos?.message} />
            </div>

            <div className="flex flex-col gap-2 sm:col-span-2">
              <Label>Asignaciones por remito (suma: {MONEY.format(sumItems)})</Label>
              {itemsArray.fields.map((field, index) => {
                const remitoId = String(watchedItems[index]?.remitoId ?? "");
                const amount = allocationAmount(watchedItems[index]?.montoAplicado);
                return (
                  <AllocationEntityCard
                    key={field.id}
                    docLabel="Remito"
                    entity={buildRemitoEntity(remitoId)}
                    amount={amount}
                    onRemove={() => itemsArray.remove(index)}
                    removeLabel="Eliminar asignación"
                    selectSlot={
                      <>
                        <Controller
                          control={control}
                          name={`items.${index}.remitoId`}
                          render={({ field: f }) => (
                            <Select value={f.value || undefined} onValueChange={f.onChange}>
                              <SelectTrigger className="w-full">
                                <SelectValue placeholder="Seleccionar remito…" />
                              </SelectTrigger>
                              <SelectContent>
                                {remitosEnviados.map((r) => (
                                  <SelectItem key={r.id} value={r.id}>
                                    N° {r.numeroRemito} — {r.barNombre}
                                  </SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                          )}
                        />
                        <FieldError message={errors.items?.[index]?.remitoId?.message} />
                      </>
                    }
                    amountSlot={
                      <>
                        <Label className="text-xs text-muted-foreground">Monto a aplicar</Label>
                        <Input
                          type="number"
                          step="any"
                          min="0"
                          {...register(`items.${index}.montoAplicado`)}
                        />
                        <FieldError
                          message={errors.items?.[index]?.montoAplicado?.message}
                        />
                      </>
                    }
                  />
                );
              })}
              <div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => itemsArray.append({ id: "", remitoId: "", montoAplicado: "" })}
                >
                  <Plus className="size-4" />
                  Asignar remito
                </Button>
              </div>
              <FieldError message={errors.items?.root?.message ?? errors.items?.message} />
              <AllocationSummaryStrip asignado={sumItems} montoTotal={watchedMontoTotal} />
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
            <DialogTitle>Pago N° {detail?.numero}</DialogTitle>
            <DialogDescription>Métodos de pago y asignaciones.</DialogDescription>
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
                  {new Date(detail.fechaPago).toLocaleDateString("es-AR")}
                </div>
                <div>
                  <span className="font-medium">Monto total:</span> {MONEY.format(detail.montoTotal)}
                </div>
                {detail.observaciones && (
                  <div className="sm:col-span-2">
                    <span className="font-medium">Observaciones:</span> {detail.observaciones}
                  </div>
                )}
              </div>

              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Tipo</TableHead>
                    <TableHead className="text-left">Monto</TableHead>
                    <TableHead>Referencia</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detail.metodos.map((m, i) => (
                    <TableRow key={i}>
                      <TableCell>{METODO_PAGO_LABELS[m.tipo] ?? m.tipo}</TableCell>
                      <TableCell className="text-left">{MONEY.format(m.monto)}</TableCell>
                      <TableCell>{m.referencia || "—"}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>

              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Remito</TableHead>
                    <TableHead className="text-left">Monto aplicado</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detail.items.map((a, i) => (
                    <TableRow key={i}>
                      <TableCell>N° {a.remitoNumeroRemito}</TableCell>
                      <TableCell className="text-left">{MONEY.format(a.montoAplicado)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          ) : null}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setDetail(null)}>
              Cerrar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
