"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { ClipboardList, Plus, RefreshCw, ShoppingCart } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError, fetchAllPages } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type {
  StockOverview,
  StockAlert,
  MovimientoStock,
  Insumo,
  UnidadMedida,
  RegisterMovementCommand,
  TipoMovimientoStock,
} from "@/lib/types";
import { TIPO_MOVIMIENTO_STOCK_LABELS } from "@/lib/types";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import { Card, CardContent } from "@/components/ui/card";
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

const TIPO_OPTIONS = (Object.keys(TIPO_MOVIMIENTO_STOCK_LABELS) as unknown as string[]).map((v) => ({
  value: v,
  label: TIPO_MOVIMIENTO_STOCK_LABELS[Number(v) as TipoMovimientoStock],
}));

const movementSchema = z.object({
  insumoId: z.string().min(1, "Seleccioná un insumo."),
  tipo: z.enum(["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"]),
  cantidad: z.coerce
    .number({ message: "Ingresá un número válido." })
    .positive("Debe ser mayor a 0."),
  unidadOriginalId: z.string().min(1, "Seleccioná la unidad."),
  precioUnitario: z.string(),
  motivo: z.string().trim().min(1, "El motivo es obligatorio.").max(500, "Máximo 500 caracteres."),
  documentoOrigen: z.string().max(200, "Máximo 200 caracteres."),
});

type MovementFormInput = z.input<typeof movementSchema>;
type MovementFormValues = z.output<typeof movementSchema>;

