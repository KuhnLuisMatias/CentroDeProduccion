"use client";

import { useEffect, useState, useCallback, useMemo } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { toast } from "sonner";
import { RefreshCw } from "lucide-react";
import { apiClient, ApiError } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type {
  Proveedor,
  BarListItem,
  CuentaCorrienteMovimiento,
  CuentaCorrienteBarMovimiento,
  TipoMovimientoCtaCteBar,
  RegisterNotaDebitoProveedorCommand,
  RegisterNotaCreditoProveedorCommand,
  RegisterNotaDebitoBarCommand,
  RegisterNotaCreditoBarCommand,
  RegisterCompensacionBarCommand,
} from "@/lib/types";
import {
  TIPO_MOVIMIENTO_CTA_CTE_LABELS,
  TIPO_MOVIMIENTO_CTA_CTE_BAR_LABELS,
} from "@/lib/types";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
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

type Tab = "proveedores" | "bares";
type NotaTipo = "debito" | "credito" | "compensacion";

const notaSchema = z.object({
  monto: z.coerce.number({ message: "Ingresá un número válido." }),
  referencia: z.string().max(500, "Máximo 500 caracteres."),
  fecha: z.string(),
});

type NotaFormInput = z.input<typeof notaSchema>;
type NotaFormValues = z.output<typeof notaSchema>;

