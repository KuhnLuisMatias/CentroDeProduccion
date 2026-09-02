"use client";

import { useEffect, useState } from "react";
import {
  AlertTriangle,
  CalendarDays,
  Clock4,
  Factory,
  PackageCheck,
  Receipt,
  ShieldAlert,
  Store,
  TrendingUp,
  Truck,
  type LucideIcon,
} from "lucide-react";
import { apiClient, ApiError } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type { DashboardCharts, DashboardKPIs } from "@/lib/types";
import ChartCard from "@/components/ChartCard";
import PageHeader from "@/components/shared/PageHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

type KpiTone = "neutral" | "positive" | "warning" | "danger";

interface KpiDef {
  key: keyof DashboardKPIs;
  label: string;
  icon: LucideIcon;
  tone: KpiTone;
  format: (value: number) => string;
}

const KPI_DEFS: KpiDef[] = [
  { key: "produccionDia", label: "Producción hoy", icon: Factory, tone: "neutral", format: (v) => v.toString() },
  { key: "produccionMes", label: "Producción del mes", icon: CalendarDays, tone: "neutral", format: (v) => v.toString() },
  { key: "stockInsumosCriticos", label: "Insumos críticos", icon: AlertTriangle, tone: "danger", format: (v) => v.toString() },
  { key: "stockProductosTerminados", label: "Productos terminados", icon: PackageCheck, tone: "positive", format: (v) => v.toString() },
  { key: "productosProximosAVencer", label: "Próximos a vencer", icon: Clock4, tone: "warning", format: (v) => v.toString() },
  { key: "ventasDia", label: "Ventas hoy", icon: Receipt, tone: "positive", format: MONEY.format },
  { key: "ventasMes", label: "Ventas del mes", icon: TrendingUp, tone: "positive", format: MONEY.format },
  { key: "deudaProveedores", label: "Deuda a proveedores", icon: Truck, tone: "danger", format: MONEY.format },
  { key: "deudaBares", label: "Deuda de bares", icon: Store, tone: "danger", format: MONEY.format },
];

const KPI_SECTIONS: { id: string; label: string; keys: (keyof DashboardKPIs)[] }[] = [
  { id: "produccion", label: "Producción", keys: ["produccionDia", "produccionMes"] },
  {
    id: "stock",
    label: "Stock",
    keys: ["stockInsumosCriticos", "stockProductosTerminados", "productosProximosAVencer"],
  },
  { id: "ventas", label: "Ventas", keys: ["ventasDia", "ventasMes"] },
  { id: "finanzas", label: "Finanzas", keys: ["deudaProveedores", "deudaBares"] },
];

function KpiCard({ def, value }: { def: KpiDef; value: number }) {
  const numeric = value ?? 0;
  // Danger/warning tones only highlight when there is something to flag.
  const tone: KpiTone =
    (def.tone === "danger" || def.tone === "warning") && numeric === 0 ? "neutral" : def.tone;
  const Icon = def.icon;
  return (
    <Card className="gap-0 py-4 transition-shadow hover:shadow-md">
      <CardContent className="flex items-center gap-3 px-4">
        <span
          className={`inline-flex size-10 shrink-0 items-center justify-center rounded-lg ${TONE_ICON_CLASS[tone]}`}
        >
          <Icon className="size-5" aria-hidden="true" />
        </span>
        <div className="flex min-w-0 flex-col">
          <span className="truncate text-[11px] font-medium uppercase tracking-[0.04em] text-muted-foreground">
            {def.label}
          </span>
          <span className={`text-xl font-bold tabular-nums ${TONE_VALUE_CLASS[tone]}`}>
            {def.format(numeric)}
          </span>
        </div>
      </CardContent>
    </Card>
  );
}

const TONE_ICON_CLASS: Record<KpiTone, string> = {
  neutral: "bg-muted text-muted-foreground",
  positive: "bg-emerald-50 text-emerald-600",
  warning: "bg-amber-50 text-amber-600",
  danger: "bg-red-50 text-red-600",
};

const TONE_VALUE_CLASS: Record<KpiTone, string> = {
  neutral: "",
  positive: "text-emerald-600",
  warning: "text-amber-600",
  danger: "text-red-600",
};

export default function DashboardPage() {
  const [kpis, setKpis] = useState<DashboardKPIs | null>(null);
  const [charts, setCharts] = useState<DashboardCharts | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [forbidden, setForbidden] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      setForbidden(false);
      try {
        const [kpiData, chartData] = await Promise.all([
          apiClient<DashboardKPIs>("/dashboard"),
          apiClient<DashboardCharts>("/dashboard/charts"),
        ]);
        if (cancelled) return;
        setKpis(kpiData);
        setCharts(chartData);
      } catch (err) {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 403) {
          setForbidden(true);
        } else {
          setError(err instanceof ApiError ? err.message : "No se pudieron cargar los datos.");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    load();
    return () => {
      cancelled = true;
    };
  }, []);

  if (loading) {
    return (
      <div>
      <PageHeader
        title="Dashboard"
        description=""
      />
        <div className="grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Card key={i} className="animate-pulse py-5">
              <CardContent className="flex items-center gap-3 px-5">
                <div className="size-10 shrink-0 rounded-lg bg-muted" />
                <div className="flex flex-1 flex-col gap-2">
                  <div className="h-3 w-24 rounded bg-muted" />
                  <div className="h-5 w-16 rounded bg-muted" />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (forbidden) {
    return (
      <Card className="mx-auto mt-12 max-w-md items-center py-10 text-center shadow-sm">
        <CardContent className="flex flex-col items-center gap-3">
          <span className="inline-flex size-14 items-center justify-center rounded-full bg-amber-50">
            <ShieldAlert className="size-7 text-amber-500" aria-hidden="true" />
          </span>
          <h2 className="text-base font-semibold">Acceso restringido</h2>
          <p className="max-w-xs text-sm text-muted-foreground">
            No tiene permisos para ver el dashboard. Se requiere rol de Administrador.
          </p>
          <Button variant="outline" size="sm" className="mt-1" onClick={() => window.history.back()}>
            Volver
          </Button>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className="border-destructive/30 py-8">
        <CardContent className="flex flex-col items-center gap-2 text-center">
          <AlertTriangle className="size-8 text-destructive" aria-hidden="true" />
          <p className="text-sm font-medium text-destructive">{error}</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div>
      <PageHeader title="Dashboard" description="" />

      {KPI_SECTIONS.map((section) => {
        const defs = section.keys.map((key) => KPI_DEFS.find((d) => d.key === key)!);
        return (
          <section key={section.id} className="mt-6 first:mt-0">
            <h2 className="mb-3 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              {section.label}
            </h2>
            <div className="grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-4">
              {defs.map((def) => (
                <KpiCard
                  key={def.key}
                  def={def}
                  value={kpis ? (kpis[def.key] as number) : 0}
                />
              ))}
            </div>
          </section>
        );
      })}

      {charts && charts.charts.length > 0 && (
        <div className="mt-6 grid grid-cols-[repeat(auto-fill,minmax(360px,1fr))] gap-5">
          {charts.charts.map((chart, idx) => (
            <ChartCard key={idx} chart={chart} />
          ))}
        </div>
      )}
    </div>
  );
}