const EMPTY_FORM: MovementFormInput = {
  insumoId: "",
  tipo: "1",
  cantidad: "",
  unidadOriginalId: "",
  precioUnitario: "",
  motivo: "",
  documentoOrigen: "",
};

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function StockPage() {
  const [overview, setOverview] = useState<StockOverview | null>(null);
  const [alerts, setAlerts] = useState<StockAlert[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [insumos, setInsumos] = useState<Insumo[]>([]);
  const [unidades, setUnidades] = useState<UnidadMedida[]>([]);

  const [movInsumoId, setMovInsumoId] = useState("");
  const [movements, setMovements] = useState<MovimientoStock[]>([]);
  const [movLoading, setMovLoading] = useState(false);
  const [movError, setMovError] = useState<string | null>(null);

  const [movFormOpen, setMovFormOpen] = useState(false);

  const [generating, setGenerating] = useState(false);

  const load = useCallback(async (isActive: () => boolean = () => true) => {
    try {
      const [ov, al, ins, uni] = await Promise.all([
        apiClient<StockOverview>("/stock/overview"),
        apiClient<StockAlert[]>("/stock/alerts"),
        fetchAllPages<Insumo>("/insumos?pageSize=100"),
        apiClient<UnidadMedida[]>("/unidadesmedida"),
      ]);
      if (!isActive()) return;
      setOverview(ov);
      setAlerts(al);
      setInsumos(ins);
      setUnidades(uni);
      setError(null);
    } catch (err) {
      if (!isActive()) return;
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar los datos de stock.");
    } finally {
      if (isActive()) setLoading(false);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load(() => !cancelled);
    return () => {
      cancelled = true;
    };
  }, [load]);

  useEffect(() => {
    if (!movInsumoId) return;
    let cancelled = false;
    void (async () => {
      try {
        const result = await apiClient<MovimientoStock[]>(`/stock/insumo/${movInsumoId}/movements`);
        if (!cancelled) setMovements(result);
      } catch (err) {
        if (!cancelled)
          setMovError(err instanceof ApiError ? err.message : "No se pudieron cargar los movimientos.");
      } finally {
        if (!cancelled) setMovLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [movInsumoId]);

  const handleMovInsumoChange = (value: string) => {
    setMovInsumoId(value);
    setMovements([]);
    setMovError(null);
    if (value) setMovLoading(true);
  };

  const form = useForm<MovementFormInput, unknown, MovementFormValues>({
    resolver: zodResolver(movementSchema),
    defaultValues: EMPTY_FORM,
  });

  const openMovForm = () => {
    form.reset(EMPTY_FORM);
    setFormInsumoId("");
    setFormTipo("1");
    setMovFormOpen(true);
  };

  const handleSaveMovement = form.handleSubmit(async (values) => {
    try {
      const payload: RegisterMovementCommand = {
        insumoId: values.insumoId,
        productoTerminadoId: null,
        tipo: Number(values.tipo) as TipoMovimientoStock,
        cantidad: values.cantidad,
        unidadOriginalId: values.unidadOriginalId,
        precioUnitario:
          values.tipo === "1" && values.precioUnitario !== ""
            ? parseFloat(values.precioUnitario)
            : null,
        motivo: values.motivo.trim(),
        documentoOrigen: values.documentoOrigen.trim() || null,
      };
      await apiClient<unknown>("/stock/movement", { method: "POST", body: payload });
      toast.success("Movimiento registrado.");
      setMovFormOpen(false);
      void load();
      if (movInsumoId) {
        setMovLoading(true);
        setMovError(null);
        try {
          const result = await apiClient<MovimientoStock[]>(
            `/stock/insumo/${movInsumoId}/movements`,
          );
          setMovements(result);
        } catch (err) {
          setMovError(
            err instanceof ApiError ? err.message : "No se pudieron cargar los movimientos.",
          );
        } finally {
          setMovLoading(false);
        }
      }
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo registrar el movimiento.");
    }
  });

  const handleGenerarOC = async () => {
    if (alerts.length === 0) return;
    setGenerating(true);
    setError(null);
    try {
      await apiClient<unknown>("/stock/alertas/generar-oc", {
        method: "POST",
        body: { insumoIds: alerts.map((a) => a.id) },
      });
      toast.success("Orden de compra generada.");
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "No se pudo generar la orden de compra.",
      );
    } finally {
      setGenerating(false);
    }
  };

  const [formInsumoId, setFormInsumoId] = useState("");
  const [formTipo, setFormTipo] = useState("1");
  const selectedInsumoData = insumos.find((i) => i.id === formInsumoId);

  const alertColumns: ColumnDef<StockAlert, unknown>[] = [
    { accessorKey: "nombre", header: "Insumo" },
    {
      id: "presentacion",
      header: "Presentación",
      cell: ({ row }) =>
        `${row.original.presentacion} ${row.original.unidadConsumoSimbolo ?? ""}`.trim(),
    },
    { accessorKey: "codigoSku", header: "SKU" },
    {
      id: "stockActual",
      header: "Stock actual",
      cell: ({ row }) =>
        `${row.original.stockActual} ${row.original.unidadConsumoSimbolo ?? ""}`,
    },
    {
      id: "stockMinimo",
      header: "Stock mínimo",
      cell: ({ row }) =>
        `${row.original.stockMinimo} ${row.original.unidadConsumoSimbolo ?? ""}`,
    },
    {
      id: "proveedor",
      header: "Proveedor",
      cell: ({ row }) => row.original.proveedorPrincipalNombreRazonSocial ?? "—",
    },
    {
      id: "precio",
      header: "Última compra",
      cell: ({ row }) =>
        row.original.precioUltimaCompra ? MONEY.format(row.original.precioUltimaCompra) : "—",
    },
  ];

  const movementColumns: ColumnDef<MovimientoStock, unknown>[] = [
    {
      id: "fecha",
      header: "Fecha",
      cell: ({ row }) => new Date(row.original.fecha).toLocaleString("es-AR"),
    },
    {
      id: "tipo",
      header: "Tipo",
      cell: ({ row }) =>
        TIPO_MOVIMIENTO_STOCK_LABELS[row.original.tipo] ?? String(row.original.tipo),
    },
    {
      id: "cantidad",
      header: "Cantidad",
      cell: ({ row }) =>
        `${row.original.cantidad > 0 ? "+" : ""}${row.original.cantidad} ${
          row.original.unidadOriginal?.simbolo ?? ""
        }`,
    },
    {
      accessorKey: "motivo",
      header: "Motivo",
      cell: ({ getValue }) => getValue<string>() || "—",
    },
    {
      id: "documento",
      header: "Documento",
      cell: ({ row }) => row.original.documentoOrigen || "—",
    },
    {
      id: "usuario",
      header: "Usuario",
      cell: ({ row }) =>
        row.original.usuario
          ? `${row.original.usuario.nombre} ${row.original.usuario.apellido}`
          : "—",
    },
  ];

  const {
    register,
    control,
    setValue,
    formState: { errors, isSubmitting },
  } = form;

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openMovForm} aria-label="Registrar movimiento" title="Registrar movimiento">
              <Plus className="size-5" />
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>          </>
        }
      />

      <div className="mb-5 grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Card>
          <CardContent className="flex flex-col gap-1">
            <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Insumos activos
            </span>
            <span className="text-2xl font-semibold">{overview?.totalInsumosActivos ?? "—"}</span>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex flex-col gap-1">
            <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Insumos críticos
            </span>
            <span className="text-2xl font-semibold text-red-600 dark:text-red-400">
              {overview?.insumosCriticos ?? "—"}
            </span>
          </CardContent>
        </Card>
      </div>

      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="text-base font-semibold">Alertas de stock mínimo</h2>
        <Button onClick={() => void handleGenerarOC()} disabled={generating || alerts.length === 0}>
          <ShoppingCart className="size-4" />
          {generating ? "Generando…" : "Generar OC"}
        </Button>
      </div>

      <DataTable
        columns={alertColumns}
        data={alerts}
        loading={loading}
        error={error}
        emptyMessage="No hay insumos por debajo del stock mínimo."
      />

      <div className="mb-4 mt-8 flex flex-wrap items-center justify-between gap-3">
        <h2 className="flex items-center gap-2 text-base font-semibold">
          <ClipboardList className="size-4 text-muted-foreground" aria-hidden="true" />
          Movimientos por insumo
        </h2>
        <Select value={movInsumoId || undefined} onValueChange={handleMovInsumoChange}>
          <SelectTrigger className="w-64">
            <SelectValue placeholder="Seleccionar insumo…" />
          </SelectTrigger>
          <SelectContent>
            {insumos.map((i) => (
              <SelectItem key={i.id} value={i.id}>
                {i.nombre}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={movementColumns}
        data={movements}
        loading={movLoading}
        error={movError}
        emptyMessage={
          movInsumoId
            ? "Sin movimientos para este insumo."
            : "Seleccioná un insumo para ver sus movimientos."
        }
      />

      <Dialog open={movFormOpen} onOpenChange={setMovFormOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Registrar movimiento de stock</DialogTitle>
            <DialogDescription>Registrá una entrada o salida de stock.</DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSaveMovement} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label htmlFor="mov-insumo">Insumo</Label>
              <Controller
                control={control}
                name="insumoId"
                render={({ field }) => (
                  <Select
                    value={field.value || undefined}
                    onValueChange={(v) => {
                      field.onChange(v);
                      setFormInsumoId(v);
                      const insumo = insumos.find((i) => i.id === v);
                      if (insumo?.unidadConsumoId) {
                        setValue("unidadOriginalId", insumo.unidadConsumoId, {
                          shouldValidate: true,
                        });
                      }
                    }}
                  >
                    <SelectTrigger id="mov-insumo" className="w-full">
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
              {selectedInsumoData && (
                <span className="text-xs text-muted-foreground">
                  Stock actual: {selectedInsumoData.stockActual}
                </span>
              )}
              <FieldError message={errors.insumoId?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="mov-tipo">Tipo de movimiento</Label>
              <Controller
                control={control}
                name="tipo"
                render={({ field }) => (
                  <Select
                    value={field.value}
                    onValueChange={(v) => {
                      field.onChange(v);
                      setFormTipo(v);
                    }}
                  >
                    <SelectTrigger id="mov-tipo" className="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {TIPO_OPTIONS.map((opt) => (
                        <SelectItem key={opt.value} value={opt.value}>
                          {opt.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="mov-cantidad">Cantidad</Label>
              <Input id="mov-cantidad" type="number" step="any" min="0" {...register("cantidad")} />
              <FieldError message={errors.cantidad?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="mov-unidad">Unidad</Label>
              <Controller
                control={control}
                name="unidadOriginalId"
                render={({ field }) => (
                  <Select value={field.value || undefined} onValueChange={field.onChange}>
                    <SelectTrigger id="mov-unidad" className="w-full">
                      <SelectValue placeholder="Seleccionar…" />
                    </SelectTrigger>
                    <SelectContent>
                      {unidades.map((u) => (
                        <SelectItem key={u.id} value={u.id}>
                          {u.nombre} ({u.simbolo})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              <FieldError message={errors.unidadOriginalId?.message} />
            </div>

            {formTipo === "1" && (
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="mov-precio">Precio unitario</Label>
                <Input id="mov-precio" type="number" step="any" min="0" {...register("precioUnitario")} />
              </div>
            )}

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="mov-motivo">Motivo</Label>
              <Input id="mov-motivo" {...register("motivo")} />
              <FieldError message={errors.motivo?.message} />
            </div>

            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label htmlFor="mov-doc">Documento origen</Label>
              <Input id="mov-doc" {...register("documentoOrigen")} />
              <FieldError message={errors.documentoOrigen?.message} />
            </div>

            <DialogFooter className="sm:col-span-2">
              <Button type="button" variant="outline" onClick={() => setMovFormOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Guardando…" : "Guardar"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
