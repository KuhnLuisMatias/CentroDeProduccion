"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm, Controller, useFieldArray } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { Plus, RefreshCw, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type {
  Devolucion,
  DevolucionListItem,
  RemitoListItem,
  BarListItem,
  ProductoTerminado,
  CreateDevolucionCommand,
} from "@/lib/types";
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

const lineaSchema = z.object({
  id: z.string(),
  productoTerminadoId: z.string().min(1, "Seleccioná un producto terminado."),
  cantidad: z.coerce
    .number({ message: "Ingresá un número válido." })
    .positive("Debe ser mayor a 0."),
  lote: z.string().max(50, "Máximo 50 caracteres."),
});

const devolucionSchema = z.object({
  remitoId: z.string().min(1, "Seleccioná un remito."),
  observaciones: z.string().max(500, "Máximo 500 caracteres."),
  recibidoPor: z.string().max(200, "Máximo 200 caracteres."),
  lineas: z.array(lineaSchema).min(1, "Agregá al menos una línea."),
});

type DevolucionFormInput = z.input<typeof devolucionSchema>;
type DevolucionFormValues = z.output<typeof devolucionSchema>;

const EMPTY_LINE: DevolucionFormInput["lineas"][number] = {
  id: "",
  productoTerminadoId: "",
  cantidad: "1",
  lote: "",
};

