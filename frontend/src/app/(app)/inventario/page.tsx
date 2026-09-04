"use client";

import { useCallback, useEffect, useState } from "react";
import type { ColumnDef } from "@tanstack/react-table";
import { Plus, RefreshCw, Save, Search } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api";
import type {
  ConfirmInventarioSesionCommand,
  ConfirmInventarioSesionResponse,
  CreateInventarioSesionResponse,
  EstadoInventario,
  InventarioConteoDto,
  InventarioSesionDetail,
  InventarioSesionListItem,
  RegistrarConteoCommand,
  RegistrarConteoResponse,
  TipoInventario,
} from "@/lib/types";
import { ESTADO_INVENTARIO_LABELS, TIPO_INVENTARIO_LABELS } from "@/lib/types";
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const NUM = new Intl.NumberFormat("es-AR", { maximumFractionDigits: 2 });

const ESTADO_OPTIONS = (Object.keys(ESTADO_INVENTARIO_LABELS) as unknown as string[]).map((v) => ({
  value: v,
  label: ESTADO_INVENTARIO_LABELS[Number(v) as EstadoInventario],
}));

const TIPO_OPTIONS = (Object.keys(TIPO_INVENTARIO_LABELS) as unknown as string[]).map((v) => ({
  value: v,
  label: TIPO_INVENTARIO_LABELS[Number(v) as TipoInventario],
}));

interface Conteodraft {
  cantidadContada: string;
  observaciones: string;
}

function estadoBadgeClass(estado: EstadoInventario) {
  if (estado === 3)
    return "border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400";
  if (estado === 2)
    return "border-amber-600/30 bg-amber-500/10 text-amber-700 dark:text-amber-400";
  return undefined;
}