const EMPTY_NOTA: NotaFormInput = { monto: "", referencia: "", fecha: "" };

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function CuentaCorrientePage() {
  const [proveedores, setProveedores] = useState<Proveedor[]>([]);
  const [bares, setBares] = useState<BarListItem[]>([]);

  // proveedor tab state
  const [provId, setProvId] = useState("");
  const [provSaldo, setProvSaldo] = useState<number | null>(null);
  const [provMovs, setProvMovs] = useState<CuentaCorrienteMovimiento[]>([]);
  const [provDesde, setProvDesde] = useState("");
  const [provHasta, setProvHasta] = useState("");

  // bar tab state
  const [barId, setBarId] = useState("");
  const [barSaldo, setBarSaldo] = useState<number | null>(null);
  const [barMovs, setBarMovs] = useState<CuentaCorrienteBarMovimiento[]>([]);
  const [barTipo, setBarTipo] = useState("all");
  const [barDesde, setBarDesde] = useState("");
  const [barHasta, setBarHasta] = useState("");

  const [error, setError] = useState<string | null>(null);
  const [refreshTick, setRefreshTick] = useState(0);
  const [loading, setLoading] = useState(false);

  const load = useCallback(() => {
    setLoading(true);
    setRefreshTick((t) => t + 1);
    // setLoading will be reset on next render cycle by the useEffect triggers
    setTimeout(() => setLoading(false), 500);
  }, []);

  // nota dialog
  const [notaModal, setNotaModal] = useState<NotaTipo | null>(null);
  const [tab, setTab] = useState<Tab>("proveedores");

  useEffect(() => {
    let cancelled = false;
    async function run() {
      try {
        const [provList, barList] = await Promise.all([
          apiClient<Proveedor[]>("/proveedores"),
          apiClient<BarListItem[]>("/bares"),
        ]);
        if (cancelled) return;
        setProveedores(provList);
        setBares(barList);
      } catch (err) {
        if (!cancelled) setError(err instanceof ApiError ? err.message : "No se pudieron cargar los datos.");
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, []);

  const notaForm = useForm<NotaFormInput, unknown, NotaFormValues>({
    resolver: zodResolver(notaSchema),
    defaultValues: EMPTY_NOTA,
  });

  const openNota = (tipo: NotaTipo) => {
    notaForm.reset({ ...EMPTY_NOTA, fecha: new Date().toISOString().slice(0, 10) });
    setNotaModal(tipo);
  };

  useEffect(() => {
    let cancelled = false;
    async function run() {
      if (!provId) return;
      try {
        const params = new URLSearchParams();
        if (provDesde) params.set("fechaDesde", provDesde);
        if (provHasta) params.set("fechaHasta", provHasta);
        const qs = params.toString();
        const [movs, saldo] = await Promise.all([
          apiClient<CuentaCorrienteMovimiento[]>(`/proveedores/${provId}/cuenta-corriente${qs ? `?${qs}` : ""}`),
          apiClient<number>(`/proveedores/${provId}/cuenta-corriente/saldo`),
        ]);
        if (cancelled) return;
        setProvMovs(movs);
        setProvSaldo(saldo);
        setError(null);
      } catch (err) {
        if (!cancelled) setError(err instanceof ApiError ? err.message : "No se pudo cargar la cuenta corriente.");
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, [provId, provDesde, provHasta, refreshTick]);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      if (!barId) return;
      try {
        const params = new URLSearchParams();
        if (barTipo && barTipo !== "all") params.set("tipo", barTipo);
        if (barDesde) params.set("fechaDesde", barDesde);
        if (barHasta) params.set("fechaHasta", barHasta);
        const qs = params.toString();
        const [movs, saldo] = await Promise.all([
          apiClient<CuentaCorrienteBarMovimiento[]>(`/bares/${barId}/cuenta-corriente${qs ? `?${qs}` : ""}`),
          apiClient<number>(`/bares/${barId}/cuenta-corriente/saldo`),
        ]);
        if (cancelled) return;
        setBarMovs(movs);
        setBarSaldo(saldo);
        setError(null);
      } catch (err) {
        if (!cancelled) setError(err instanceof ApiError ? err.message : "No se pudo cargar la cuenta corriente.");
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, [barId, barTipo, barDesde, barHasta, refreshTick]);

  const saveNota = notaForm.handleSubmit(async (values) => {
    const monto = values.monto;
    if (tab === "proveedores") {
      if (!provId) return;
      if (monto <= 0) {
        notaForm.setError("monto", { message: "El monto debe ser mayor a cero." });
        return;
      }
      try {
        const referencia = values.referencia.trim() || null;
        if (notaModal === "debito") {
          const body: RegisterNotaDebitoProveedorCommand = { proveedorId: provId, monto, referencia };
          await apiClient<unknown>(`/proveedores/${provId}/cuenta-corriente/nota-debito`, { method: "POST", body });
        } else if (notaModal === "credito") {
          const body: RegisterNotaCreditoProveedorCommand = { proveedorId: provId, monto, referencia };
          await apiClient<unknown>(`/proveedores/${provId}/cuenta-corriente/nota-credito`, { method: "POST", body });
        } else {
          return;
        }
        toast.success(notaModal === "debito" ? "Nota de débito registrada." : "Nota de crédito registrada.");
        setNotaModal(null);
        setRefreshTick((t) => t + 1);
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "No se pudo registrar la nota.");
      }
    } else {
      if (!barId) return;
      if (monto === 0) {
        notaForm.setError("monto", { message: "El monto no puede ser cero." });
        return;
      }
      try {
        const referencia = values.referencia.trim() || null;
        const fecha = values.fecha || null;
        const urlBase = `/bares/${barId}/cuenta-corriente`;
        if (notaModal === "debito") {
          const body: RegisterNotaDebitoBarCommand = { barId, monto, referencia, fecha };
          await apiClient<unknown>(`${urlBase}/nota-debito`, { method: "POST", body });
          toast.success("Nota de débito registrada.");
        } else if (notaModal === "credito") {
          const body: RegisterNotaCreditoBarCommand = { barId, monto, referencia, fecha };
          await apiClient<unknown>(`${urlBase}/nota-credito`, { method: "POST", body });
          toast.success("Nota de crédito registrada.");
        } else {
          const body: RegisterCompensacionBarCommand = { barId, monto, referencia, fecha };
          await apiClient<unknown>(`${urlBase}/compensacion`, { method: "POST", body });
          toast.success("Compensación registrada.");
        }
        setNotaModal(null);
        setRefreshTick((t) => t + 1);
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "No se pudo registrar la nota.");
      }
    }
  });

  const provColumns: ColumnDef<CuentaCorrienteMovimiento, unknown>[] = [
    {
      id: "fecha",
      header: "Fecha",
      cell: ({ row }) => new Date(row.original.fecha).toLocaleString("es-AR"),
    },
    {
      id: "tipo",
      header: "Tipo",
      cell: ({ row }) =>
        TIPO_MOVIMIENTO_CTA_CTE_LABELS[row.original.tipoMovimiento] ?? row.original.tipoMovimiento,
    },
    {
      accessorKey: "monto",
      header: "Monto",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
    {
      accessorKey: "saldo",
      header: "Saldo acumulado",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
    {
      id: "referencia",
      header: "Referencia",
      cell: ({ row }) => row.original.referencia || "—",
    },
  ];

  const barColumns: ColumnDef<CuentaCorrienteBarMovimiento, unknown>[] = [
    {
      id: "fecha",
      header: "Fecha",
      cell: ({ row }) => new Date(row.original.fecha).toLocaleString("es-AR"),
    },
    {
      id: "tipo",
      header: "Tipo",
      cell: ({ row }) =>
        TIPO_MOVIMIENTO_CTA_CTE_BAR_LABELS[row.original.tipoMovimiento] ?? row.original.tipoMovimiento,
    },
    {
      accessorKey: "monto",
      header: "Monto",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
    {
      accessorKey: "saldoAcumulado",
      header: "Saldo acumulado",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
    {
      id: "referencia",
      header: "Referencia",
      cell: ({ row }) => row.original.referencia || "—",
    },
  ];

  const notaTitle =
    tab === "bares" && notaModal === "compensacion"
      ? "Registrar compensación"
      : notaModal === "debito"
        ? "Registrar nota de débito"
        : "Registrar nota de crédito";

  // Monthly totals computed from the movements already fetched for the
  // selected account (Σ positive Compra / Remito movements in the current month).
  const provEgresosMes = useMemo(() => {
    const now = new Date();
    return provMovs
      .filter((m) => {
        const d = new Date(m.fecha);
        return (
          m.tipoMovimiento === 1 &&
          m.monto > 0 &&
          d.getMonth() === now.getMonth() &&
          d.getFullYear() === now.getFullYear()
        );
      })
      .reduce((sum, m) => sum + m.monto, 0);
  }, [provMovs]);

  const barIngresosMes = useMemo(() => {
    const now = new Date();
    return barMovs
      .filter((m) => {
        const d = new Date(m.fecha);
        return (
          m.tipoMovimiento === 1 &&
          m.monto > 0 &&
          d.getMonth() === now.getMonth() &&
          d.getFullYear() === now.getFullYear()
        );
      })
      .reduce((sum, m) => sum + m.monto, 0);
  }, [barMovs]);

  const {
    register,
    formState: { errors, isSubmitting },
  } = notaForm;

  return (
    <div>
      <PageHeader
        title="Cuentas Corrientes"
        description="Estado de cuenta y movimientos de proveedores y bares."
        actions={
          <Button variant="outline" size="sm" onClick={load} disabled={loading}>
            <RefreshCw className={`size-4 ${loading ? "animate-spin" : ""}`} />
            Actualizar
          </Button>
        }
      />

      <Tabs value={tab} onValueChange={(v) => setTab(v as Tab)}>
        <TabsList className="mb-4">
          <TabsTrigger value="proveedores">Proveedores</TabsTrigger>
          <TabsTrigger value="bares">Bares</TabsTrigger>
        </TabsList>

        <TabsContent value="proveedores">
          <div className="mb-4 flex flex-wrap items-center gap-2">
            <Select value={provId || undefined} onValueChange={setProvId}>
              <SelectTrigger className="w-[220px]">
                <SelectValue placeholder="Seleccionar proveedor…" />
              </SelectTrigger>
              <SelectContent>
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
              value={provDesde}
              onChange={(e) => setProvDesde(e.target.value)}
              aria-label="Fecha desde"
            />
            <Input
              type="date"
              className="w-[150px]"
              value={provHasta}
              onChange={(e) => setProvHasta(e.target.value)}
              aria-label="Fecha hasta"
            />
            {provId && (
              <>
                <Button variant="outline" size="sm" onClick={() => openNota("debito")}>
                  Nota débito
                </Button>
                <Button variant="outline" size="sm" onClick={() => openNota("credito")}>
                  Nota crédito
                </Button>
              </>
            )}
          </div>

          {provId && (
            <div className="mb-4 flex flex-wrap gap-3">
              <Card className="w-fit min-w-56">
                <CardContent>
                  <p className="text-xs font-semibold uppercase tracking-[0.04em] text-muted-foreground">
                    Saldo
                  </p>
                  <p className="mt-1 text-2xl font-bold tabular-nums">
                    {provSaldo === null ? "…" : MONEY.format(provSaldo)}
                  </p>
                </CardContent>
              </Card>
              <Card className="w-fit min-w-56">
                <CardContent>
                  <p className="text-xs font-semibold uppercase tracking-[0.04em] text-muted-foreground">
                    Total egresos (mes)
                  </p>
                  <p className="mt-1 text-2xl font-bold tabular-nums">
                    {MONEY.format(provEgresosMes)}
                  </p>
                </CardContent>
              </Card>
            </div>
          )}

          <DataTable
            columns={provColumns}
            data={provId ? provMovs : []}
            error={error}
            emptyMessage="No hay movimientos."
          />
        </TabsContent>

        <TabsContent value="bares">
          <div className="mb-4 flex flex-wrap items-center gap-2">
            <Select value={barId || undefined} onValueChange={setBarId}>
              <SelectTrigger className="w-[220px]">
                <SelectValue placeholder="Seleccionar bar…" />
              </SelectTrigger>
              <SelectContent>
                {bares.map((b) => (
                  <SelectItem key={b.id} value={b.id}>
                    {b.nombre}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select value={barTipo} onValueChange={setBarTipo}>
              <SelectTrigger className="w-[170px]">
                <SelectValue placeholder="Todos los tipos" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos los tipos</SelectItem>
                {(Object.keys(TIPO_MOVIMIENTO_CTA_CTE_BAR_LABELS) as unknown as string[]).map((v) => (
                  <SelectItem key={v} value={v}>
                    {TIPO_MOVIMIENTO_CTA_CTE_BAR_LABELS[Number(v) as TipoMovimientoCtaCteBar]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Input
              type="date"
              className="w-[150px]"
              value={barDesde}
              onChange={(e) => setBarDesde(e.target.value)}
              aria-label="Fecha desde"
            />
            <Input
              type="date"
              className="w-[150px]"
              value={barHasta}
              onChange={(e) => setBarHasta(e.target.value)}
              aria-label="Fecha hasta"
            />
            {barId && (
              <>
                <Button variant="outline" size="sm" onClick={() => openNota("debito")}>
                  Nota débito
                </Button>
                <Button variant="outline" size="sm" onClick={() => openNota("credito")}>
                  Nota crédito
                </Button>
                <Button variant="outline" size="sm" onClick={() => openNota("compensacion")}>
                  Compensación
                </Button>
              </>
            )}
          </div>

          {barId && (
            <div className="mb-4 flex flex-wrap gap-3">
              <Card className="w-fit min-w-56">
                <CardContent>
                  <p className="text-xs font-semibold uppercase tracking-[0.04em] text-muted-foreground">
                    Saldo
                  </p>
                  <p className="mt-1 text-2xl font-bold tabular-nums">
                    {barSaldo === null ? "…" : MONEY.format(barSaldo)}
                  </p>
                </CardContent>
              </Card>
              <Card className="w-fit min-w-56">
                <CardContent>
                  <p className="text-xs font-semibold uppercase tracking-[0.04em] text-muted-foreground">
                    Total ingresos (mes)
                  </p>
                  <p className="mt-1 text-2xl font-bold tabular-nums">
                    {MONEY.format(barIngresosMes)}
                  </p>
                </CardContent>
              </Card>
            </div>
          )}

          <DataTable
            columns={barColumns}
            data={barId ? barMovs : []}
            error={error}
            emptyMessage="No hay movimientos."
          />
        </TabsContent>
      </Tabs>

      <Dialog
        open={notaModal !== null}
        onOpenChange={(open) => {
          if (!open) setNotaModal(null);
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{notaTitle}</DialogTitle>
            <DialogDescription>
              {tab === "bares" && notaModal !== null
                ? "El monto puede ser positivo o negativo según el impacto en la cuenta."
                : "Ingresá el monto de la nota."}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={saveNota} className="flex flex-col gap-3" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="nota-monto">Monto</Label>
              <Input id="nota-monto" type="number" step="any" {...register("monto")} />
              <FieldError message={errors.monto?.message} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="nota-referencia">Referencia</Label>
              <Input id="nota-referencia" placeholder="Opcional" {...register("referencia")} />
              <FieldError message={errors.referencia?.message} />
            </div>
            {tab === "bares" && (
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="nota-fecha">Fecha</Label>
                <Input id="nota-fecha" type="date" {...register("fecha")} />
                <FieldError message={errors.fecha?.message} />
              </div>
            )}

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setNotaModal(null)}
                disabled={isSubmitting}
              >
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