const EMPTY_FORM: DevolucionFormInput = {
  remitoId: "",
  observaciones: "",
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

export default function DevolucionesPage() {
  const [rows, setRows] = useState<DevolucionListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [remitos, setRemitos] = useState<RemitoListItem[]>([]);
  const [bares, setBares] = useState<BarListItem[]>([]);
  const [productos, setProductos] = useState<ProductoTerminado[]>([]);

  // Filters
  const [filtroRemito, setFiltroRemito] = useState("all");
  const [filtroBar, setFiltroBar] = useState("all");
  const [filtroDesde, setFiltroDesde] = useState("");
  const [filtroHasta, setFiltroHasta] = useState("");

  const [dialogOpen, setDialogOpen] = useState(false);

  const [detail, setDetail] = useState<Devolucion | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const buildQuery = useCallback(() => {
    const params = new URLSearchParams();
    if (filtroRemito && filtroRemito !== "all") params.set("remitoId", filtroRemito);
    if (filtroBar && filtroBar !== "all") params.set("barId", filtroBar);
    if (filtroDesde) params.set("fechaDesde", filtroDesde);
    if (filtroHasta) params.set("fechaHasta", filtroHasta);
    const qs = params.toString();
    return `/devoluciones${qs ? `?${qs}` : ""}`;
  }, [filtroRemito, filtroBar, filtroDesde, filtroHasta]);

  const load = useCallback(async () => {
    try {
      const result = await apiClient<DevolucionListItem[]>(buildQuery());
      setRows(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar las devoluciones.");
    } finally {
      setLoading(false);
    }
  }, [buildQuery]);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      try {
        const [devoluciones, remitoList, barList, prodList] = await Promise.all([
          apiClient<DevolucionListItem[]>(buildQuery()),
          apiClient<RemitoListItem[]>("/remitos"),
          apiClient<BarListItem[]>("/bares"),
          apiClient<ProductoTerminado[]>("/productoterminado"),
        ]);
        if (cancelled) return;
        setRows(devoluciones);
        setRemitos(remitoList);
        setBares(barList);
        setProductos(prodList);
        setError(null);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof ApiError ? err.message : "No se pudieron cargar las devoluciones.");
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

  const form = useForm<DevolucionFormInput, unknown, DevolucionFormValues>({
    resolver: zodResolver(devolucionSchema),
    defaultValues: EMPTY_FORM,
  });

  const { fields, append, remove } = useFieldArray({ control: form.control, name: "lineas" });

  const openCreate = () => {
    form.reset({ ...EMPTY_FORM, lineas: [{ ...EMPTY_LINE, id: `line-${Date.now()}` }] });
    setDialogOpen(true);
  };

  const handleSave = form.handleSubmit(async (values) => {
    const payload: CreateDevolucionCommand = {
      remitoId: values.remitoId,
      observaciones: values.observaciones.trim() || null,
      recibidoPor: values.recibidoPor.trim() || null,
      lineas: values.lineas.map((l) => ({
        productoTerminadoId: l.productoTerminadoId,
        cantidad: l.cantidad,
        lote: l.lote.trim() || null,
      })),
    };
    try {
      await apiClient<unknown>("/devoluciones", { method: "POST", body: payload });
      toast.success("Devolución creada.");
      setDialogOpen(false);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo crear la devolución.");
    }
  });

  const openDetail = async (row: DevolucionListItem) => {
    setDetail(row as unknown as Devolucion);
    setDetailLoading(true);
    try {
      const det = await apiClient<Devolucion>(`/devoluciones/${row.id}`);
      setDetail(det);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo cargar el detalle.");
    } finally {
      setDetailLoading(false);
    }
  };

  const columns: ColumnDef<DevolucionListItem, unknown>[] = [
    { accessorKey: "numero", header: "N°" },
    {
      id: "remito",
      header: "Remito",
      cell: ({ row }) => `N° ${row.original.remitoNumeroRemito}`,
    },
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

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nueva devolución" title="Nueva devolución">
              <Plus className="size-5" />
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>          </>
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Select value={filtroRemito} onValueChange={setFiltroRemito}>
          <SelectTrigger className="w-[220px]">
            <SelectValue placeholder="Todos los remitos" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los remitos</SelectItem>
            {remitosEnviados.map((r) => (
              <SelectItem key={r.id} value={r.id}>
                N° {r.numeroRemito} — {r.barNombre}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
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
        emptyMessage="No hay devoluciones."
        actions={(row) => (
          <Button variant="outline" size="sm" onClick={() => void openDetail(row)}>
            Ver
          </Button>
        )}
      />

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>Nueva devolución</DialogTitle>
            <DialogDescription>
              Registrá los productos devueltos por el bar.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="devolucion-remito">Remito</Label>
              <Controller
                control={control}
                name="remitoId"
                render={({ field }) => (
                  <Select value={field.value || undefined} onValueChange={field.onChange}>
                    <SelectTrigger id="devolucion-remito" className="w-full">
                      <SelectValue placeholder="Seleccionar…" />
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
              <FieldError message={errors.remitoId?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="devolucion-recibidoPor">Recibido por</Label>
              <Input id="devolucion-recibidoPor" {...register("recibidoPor")} />
              <FieldError message={errors.recibidoPor?.message} />
            </div>

            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label htmlFor="devolucion-observaciones">Observaciones</Label>
              <Input id="devolucion-observaciones" {...register("observaciones")} />
              <FieldError message={errors.observaciones?.message} />
            </div>

            <div className="flex flex-col gap-2 sm:col-span-2">
              <Label>Líneas</Label>
              {fields.map((field, index) => (
                <div key={field.id} className="grid grid-cols-[1.6fr_0.8fr_0.8fr_auto] items-start gap-2">
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
                    <FieldError message={errors.lineas?.[index]?.productoTerminadoId?.message} />
                  </div>
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
              ))}
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
            <DialogTitle>Devolución N° {detail?.numero}</DialogTitle>
            <DialogDescription>Detalle de la devolución y sus líneas.</DialogDescription>
          </DialogHeader>

          {detailLoading ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Cargando detalle…</p>
          ) : detail ? (
            <div className="flex flex-col gap-4">
              <div className="grid grid-cols-1 gap-x-4 gap-y-2 text-sm sm:grid-cols-2">
                <div>
                  <span className="font-medium">Remito:</span> N° {detail.remitoNumeroRemito}
                </div>
                <div>
                  <span className="font-medium">Bar:</span> {detail.barNombre || "—"}
                </div>
                <div>
                  <span className="font-medium">Fecha:</span>{" "}
                  {new Date(detail.fecha).toLocaleDateString("es-AR")}
                </div>
                <div>
                  <span className="font-medium">Total:</span> {MONEY.format(detail.totalDevolucion)}
                </div>
                {detail.observaciones && (
                  <div className="sm:col-span-2">
                    <span className="font-medium">Observaciones:</span> {detail.observaciones}
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
                    <TableHead>Producto</TableHead>
                    <TableHead className="text-right">Cantidad</TableHead>
                    <TableHead className="text-left">P. unitario original</TableHead>
                    <TableHead className="text-left">Subtotal</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detail.lineas.map((l) => (
                    <TableRow key={l.id}>
                      <TableCell>{l.productoTerminadoNombre}</TableCell>
                      <TableCell className="text-right">{l.cantidad}</TableCell>
                      <TableCell className="text-left">
                        {l.precioUnitarioOriginal ? MONEY.format(l.precioUnitarioOriginal) : "—"}
                      </TableCell>
                      <TableCell className="text-left">{MONEY.format(l.subtotal)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <p className="text-right text-sm font-medium">
                Total: {MONEY.format(detail.totalDevolucion)}
              </p>
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