export default function InventarioPage() {
  const [sessions, setSessions] = useState<InventarioSesionListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [filtroEstado, setFiltroEstado] = useState("all");
  const [filtroTipo, setFiltroTipo] = useState("all");
  const [filtroDesde, setFiltroDesde] = useState("");
  const [filtroHasta, setFiltroHasta] = useState("");

  const [createOpen, setCreateOpen] = useState(false);
  const [createTipo, setCreateTipo] = useState("1");
  const [createNotas, setCreateNotas] = useState("");
  const [creating, setCreating] = useState(false);

  const [detail, setDetail] = useState<InventarioSesionDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [draft, setDraft] = useState<Record<string, Conteodraft>>({});
  const [savingId, setSavingId] = useState<string | null>(null);

  const [confirmOpen, setConfirmOpen] = useState(false);
  const [confirming, setConfirming] = useState(false);

  const buildQuery = useCallback(() => {
    const params = new URLSearchParams();
    if (filtroEstado && filtroEstado !== "all") params.append("estado", filtroEstado);
    if (filtroTipo && filtroTipo !== "all") params.append("tipo", filtroTipo);
    if (filtroDesde) params.append("desde", filtroDesde);
    if (filtroHasta) params.append("hasta", filtroHasta);
    const qs = params.toString();
    return `/stock/inventario${qs ? `?${qs}` : ""}`;
  }, [filtroEstado, filtroTipo, filtroDesde, filtroHasta]);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await apiClient<InventarioSesionListItem[]>(buildQuery());
      setSessions(result);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar las sesiones de inventario.");
    } finally {
      setLoading(false);
    }
  }, [buildQuery]);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      try {
        const result = await apiClient<InventarioSesionListItem[]>(buildQuery());
        if (cancelled) return;
        setSessions(result);
        setError(null);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof ApiError ? err.message : "No se pudieron cargar las sesiones de inventario.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, [buildQuery]);

  const openCreate = () => {
    setCreateTipo("1");
    setCreateNotas("");
    setCreateOpen(true);
  };

  const handleCreate = async () => {
    setCreating(true);
    try {
      const result = await apiClient<CreateInventarioSesionResponse>("/stock/inventario", {
        method: "POST",
        body: {
          tipoInventario: Number(createTipo) as TipoInventario,
          notas: createNotas.trim() || null,
        },
      });
      toast.success("Sesión de inventario creada.");
      setCreateOpen(false);
      await load();
      void openDetail(result.id);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo crear la sesión.");
    } finally {
      setCreating(false);
    }
  };

  const applyDetail = (result: InventarioSesionDetail) => {
    setDetail(result);
    const draftMap: Record<string, Conteodraft> = {};
    for (const c of result.conteos) {
      draftMap[c.id] = {
        cantidadContada: String(c.cantidadContada),
        observaciones: c.observaciones ?? "",
      };
    }
    setDraft(draftMap);
  };

  const openDetail = async (id: string) => {
    setDetailLoading(true);
    try {
      const result = await apiClient<InventarioSesionDetail>(`/stock/inventario/${id}`);
      applyDetail(result);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo cargar la sesión.");
    } finally {
      setDetailLoading(false);
    }
  };

  const closeDetail = () => {
    setDetail(null);
    setDraft({});
  };

  const updateDraft = (conteoId: string, patch: Partial<Conteodraft>) => {
    setDraft((prev) => ({
      ...prev,
      [conteoId]: { ...(prev[conteoId] ?? { cantidadContada: "", observaciones: "" }), ...patch },
    }));
  };

  const saveConteo = async (conteo: InventarioConteoDto) => {
    if (!detail) return;
    setSavingId(conteo.id);
    try {
      const d = draft[conteo.id];
      const cantidad = parseFloat(d?.cantidadContada ?? "");
      const command: RegistrarConteoCommand = {
        inventarioSesionId: detail.id,
        conteoId: conteo.id,
        cantidadContada: Number.isNaN(cantidad) ? 0 : cantidad,
        observaciones: d?.observaciones.trim() ? d.observaciones.trim() : null,
      };
      await apiClient<RegistrarConteoResponse>(`/stock/inventario/${detail.id}/conteos`, {
        method: "POST",
        body: command,
      });
      toast.success("Conteo guardado.");
      await openDetail(detail.id);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo guardar el conteo.");
    } finally {
      setSavingId(null);
    }
  };

  const handleConfirm = async () => {
    if (!detail) return;
    setConfirming(true);
    try {
      // Fetch fresh detail first to get the latest rowVersion for concurrency.
      const fresh = await apiClient<InventarioSesionDetail>(`/stock/inventario/${detail.id}`);
      const command: ConfirmInventarioSesionCommand = {
        inventarioSesionId: detail.id,
        rowVersion: fresh.rowVersion ?? "",
      };
      const result = await apiClient<ConfirmInventarioSesionResponse>(
        `/stock/inventario/${detail.id}/confirmar`,
        { method: "POST", body: command },
      );
      toast.success(
        `Sesión confirmada. Ajustes generados: ${result.ajustesGenerados}. Diferencia total: ${NUM.format(result.diferenciaTotal)}.`,
      );
      setConfirmOpen(false);
      await openDetail(detail.id);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo confirmar la sesión.");
      setConfirmOpen(false);
    } finally {
      setConfirming(false);
    }
  };

  const canConfirm =
    detail !== null &&
    (detail.estado === 1 || detail.estado === 2) &&
    detail.conteos.some((c) => !c.conteoOk);

  const columns: ColumnDef<InventarioSesionListItem, unknown>[] = [
    {
      accessorKey: "fecha",
      header: "Fecha",
      cell: ({ row }) => new Date(row.original.fecha).toLocaleString("es-AR"),
    },
    {
      accessorKey: "tipo",
      header: "Tipo",
      cell: ({ row }) => TIPO_INVENTARIO_LABELS[row.original.tipo] ?? String(row.original.tipo),
    },
    {
      accessorKey: "estado",
      header: "Estado",
      cell: ({ row }) => (
        <Badge variant="outline" className={estadoBadgeClass(row.original.estado)}>
          {ESTADO_INVENTARIO_LABELS[row.original.estado] ?? String(row.original.estado)}
        </Badge>
      ),
    },
    { accessorKey: "totalItems", header: "Ítems" },
    {
      accessorKey: "diferenciaTotal",
      header: "Diferencia total",
      cell: ({ getValue }) => NUM.format(getValue<number>()),
    },
  ];

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nueva sesión" title="Nueva sesión">
              <Plus className="size-5" aria-hidden="true" />
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>          </>
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Select value={filtroEstado} onValueChange={setFiltroEstado}>
          <SelectTrigger className="w-[160px]" aria-label="Estado">
            <SelectValue placeholder="Todos los estados" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los estados</SelectItem>
            {ESTADO_OPTIONS.map((opt) => (
              <SelectItem key={opt.value} value={opt.value}>
                {opt.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={filtroTipo} onValueChange={setFiltroTipo}>
          <SelectTrigger className="w-[180px]" aria-label="Tipo">
            <SelectValue placeholder="Todos los tipos" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los tipos</SelectItem>
            {TIPO_OPTIONS.map((opt) => (
              <SelectItem key={opt.value} value={opt.value}>
                {opt.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Input
          type="date"
          className="w-[150px]"
          aria-label="Desde"
          value={filtroDesde}
          onChange={(e) => setFiltroDesde(e.target.value)}
        />
        <Input
          type="date"
          className="w-[150px]"
          aria-label="Hasta"
          value={filtroHasta}
          onChange={(e) => setFiltroHasta(e.target.value)}
        />
        <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
          <Search className="size-4" aria-hidden="true" />
          Filtrar
        </Button>
      </div>

      <DataTable
        columns={columns}
                data={sessions}
        loading={loading}
        error={error}
        emptyMessage="No hay sesiones de inventario."
        actions={(row) => (
          <Button variant="outline" size="sm" onClick={() => void openDetail(row.id)}>
            Ver detalle
          </Button>
        )}
      />

      <Dialog open={createOpen} onOpenChange={(open) => !creating && setCreateOpen(open)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Nueva sesión de inventario</DialogTitle>
            <DialogDescription>
              Se generará una línea de conteo por cada ítem del tipo elegido.
            </DialogDescription>
          </DialogHeader>

          <div className="grid grid-cols-1 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="inv-tipo">Tipo de inventario</Label>
              <Select value={createTipo} onValueChange={setCreateTipo}>
                <SelectTrigger id="inv-tipo" className="w-full">
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
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="inv-notas">Notas</Label>
              <Input
                id="inv-notas"
                value={createNotas}
                onChange={(e) => setCreateNotas(e.target.value)}
                placeholder="Opcional"
              />
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setCreateOpen(false)} disabled={creating}>
              Cancelar
            </Button>
            <Button type="button" onClick={() => void handleCreate()} disabled={creating}>
              {creating ? "Creando…" : "Crear"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={detail !== null} onOpenChange={(open) => !open && closeDetail()}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>Detalle de sesión</DialogTitle>
            <DialogDescription>
              Registrá las cantidades contadas y confirmá la sesión para generar los ajustes de stock.
            </DialogDescription>
          </DialogHeader>

          {detailLoading && !detail ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Cargando…</p>
          ) : detail ? (
            <div className="flex flex-col gap-4">
              <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-sm">
                <div>
                  <span className="font-medium">Tipo:</span>{" "}
                  {TIPO_INVENTARIO_LABELS[detail.tipo]}
                </div>
                <div className="flex items-center gap-1.5">
                  <span className="font-medium">Estado:</span>
                  <Badge variant="outline" className={estadoBadgeClass(detail.estado)}>
                    {ESTADO_INVENTARIO_LABELS[detail.estado]}
                  </Badge>
                </div>
                <div>
                  <span className="font-medium">Fecha:</span>{" "}
                  {new Date(detail.fecha).toLocaleString("es-AR")}
                </div>
                <div>
                  <span className="font-medium">Diferencia total:</span>{" "}
                  {NUM.format(detail.diferenciaTotal)}
                </div>
              </div>
              {detail.notas && (
                <p className="rounded-md border bg-muted/40 px-3 py-2 text-sm text-muted-foreground">
                  {detail.notas}
                </p>
              )}

              <ConteosTable
                detail={detail}
                draft={draft}
                savingId={savingId}
                onUpdate={updateDraft}
                onSave={(conteo) => void saveConteo(conteo)}
              />

              {(detail.estado === 1 || detail.estado === 2) && (
                <div className="flex flex-wrap items-center gap-3">
                  <Button
                    variant="destructive"
                    onClick={() => setConfirmOpen(true)}
                    disabled={!canConfirm || savingId !== null}
                  >
                    Confirmar sesión
                  </Button>
                  {!canConfirm && (
                    <span className="text-sm text-muted-foreground">
                      No hay diferencias para ajustar (o la sesión está cerrada).
                    </span>
                  )}
                </div>
              )}
            </div>
          ) : null}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmOpen && detail !== null}
        onOpenChange={(open) => {
          if (!open) setConfirmOpen(false);
        }}
        title="Confirmar sesión"
        message="Al confirmar se ajustará el stock según las cantidades contadas y se cerrará la sesión. ¿Desea continuar?"
        confirmLabel="Confirmar"
        destructive
        busy={confirming}
        onConfirm={() => void handleConfirm()}
      />
    </div>
  );
}

interface ConteosTableProps {
  detail: InventarioSesionDetail;
  draft: Record<string, Conteodraft>;
  savingId: string | null;
  onUpdate: (conteoId: string, patch: Partial<Conteodraft>) => void;
  onSave: (conteo: InventarioConteoDto) => void;
}

function ConteosTable({ detail, draft, savingId, onUpdate, onSave }: ConteosTableProps) {
  const closed = detail.estado === 3;

  const columns: ColumnDef<InventarioConteoDto, unknown>[] = [
    {
      id: "item",
      header: "Ítem",
      cell: ({ row }) => row.original.insumoNombre ?? row.original.productoTerminadoNombre ?? "—",
    },
    {
      id: "cantidadSistema",
      header: "Cant. sistema",
      cell: ({ row }) => (
        <span className="tabular-nums">{NUM.format(row.original.cantidadSistema)}</span>
      ),
    },
    {
      id: "cantidadContada",
      header: "Cant. contada",
      cell: ({ row }) => {
        const conteo = row.original;
        if (closed) {
          return <span className="tabular-nums">{NUM.format(conteo.cantidadContada)}</span>;
        }
        return (
          <div className="flex items-center gap-2">
            <Input
              type="number"
              step="any"
              className="w-24"
              aria-label={`Cantidad contada de ${conteo.insumoNombre ?? conteo.productoTerminadoNombre ?? "ítem"}`}
              value={draft[conteo.id]?.cantidadContada ?? ""}
              onChange={(e) => onUpdate(conteo.id, { cantidadContada: e.target.value })}
            />
            <Button
              variant="outline"
              size="sm"
              disabled={savingId === conteo.id}
              onClick={() => onSave(conteo)}
            >
              <Save className="size-4" aria-hidden="true" />
              {savingId === conteo.id ? "…" : "Guardar"}
            </Button>
          </div>
        );
      },
    },
    {
      id: "diferencia",
      header: "Diferencia",
      cell: ({ row }) => {
        const conteo = row.original;
        let diff = conteo.diferencia;
        if (!closed) {
          const parsed = parseFloat(draft[conteo.id]?.cantidadContada ?? "");
          diff = Number.isNaN(parsed) ? conteo.cantidadContada - conteo.cantidadSistema : parsed - conteo.cantidadSistema;
        }
        return (
          <Badge
            variant="outline"
            className={
              diff === 0
                ? "border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400"
                : "border-red-600/30 bg-red-500/10 text-red-700 dark:text-red-400"
            }
          >
            {NUM.format(diff)}
          </Badge>
        );
      },
    },
    {
      id: "observaciones",
      header: "Observaciones",
      cell: ({ row }) => {
        const conteo = row.original;
        if (closed) return conteo.observaciones || "—";
        return (
          <Input
            className="w-56"
            aria-label={`Observaciones de ${conteo.insumoNombre ?? conteo.productoTerminadoNombre ?? "ítem"}`}
            value={draft[conteo.id]?.observaciones ?? ""}
            onChange={(e) => onUpdate(conteo.id, { observaciones: e.target.value })}
          />
        );
      },
    },
  ];

  return (
    <DataTable
      columns={columns}
      data={detail.conteos}
      emptyMessage="No hay líneas de conteo."
    />
  );
}
