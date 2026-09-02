"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
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
  CreatePagoProveedorCommand,
} from "@/lib/types";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { CurrencyInput } from "@/components/ui/currency-input";
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
  ];

  const {
    register,
    watch,
    setValue,
    control,
    formState: { errors, isSubmitting },
  } = form;

  const watchedInsumos = useWatch({ control, name: "insumos" }) ?? [];
  const sumInsumos = watchedInsumos.reduce(
    (s, i) =>
      s + (parseFloat(String(i?.cantidad)) || 0) * (parseFloat(String(i?.precioUnitario)) || 0),
    0,
  );

  const insumoById = useMemo(() => new Map(insumos.map((i) => [i.id, i])), [insumos]);

  return (
    <div>
      <PageHeader
        title="Facturas y Pagos"
        description="Facturas de compra a proveedores: suman stock de insumos y generan deuda en cuenta corriente."
        actions={
          <>
            <Button size="sm" onClick={openCreate}>
              <Plus className="size-4" />
              Nueva factura
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-4 ${loading ? "animate-spin" : ""}`} />
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
        emptyMessage="No hay facturas de compra."
        actions={(row) => (
          <Button variant="outline" size="sm" onClick={() => void openDetail(row)}>
            Ver
          </Button>
        )}
      />

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

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="factura-observaciones">Observaciones</Label>
              <Input id="factura-observaciones" {...register("observaciones")} />
              <FieldError message={errors.observaciones?.message} />
            </div>

            <div className="flex flex-col gap-2 sm:col-span-2">
              <Label>Insumos</Label>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-[40%]">Insumo</TableHead>
                    <TableHead>Cantidad</TableHead>
                    <TableHead>Precio unitario</TableHead>
                    <TableHead className="text-right">Subtotal</TableHead>
                    <TableHead className="w-10" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {insumosArray.fields.map((field, index) => {
                    const insumo = insumoById.get(
                      String(watchedInsumos[index]?.insumoId ?? ""),
                    );
                    const subtotal =
                      (parseFloat(String(watchedInsumos[index]?.cantidad)) || 0) *
                      (parseFloat(String(watchedInsumos[index]?.precioUnitario)) || 0);
                    return (
                      <TableRow key={field.id}>
                        <TableCell>
                          <Controller
                            control={control}
                            name={`insumos.${index}.insumoId`}
                            render={({ field: f }) => (
                              <Select value={f.value || undefined} onValueChange={f.onChange}>
                                <SelectTrigger className="w-full">
                                  <SelectValue placeholder="Seleccionar insumo…" />
                                </SelectTrigger>
                                <SelectContent>
                                  {insumos.map((i) => (
                                    <SelectItem key={i.id} value={i.id}>
                                      {i.nombre} ({i.unidadCompra?.simbolo ?? "u"})
                                    </SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                            )}
                          />
                          <FieldError
                            message={errors.insumos?.[index]?.insumoId?.message}
                          />
                        </TableCell>
                        <TableCell>
                          <Input
                            type="number"
                            step="any"
                            min="0"
                            className="w-28"
                            placeholder={insumo ? `en ${insumo.unidadCompra?.simbolo ?? "compra"}` : "Cantidad"}
                            {...register(`insumos.${index}.cantidad`)}
                          />
                          <FieldError
                            message={errors.insumos?.[index]?.cantidad?.message}
                          />
                        </TableCell>
                        <TableCell>
                          <CurrencyInput
                            value={String(watch(`insumos.${index}.precioUnitario`) ?? "")}
                            onChange={(v) =>
                              setValue(`insumos.${index}.precioUnitario`, v)
                            }
                          />
                          <FieldError
                            message={errors.insumos?.[index]?.precioUnitario?.message}
                          />
                        </TableCell>
                        <TableCell className="text-right align-top">
                          {MONEY.format(subtotal)}
                        </TableCell>
                        <TableCell className="align-top">
                          <Button
                            type="button"
                            variant="destructive"
                            size="icon"
                            onClick={() => insumosArray.remove(index)}
                            aria-label="Eliminar insumo"
                          >
                            <Trash2 className="size-4" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
              <div className="flex items-center justify-between rounded-md border bg-muted/50 px-3 py-2">
                <span className="text-sm font-medium">Total de la factura</span>
                <span className="text-sm font-semibold">{MONEY.format(sumInsumos)}</span>
              </div>
              <div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => insumosArray.append(emptyInsumo())}
                >
                  <Plus className="size-4" />
                  Agregar insumo
                </Button>
              </div>
              <FieldError message={errors.insumos?.root?.message ?? errors.insumos?.message} />
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
                  <span className="font-medium">Monto total:</span> {MONEY.format(detail.montoTotal)}
                </div>
                {detail.observaciones && (
                  <div className="sm:col-span-2">
                    <span className="font-medium">Observaciones:</span> {detail.observaciones}
                  </div>
                )}
              </div>

              <div>
                <Badge variant="outline" className="mb-2">
                  Insumos
                </Badge>
                <Table>
                  <TableHeader>
                    <TableRow>
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
