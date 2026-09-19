"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm, Controller, useFieldArray, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { Plus, RefreshCw, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError, fetchAllPages } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type {
  PagoProveedor,
  Proveedor,
  Insumo,
  MetodoPago,
  EstadoPago,
  CreatePagoProveedorCommand,
  RegistrarPagoProveedorCommand,
} from "@/lib/types";
import { METODO_PAGO_LABELS } from "@/lib/types";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import LineasInsumosEditor from "@/components/shared/LineasInsumosEditor";
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

const insumoLineSchema = z.object({
  id: z.string(),
  insumoId: z.string().min(1, "Seleccioná un insumo."),
  cantidad: z.coerce
    .number({ message: "Ingresá un número válido." })
    .positive("Debe ser mayor a 0."),
  precioUnitario: z.coerce
    .number({ message: "Ingresá un número válido." })
    .min(0, "No puede ser negativo."),
});

const facturaSchema = z.object({
  proveedorId: z.string().min(1, "Seleccioná un proveedor."),
  fechaPago: z.string().min(1, "La fecha de pago es obligatoria."),
  observaciones: z.string().max(1000, "Máximo 1000 caracteres."),
  insumos: z.array(insumoLineSchema),
});

type FacturaFormInput = z.input<typeof facturaSchema>;
type FacturaFormValues = z.output<typeof facturaSchema>;

const emptyInsumo = () => ({
  id: "",
  insumoId: "",
  cantidad: "",
  precioUnitario: "",
});

const pagoSchema = z.object({
  fecha: z.string().min(1, "La fecha del pago es obligatoria."),
  observaciones: z.string().max(500, "Máximo 500 caracteres."),
  medios: z
    .array(
      z.object({
        id: z.string(),
        tipo: z.coerce
          .number({ message: "Seleccioná un medio de pago." })
          .refine((v) => [1, 2, 3, 4, 5].includes(v), "Seleccioná un medio de pago."),
        monto: z.coerce
          .number({ message: "Ingresá un número válido." })
          .positive("Debe ser mayor a 0."),
        referencia: z.string().max(100, "Máximo 100 caracteres."),
      }),
    )
    .min(1, "Agregá al menos un medio de pago."),
});

type PagoFormInput = z.input<typeof pagoSchema>;
type PagoFormValues = z.output<typeof pagoSchema>;

const emptyMedio = () => ({
  id: crypto.randomUUID(),
  tipo: "",
  monto: "",
  referencia: "",
});

const METODO_PAGO_OPTIONS: { value: string; label: string }[] = (
  [1, 2, 3, 4, 5] as MetodoPago[]
).map((v) => ({ value: String(v), label: METODO_PAGO_LABELS[v] }));

