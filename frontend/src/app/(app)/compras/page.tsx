"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm, Controller, useFieldArray, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { Plus, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError, fetchAllPages } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type {
  OrdenCompra,
  OrdenCompraItem,
  Proveedor,
  Insumo,
  EstadoOrdenCompra,
  CreateOrdenCompraCommand,
  UpdateOrdenCompraCommand,
} from "@/lib/types";
import { ESTADO_ORDEN_COMPRA_LABELS } from "@/lib/types";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import ConfirmDialog from "@/components/shared/ConfirmDialog";
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

const itemSchema = z.object({
  id: z.string(),
  insumoId: z.string().min(1, "Seleccioná un insumo."),
  cantidadPedida: z.coerce
    .number({ message: "Ingresá un número válido." })
    .positive("Debe ser mayor a 0."),
  precioUnitario: z.coerce
    .number({ message: "Ingresá un número válido." })
    .min(0, "No puede ser negativo."),
});

const compraSchema = z.object({
  proveedorId: z.string().min(1, "Seleccioná un proveedor."),
  observaciones: z.string().max(1000, "Máximo 1000 caracteres."),
  items: z.array(itemSchema),
});

type CompraFormInput = z.input<typeof compraSchema>;
type CompraFormValues = z.output<typeof compraSchema>;

const EMPTY_FORM: CompraFormInput = {
  proveedorId: "",
  observaciones: "",
  items: [],
};

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

function estadoBadgeClass(estado: EstadoOrdenCompra) {
  if (estado === 2)
    return "border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400";
  if (estado === 6)
    return "border-red-600/30 bg-red-500/10 text-red-700 dark:text-red-400";
  return undefined;
}