function estadoPagoBadgeClass(estado: EstadoPago) {
  switch (estado) {
    case "Pagada":
      return "border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400";
    case "Pendiente":
      return "border-muted-foreground/30 bg-muted/50 text-muted-foreground";
    default:
      return "";
  }
}

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function PagosPage() {
  const [rows, setRows] = useState<PagoProveedor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [proveedores, setProveedores] = useState<Proveedor[]>([]);
  const [insumos, setInsumos] = useState<Insumo[]>([]);

  // Filters
  const [filtroProveedor, setFiltroProveedor] = useState("all");
  const [filtroDesde, setFiltroDesde] = useState("");
  const [filtroHasta, setFiltroHasta] = useState("");

  const [dialogOpen, setDialogOpen] = useState(false);

  const [detail, setDetail] = useState<PagoProveedor | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const [pagoTarget, setPagoTarget] = useState<PagoProveedor | null>(null);
  const [dialogPagoOpen, setDialogPagoOpen] = useState(false);

  const pagoForm = useForm<PagoFormInput, unknown, PagoFormValues>({
    resolver: zodResolver(pagoSchema),
    defaultValues: {
      fecha: new Date().toISOString().slice(0, 10),
      observaciones: "",
      medios: [],
    },
  });
  const mediosArray = useFieldArray({ control: pagoForm.control, name: "medios" });

  const openPago = (row: PagoProveedor) => {
    setPagoTarget(row);
    pagoForm.reset({
      fecha: new Date().toISOString().slice(0, 10),
      observaciones: "",
      medios: [emptyMedio()],
    });
    setDialogPagoOpen(true);
  };

  const watchedMedios = useWatch({ control: pagoForm.control, name: "medios" }) ?? [];
  const mediosTotal = watchedMedios.reduce(
    (s, m) => s + (Number(m?.monto) || 0),
    0,
  );

  const handlePagoSave = pagoForm.handleSubmit(async (values) => {
    if (!pagoTarget) return;
    const payload: RegistrarPagoProveedorCommand = {
      proveedorId: pagoTarget.proveedorId,
      fecha: values.fecha,
      facturaId: pagoTarget.id,
      observaciones: values.observaciones.trim() || null,
      medios: values.medios.map((m) => ({
        tipo: m.tipo as MetodoPago,
        monto: m.monto,
        referencia: m.referencia.trim() || null,
      })),
    };
    try {
      await apiClient<unknown>(`/proveedores/${pagoTarget.proveedorId}/pagos`, {
        method: "POST",
        body: payload,
      });
      toast.success("Pago registrado.");
      setDialogPagoOpen(false);
      setPagoTarget(null);
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "No se pudo registrar el pago.",
      );
    }
  });

  const buildQuery = useCallback(() => {
    const params = new URLSearchParams();
    if (filtroProveedor && filtroProveedor !== "all") params.set("proveedorId", filtroProveedor);
    if (filtroDesde) params.set("fechaDesde", filtroDesde);
    if (filtroHasta) params.set("fechaHasta", filtroHasta);
    const qs = params.toString();
    return `/pagos-proveedor${qs ? `?${qs}` : ""}`;
  }, [filtroProveedor, filtroDesde, filtroHasta]);

  const load = useCallback(async () => {
    try {
      const result = await apiClient<PagoProveedor[]>(buildQuery());
      setRows(result);
      setError(null);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "No se pudieron cargar las facturas.",
      );
    } finally {
      setLoading(false);
    }
  }, [buildQuery]);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      try {
        const [facturas, prov, ins] = await Promise.all([
          apiClient<PagoProveedor[]>(buildQuery()),
          apiClient<Proveedor[]>("/proveedores"),
          fetchAllPages<Insumo>("/insumos?pageSize=100"),
        ]);
        if (cancelled) return;
        setRows(facturas);
        setProveedores(prov);
        setInsumos(ins);
        setError(null);
      } catch (err) {
        if (cancelled) return;
        setError(
          err instanceof ApiError ? err.message : "No se pudieron cargar las facturas.",
        );
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, [buildQuery]);

  const form = useForm<FacturaFormInput, unknown, FacturaFormValues>({
    resolver: zodResolver(facturaSchema),
    defaultValues: {
      proveedorId: "",
      fechaPago: new Date().toISOString().slice(0, 10),
      observaciones: "",
      insumos: [],
    },
  });

  const insumosArray = useFieldArray({ control: form.control, name: "insumos" });

  const openCreate = () => {
    form.reset({
      proveedorId: "",
      fechaPago: new Date().toISOString().slice(0, 10),
      observaciones: "",
      insumos: [],
    });
    setDialogOpen(true);
  };

  const handleSave = form.handleSubmit(async (values) => {
    const montoTotal = values.insumos.reduce((s, i) => s + i.cantidad * i.precioUnitario, 0);
    const payload: CreatePagoProveedorCommand = {
      proveedorId: values.proveedorId,
      fechaPago: values.fechaPago,
      montoTotal,
      observaciones: values.observaciones.trim() || null,
      insumos: values.insumos.map((i) => ({
        insumoId: i.insumoId,
        cantidad: i.cantidad,
        precioUnitario: i.precioUnitario,
      })),
    };
    try {
      await apiClient<unknown>("/pagos-proveedor", { method: "POST", body: payload });
      toast.success("Factura de compra creada.");
      setDialogOpen(false);
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "No se pudo crear la factura de compra.",
      );
    }
  });

  const openDetail = async (row: PagoProveedor) => {
    setDetail(row);
    setDetailLoading(true);
    try {
      const det = await apiClient<PagoProveedor>(`/pagos-proveedor/${row.id}`);
      setDetail(det);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo cargar el detalle.");
    } finally {
      setDetailLoading(false);
    }
  };

  const columns: ColumnDef<PagoProveedor, unknown>[] = [
    { accessorKey: "numero", header: "N°" },
    {
      id: "proveedor",
      header: "Proveedor",
      cell: ({ row }) => row.original.proveedorNombre || "—",
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
    {
      accessorKey: "montoPagado",
      header: "Pagado",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
    {
      accessorKey: "estadoPago",
      header: "Estado pago",
      cell: ({ row }) => (
        <Badge variant="outline" className={estadoPagoBadgeClass(row.original.estadoPago)}>
          {row.original.estadoPago}
        </Badge>
      ),
    },
  ];

  const {
    register,
    setValue,
    control,
    formState: { errors, isSubmitting },
  } = form;

  const watchedInsumos = useWatch({ control, name: "insumos" }) ?? [];

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nueva factura" title="Nueva factura">
              <Plus className="size-5" />
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>          </>
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Select value={filtroProveedor} onValueChange={setFiltroProveedor}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Todos los proveedores" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los proveedores</SelectItem>
            {proveedores.map((p) => (
              <SelectItem key={p.id} value={p.id}>
                {p.nombreRazonSocial}
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
        columns={columns}
        data={rows}
        loading={loading}
        error={error}
        emptyMessage="No hay facturas de compra."
        actions={(row) => (
          <>
            <Button variant="outline" size="sm" onClick={() => void openDetail(row)}>
              Ver
            </Button>
            {row.estadoPago !== "Pagada" && (
              <Button variant="outline" size="sm" onClick={() => openPago(row)}>
                Pagar
              </Button>
            )}
          </>
        )}
      />

      <Dialog open={dialogPagoOpen} onOpenChange={setDialogPagoOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>Registrar pago — Factura N° {pagoTarget?.numero}</DialogTitle>
            <DialogDescription>
              El pago se registra en cuenta corriente. Permite pagos parciales; no puede
              exceder el pendiente.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handlePagoSave} className="flex flex-col gap-3" noValidate>
            <div className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="pago-fecha">Fecha del pago</Label>
                <Input id="pago-fecha" type="date" {...pagoForm.register("fecha")} />
                <FieldError message={pagoForm.formState.errors.fecha?.message} />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label>Pendiente</Label>
                <Input
                  readOnly
                  value={MONEY.format(pagoTarget?.montoPendiente ?? 0)}
                  className="bg-muted"
                />
              </div>
            </div>

            <div className="flex flex-col gap-2">
              <Label>Medios de pago</Label>
              {mediosArray.fields.length > 0 && (
                <div className="flex flex-col gap-2">
                  {mediosArray.fields.map((field, idx) => {
                    const medioError = pagoForm.formState.errors.medios?.[idx];
                    return (
                      <div key={field.id} className="grid grid-cols-[130px_1fr_1fr_36px] items-start gap-2">
                        <div className="flex flex-col gap-1">
                          <Controller
                            control={pagoForm.control}
                            name={`medios.${idx}.tipo`}
                            render={({ field: f }) => (
                              <Select
                                value={f.value ? String(f.value) : undefined}
                                onValueChange={(v) => f.onChange(Number(v))}
                              >
                                <SelectTrigger aria-label="Medio de pago">
                                  <SelectValue placeholder="Medio…" />
                                </SelectTrigger>
                                <SelectContent>
                                  {METODO_PAGO_OPTIONS.map((o) => (
                                    <SelectItem key={o.value} value={o.value}>
                                      {o.label}
                                    </SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                            )}
                          />
                          <FieldError message={medioError?.tipo?.message} />
                        </div>
                        <div className="flex flex-col gap-1">
                          <Input
                            type="number"
                            min="0"
                            step="0.01"
                            placeholder="Monto"
                            {...pagoForm.register(`medios.${idx}.monto`)}
                          />
                          <FieldError message={medioError?.monto?.message} />
                        </div>
                        <div className="flex flex-col gap-1">
                          <Input
                            placeholder="Referencia (opcional)"
                            {...pagoForm.register(`medios.${idx}.referencia`)}
                          />
                          <FieldError message={medioError?.referencia?.message} />
                        </div>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => mediosArray.remove(idx)}
                          disabled={mediosArray.fields.length <= 1}
                          aria-label="Quitar medio de pago"
                        >
                          <Trash2 className="size-4" />
                        </Button>
                      </div>
                    );
                  })}
                </div>
              )}
              <FieldError
                message={
                  pagoForm.formState.errors.medios?.root?.message ??
                  pagoForm.formState.errors.medios?.message
                }
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="w-fit"
                onClick={() => mediosArray.append(emptyMedio())}
              >
                <Plus className="size-4" />
                Agregar medio
              </Button>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="pago-observaciones">Observaciones</Label>
              <Input id="pago-observaciones" {...pagoForm.register("observaciones")} />
              <FieldError message={pagoForm.formState.errors.observaciones?.message} />
            </div>

            <p className="text-sm font-medium">
              Total del pago: {MONEY.format(mediosTotal)}
            </p>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setDialogPagoOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={pagoForm.formState.isSubmitting}>
                {pagoForm.formState.isSubmitting ? "Registrando…" : "Registrar pago"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>Nueva Factura de Compra</DialogTitle>
            <DialogDescription>
              El total de la factura se calcula con la suma de los insumos. La factura suma
              stock y genera deuda en cuenta corriente.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="factura-proveedor">Proveedor</Label>
              <Controller
                control={control}
                name="proveedorId"
                render={({ field }) => (
                  <Select value={field.value || undefined} onValueChange={field.onChange}>
                    <SelectTrigger id="factura-proveedor" className="w-full">
                      <SelectValue placeholder="Seleccionar…" />
                    </SelectTrigger>
                    <SelectContent>
                      {proveedores.map((p) => (
                        <SelectItem key={p.id} value={p.id}>
                          {p.nombreRazonSocial}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              <FieldError message={errors.proveedorId?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="factura-fecha">Fecha de pago</Label>
              <Input id="factura-fecha" type="date" {...register("fechaPago")} />
              <FieldError message={errors.fechaPago?.message} />
            </div>

            <div className="flex flex-col gap-2 sm:col-span-2">
              <LineasInsumosEditor
                insumos={insumos}
                lines={insumosArray.fields.map((field, idx) => ({
                  key: field.id,
                  insumoId: String(watchedInsumos[idx]?.insumoId ?? ""),
                  cantidad: String(watchedInsumos[idx]?.cantidad ?? ""),
                  precioUnitario: String(watchedInsumos[idx]?.precioUnitario ?? ""),
                }))}
                onInsumoChange={(index, id) =>
                  setValue(`insumos.${index}.insumoId`, id, { shouldValidate: true })
                }
                onCantidadChange={(index, v) => setValue(`insumos.${index}.cantidad`, v)}
                onPrecioChange={(index, v) => setValue(`insumos.${index}.precioUnitario`, v)}
                onAdd={() => insumosArray.append(emptyInsumo())}
                onRemove={(index) => insumosArray.remove(index)}
                fieldErrors={insumosArray.fields.map((_, idx) => ({
                  insumoId: errors.insumos?.[idx]?.insumoId?.message,
                  cantidad: errors.insumos?.[idx]?.cantidad?.message,
                  precioUnitario: errors.insumos?.[idx]?.precioUnitario?.message,
                }))}
                rootError={errors.insumos?.root?.message ?? errors.insumos?.message}
              />
            </div>

            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label htmlFor="factura-observaciones">Observaciones</Label>
              <Input id="factura-observaciones" {...register("observaciones")} />
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
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Factura N° {detail?.numero}</DialogTitle>
            <DialogDescription>Insumos de la factura.</DialogDescription>
          </DialogHeader>

          {detailLoading ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Cargando detalle…</p>
          ) : detail ? (
            <div className="flex flex-col gap-4">
              <div className="grid grid-cols-1 gap-x-4 gap-y-2 text-sm sm:grid-cols-2">
                <div>
                  <span className="font-medium">Proveedor:</span> {detail.proveedorNombre || "—"}
                </div>
                <div>
                  <span className="font-medium">Fecha:</span>{" "}
                  {new Date(detail.fechaPago).toLocaleDateString("es-AR")}
                </div>
                <div>
                  <span className="font-medium">Pagado:</span>{" "}
                  {MONEY.format(detail.montoPagado)}
                </div>
                <div>
                  <span className="font-medium">Pendiente:</span>{" "}
                  {MONEY.format(detail.montoPendiente)}
                </div>
                {detail.observaciones && (
                  <div className="sm:col-span-2">
                    <span className="font-medium">Observaciones:</span> {detail.observaciones}
                  </div>
                )}
              </div>

              <div>
                <Table>
                  <TableHeader>
                    <TableRow className="hover:bg-transparent">
                      <TableHead>Insumo</TableHead>
                      <TableHead>Cantidad</TableHead>
                      <TableHead className="text-left">Precio unitario</TableHead>
                      <TableHead className="text-left">Subtotal</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {detail.insumos.map((i, idx) => (
                      <TableRow key={`${i.insumoId}-${idx}`}>
                        <TableCell>{i.insumoNombre || "—"}</TableCell>
                        <TableCell>{i.cantidad}</TableCell>
                        <TableCell className="text-left">
                          {MONEY.format(i.precioUnitario)}
                        </TableCell>
                        <TableCell className="text-left">{MONEY.format(i.subtotal)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
                <p className="mt-2 text-right text-base font-semibold">
                  Total: {MONEY.format(detail.montoTotal)}
                </p>
              </div>
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