export default function ComprasPage() {
  const [rows, setRows] = useState<OrdenCompra[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [proveedores, setProveedores] = useState<Proveedor[]>([]);
  const [insumos, setInsumos] = useState<Insumo[]>([]);

  // Filters
  const [filtroProveedor, setFiltroProveedor] = useState("all");
  const [filtroEstado, setFiltroEstado] = useState("all");
  const [filtroDesde, setFiltroDesde] = useState("");
  const [filtroHasta, setFiltroHasta] = useState("");

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<OrdenCompra | null>(null);

  const [detail, setDetail] = useState<OrdenCompra | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const [enviando, setEnviando] = useState<OrdenCompra | null>(null);
  const [enviandoBusy, setEnviandoBusy] = useState(false);
  const [cancelando, setCancelando] = useState<OrdenCompra | null>(null);
  const [cancelandoBusy, setCancelandoBusy] = useState(false);

  const buildQuery = useCallback(() => {
    const params = new URLSearchParams();
    if (filtroProveedor && filtroProveedor !== "all") params.set("proveedorId", filtroProveedor);
    if (filtroEstado && filtroEstado !== "all") params.set("estado", filtroEstado);
    if (filtroDesde) params.set("fechaDesde", filtroDesde);
    if (filtroHasta) params.set("fechaHasta", filtroHasta);
    const qs = params.toString();
    return `/ordenes-compra${qs ? `?${qs}` : ""}`;
  }, [filtroProveedor, filtroEstado, filtroDesde, filtroHasta]);

  const load = useCallback(async () => {
    try {
      const result = await apiClient<OrdenCompra[]>(buildQuery());
      setRows(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar las órdenes de compra.");
    } finally {
      setLoading(false);
    }
  }, [buildQuery]);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      try {
        const [ocs, prov, ins] = await Promise.all([
          apiClient<OrdenCompra[]>(buildQuery()),
          apiClient<Proveedor[]>("/proveedores"),
          fetchAllPages<Insumo>("/insumos"),
        ]);
        if (cancelled) return;
        setRows(ocs);
        setProveedores(prov);
        setInsumos(ins);
        setError(null);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof ApiError ? err.message : "No se pudieron cargar las órdenes de compra.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, [buildQuery]);

  const form = useForm<CompraFormInput, unknown, CompraFormValues>({
    resolver: zodResolver(compraSchema),
    defaultValues: EMPTY_FORM,
  });

  const { fields, append, remove } = useFieldArray({ control: form.control, name: "items" });

  const openCreate = () => {
    setEditing(null);
    form.reset(EMPTY_FORM);
    setDialogOpen(true);
  };

  const openEdit = (row: OrdenCompra) => {
    setEditing(row);
    form.reset({
      proveedorId: row.proveedorId,
      observaciones: row.observaciones ?? "",
      items: row.items.map((it) => ({
        id: `item-${it.id}`,
        insumoId: it.insumoId,
        cantidadPedida: String(it.cantidadPedida),
        precioUnitario: String(it.precioUnitario),
      })),
    });
    setDialogOpen(true);
  };

  const handleSave = form.handleSubmit(async (values) => {
    if (editing && !editing.rowVersion) {
      toast.error(
        "No se pudo obtener la versión del registro. Recargá la página e intentá de nuevo.",
      );
      return;
    }
    const items = values.items.map((it) => ({
      insumoId: it.insumoId,
      cantidadPedida: it.cantidadPedida,
      precioUnitario: it.precioUnitario,
    }));
    const base = {
      proveedorId: values.proveedorId,
      observaciones: values.observaciones.trim() || null,
      items,
    };
    try {
      if (editing) {
        const payload: UpdateOrdenCompraCommand = {
          ...base,
          id: editing.id,
          rowVersion: editing.rowVersion,
        };
        await apiClient<unknown>(`/ordenes-compra/${editing.id}`, { method: "PUT", body: payload });
        toast.success(`Orden N° ${editing.numero} actualizada.`);
      } else {
        await apiClient<unknown>("/ordenes-compra", {
          method: "POST",
          body: base as CreateOrdenCompraCommand,
        });
        toast.success("Orden de compra creada.");
      }
      setDialogOpen(false);
      await load();
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        toast.error(
          `${err.message} El registro fue modificado por otro usuario. Recargá la lista para ver la versión más reciente y volvé a intentar.`,
        );
      } else {
        toast.error(err instanceof ApiError ? err.message : "No se pudo guardar la orden de compra.");
      }
    }
  });

  const openDetail = async (row: OrdenCompra) => {
    setDetail(row);
    setDetailLoading(true);
    try {
      const det = await apiClient<OrdenCompra>(`/ordenes-compra/${row.id}`);
      setDetail(det);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo cargar el detalle.");
    } finally {
      setDetailLoading(false);
    }
  };

  const handleEnviar = async () => {
    if (!enviando) return;
    setEnviandoBusy(true);
    try {
      await apiClient<unknown>(`/ordenes-compra/${enviando.id}/enviar`, {
        method: "POST",
        body: { ordenCompraId: enviando.id },
      });
      setEnviando(null);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo enviar la orden.");
      setEnviando(null);
    } finally {
      setEnviandoBusy(false);
    }
  };

  const handleCancelar = async () => {
    if (!cancelando) return;
    setCancelandoBusy(true);
    try {
      await apiClient<unknown>(`/ordenes-compra/${cancelando.id}/cancelar`, {
        method: "POST",
        body: { ordenCompraId: cancelando.id },
      });
      setCancelando(null);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo cancelar la orden.");
      setCancelando(null);
    } finally {
      setCancelandoBusy(false);
    }
  };

  const canCancelar = (r: OrdenCompra) => r.estado === 1 || r.estado === 2;

  const columns: ColumnDef<OrdenCompra, unknown>[] = [
    { accessorKey: "numero", header: "N°" },
    {
      id: "proveedor",
      header: "Proveedor",
      cell: ({ row }) => row.original.proveedorNombre || "—",
    },
    {
      id: "fecha",
      header: "Fecha",
      cell: ({ row }) => new Date(row.original.fechaCreacion).toLocaleDateString("es-AR"),
    },
    {
      accessorKey: "total",
      header: "Total",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
    {
      accessorKey: "estado",
      header: "Estado",
      cell: ({ row }) => (
        <Badge variant="outline" className={estadoBadgeClass(row.original.estado)}>
          {ESTADO_ORDEN_COMPRA_LABELS[row.original.estado] ?? String(row.original.estado)}
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

  const watchedItems = useWatch({ control, name: "items" });

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nueva orden" title="Nueva orden">
              <Plus className="size-5" />
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>          </>
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Select
          value={filtroProveedor}
          onValueChange={setFiltroProveedor}
        >
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
        <Select value={filtroEstado} onValueChange={setFiltroEstado}>
          <SelectTrigger className="w-[150px]">
            <SelectValue placeholder="Todos los estados" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los estados</SelectItem>
            {(Object.keys(ESTADO_ORDEN_COMPRA_LABELS) as unknown as string[]).map((v) => (
              <SelectItem key={v} value={v}>
                {ESTADO_ORDEN_COMPRA_LABELS[Number(v) as EstadoOrdenCompra]}
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
        emptyMessage="No hay órdenes de compra."
        actions={(row) => (
          <>
            <Button variant="outline" size="sm" onClick={() => void openDetail(row)}>
              Ver
            </Button>
            {row.estado === 1 && (
              <Button variant="outline" size="sm" onClick={() => openEdit(row)}>
                Editar
              </Button>
            )}
            {row.estado === 1 && (
              <Button size="sm" onClick={() => setEnviando(row)}>
                Enviar
              </Button>
            )}
            {canCancelar(row) && (
              <Button variant="destructive" size="sm" onClick={() => setCancelando(row)}>
                Cancelar
              </Button>
            )}
          </>
        )}
      />

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>{editing ? `Editar orden N° ${editing.numero}` : "Nueva orden de compra"}</DialogTitle>
            <DialogDescription>
              {editing
                ? "Modificá los datos de la orden y guardá los cambios."
                : "Completá los datos para crear una nueva orden de compra."}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="compra-proveedor">Proveedor</Label>
              <Controller
                control={control}
                name="proveedorId"
                render={({ field }) => (
                  <Select value={field.value || undefined} onValueChange={field.onChange}>
                    <SelectTrigger id="compra-proveedor" className="w-full">
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

            <div className="flex flex-col gap-2 sm:col-span-2">
              <LineasInsumosEditor
                insumos={insumos}
                lines={fields.map((field, idx) => ({
                  key: field.id,
                  insumoId: String(watchedItems?.[idx]?.insumoId ?? ""),
                  cantidad: String(watchedItems?.[idx]?.cantidadPedida ?? ""),
                  precioUnitario: String(watchedItems?.[idx]?.precioUnitario ?? ""),
                }))}
                onInsumoChange={(index, id) =>
                  setValue(`items.${index}.insumoId`, id, { shouldValidate: true })
                }
                onCantidadChange={(index, v) => setValue(`items.${index}.cantidadPedida`, v)}
                onPrecioChange={(index, v) => setValue(`items.${index}.precioUnitario`, v)}
                onAdd={() =>
                  append({ id: "", insumoId: "", cantidadPedida: "1", precioUnitario: "" })
                }
                onRemove={(index) => remove(index)}
                fieldErrors={fields.map((_, idx) => ({
                  insumoId: errors.items?.[idx]?.insumoId?.message,
                  cantidad: errors.items?.[idx]?.cantidadPedida?.message,
                  precioUnitario: errors.items?.[idx]?.precioUnitario?.message,
                }))}
                rootError={errors.items?.root?.message ?? errors.items?.message}
                addLabel="Agregar ítem"
              />
            </div>

            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label htmlFor="compra-observaciones">Observaciones</Label>
              <Input id="compra-observaciones" {...register("observaciones")} />
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
            <DialogTitle>Orden de compra N° {detail?.numero}</DialogTitle>
            <DialogDescription>Detalle de la orden y sus ítems.</DialogDescription>
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
                  <span className="font-medium">Estado:</span>{" "}
                  {ESTADO_ORDEN_COMPRA_LABELS[detail.estado] ?? detail.estado}
                </div>
                <div>
                  <span className="font-medium">Fecha:</span>{" "}
                  {new Date(detail.fechaCreacion).toLocaleDateString("es-AR")}
                </div>
                {detail.fechaEnvio && (
                  <div>
                    <span className="font-medium">Enviada:</span>{" "}
                    {new Date(detail.fechaEnvio).toLocaleDateString("es-AR")}
                  </div>
                )}
                {detail.observaciones && (
                  <div className="sm:col-span-2">
                    <span className="font-medium">Observaciones:</span> {detail.observaciones}
                  </div>
                )}
              </div>

              <Table>
                <TableHeader>
                  <TableRow className="hover:bg-transparent">
                    <TableHead>Insumo</TableHead>
                    <TableHead className="text-right">Cantidad</TableHead>
                    <TableHead className="text-left">Precio unit.</TableHead>
                    <TableHead className="text-left">Subtotal</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detail.items.map((it: OrdenCompraItem) => (
                    <TableRow key={it.id}>
                      <TableCell>{it.insumoNombre || "—"}</TableCell>
                      <TableCell className="text-right">{it.cantidadPedida}</TableCell>
                      <TableCell className="text-left">{MONEY.format(it.precioUnitario)}</TableCell>
                      <TableCell className="text-left">{MONEY.format(it.subtotal)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <p className="text-right text-base font-semibold">Total: {MONEY.format(detail.total)}</p>
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
        open={enviando !== null}
        onOpenChange={(open) => {
          if (!open) setEnviando(null);
        }}
        title="Enviar orden de compra"
        message={`¿Enviar la orden N° ${enviando?.numero ?? ""} al proveedor?`}
        confirmLabel="Enviar"
        busy={enviandoBusy}
        onConfirm={() => void handleEnviar()}
      />

      <ConfirmDialog
        open={cancelando !== null}
        onOpenChange={(open) => {
          if (!open) setCancelando(null);
        }}
        title="Cancelar orden de compra"
        message={`¿Seguro que querés cancelar la orden N° ${cancelando?.numero ?? ""}?`}
        confirmLabel="Cancelar orden"
        destructive
        busy={cancelandoBusy}
        onConfirm={() => void handleCancelar()}
      />
    </div>
  );
}
