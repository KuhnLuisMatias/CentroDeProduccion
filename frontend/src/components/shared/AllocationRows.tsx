"use client";

import type { ReactNode } from "react";
import { Trash2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { cn, MONEY } from "@/lib/utils";

export interface AllocationEntity {
  id: string;
  /** Document number, e.g. OC number or remito number. */
  numero: string | number;
  /** Counterparty name: provider name (OC) or bar name (remito). */
  nombre: string;
  estadoLabel: string;
  estadoTone?: "success" | "info" | "warning" | "danger" | "neutral";
  total: number;
  /**
   * Amount already paid against this entity by PRIOR payments, when the data
   * is available. When null/undefined the pending balance cannot be computed
   * and only the total is shown with a note.
   */
  yaPagado?: number | null;
}

const TONE_CLASSES: Record<NonNullable<AllocationEntity["estadoTone"]>, string> = {
  success:
    "border-emerald-500/40 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400",
  info: "border-sky-500/40 bg-sky-500/10 text-sky-700 dark:text-sky-400",
  warning:
    "border-amber-500/40 bg-amber-500/10 text-amber-700 dark:text-amber-400",
  danger:
    "border-destructive/50 bg-destructive/10 text-destructive",
  neutral: "border-border bg-muted text-muted-foreground",
};

/** Pending balance for an entity, or null when prior-payment data is missing. */
export function saldoPendiente(entity: AllocationEntity): number | null {
  if (entity.yaPagado == null) return null;
  return Math.max(entity.total - entity.yaPagado, 0);
}

/** Parses a currency input value ("1234.5", "", etc.) into a number. */
export function allocationAmount(value: unknown): number {
  const parsed = parseFloat(String(value ?? ""));
  return Number.isFinite(parsed) ? parsed : 0;
}

interface AllocationEntityCardProps {
  /** The entity picker (Select) wired to the form field. */
  selectSlot: ReactNode;
  /** The amount input (CurrencyInput / Input) wired to the form field. */
  amountSlot: ReactNode;
  /** Resolved entity for the currently selected id, or null while unselected. */
  entity: AllocationEntity | null;
  /** Current typed amount, already parsed via allocationAmount(). */
  amount: number;
  onRemove: () => void;
  removeLabel?: string;
  /** Document label shown in the card header ("OC", "Remito", …). */
  docLabel?: string;
}

/**
 * Rich allocation row: shows which document is being paid and how much debt
 * remains, with live progress and inline validation hints.
 */
export function AllocationEntityCard({
  selectSlot,
  amountSlot,
  entity,
  amount,
  onRemove,
  removeLabel = "Eliminar asignación",
  docLabel = "OC",
}: AllocationEntityCardProps) {
  const saldo = entity ? saldoPendiente(entity) : null;
  const exceeds = saldo != null && amount > saldo + 0.0049;
  const remaining = saldo != null ? saldo - amount : null;
  const progressPct =
    saldo != null && saldo > 0
      ? Math.min(100, Math.max(0, (amount / saldo) * 100))
      : amount > 0
        ? 100
        : 0;

  return (
    <div className="rounded-lg border border-border bg-muted/20 p-3">
      <div className="grid grid-cols-[1fr_auto] items-start gap-2">
        <div className="flex flex-col gap-1">{selectSlot}</div>
        <Button
          type="button"
          variant="destructive"
          size="icon"
          onClick={onRemove}
          aria-label={removeLabel}
        >
          <Trash2 className="size-4" />
        </Button>
      </div>

      {entity && (
        <div className="mt-3 flex flex-col gap-2">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-semibold">
              {docLabel} #{entity.numero} — {entity.nombre}
            </span>
            <Badge
              variant="outline"
              className={cn(
                "px-1.5 py-0 text-[10px]",
                TONE_CLASSES[entity.estadoTone ?? "neutral"],
              )}
            >
              {entity.estadoLabel}
            </Badge>
          </div>

          <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
            <span>
              Total {docLabel}:{" "}
              <span className="font-medium text-foreground">
                {MONEY.format(entity.total)}
              </span>
            </span>
            {saldo != null ? (
              <>
                <span>
                  Ya pagado:{" "}
                  <span className="font-medium text-foreground">
                    {MONEY.format(entity.yaPagado ?? 0)}
                  </span>
                </span>
                <span>
                  Saldo pendiente:{" "}
                  <span
                    className={cn(
                      "font-medium",
                      saldo <= 0
                        ? "text-emerald-600 dark:text-emerald-400"
                        : "text-foreground",
                    )}
                  >
                    {MONEY.format(saldo)}
                  </span>
                </span>
              </>
            ) : (
              <span>(sin datos de pagos previos para calcular el saldo)</span>
            )}
          </div>

          {saldo != null && (
            <div className="flex items-center gap-2">
              <div
                className="h-1.5 w-full max-w-56 overflow-hidden rounded-full bg-muted"
                role="progressbar"
                aria-valuemin={0}
                aria-valuemax={100}
                aria-valuenow={Math.round(progressPct)}
              >
                <div
                  className={cn(
                    "h-full rounded-full transition-all",
                    exceeds ? "bg-destructive" : "bg-emerald-500",
                  )}
                  style={{ width: `${progressPct}%` }}
                />
              </div>
              <span className="text-[11px] tabular-nums text-muted-foreground">
                {MONEY.format(Math.min(amount, saldo))} / {MONEY.format(saldo)}
              </span>
            </div>
          )}
        </div>
      )}

      <div className="mt-3 grid grid-cols-[minmax(0,220px)_1fr] items-start gap-x-3 gap-y-1">
        <div className="flex flex-col gap-1">{amountSlot}</div>
        <div className="flex flex-col gap-0.5 pt-1 text-xs">
          {exceeds ? (
            <span className="font-medium text-destructive">
              Supera el saldo pendiente por {MONEY.format(amount - (saldo ?? 0))}.
            </span>
          ) : remaining != null && amount > 0 && remaining > 0.0049 ? (
            <span className="text-muted-foreground">
              Saldo restante en {docLabel}: {MONEY.format(remaining)}.
            </span>
          ) : null}
        </div>
      </div>
    </div>
  );
}

interface AllocationSummaryStripProps {
  asignado: number;
  montoTotal: number;
}

/**
 * Sticky footer strip comparing the allocated sum against the payment total.
 * Green when they match, red otherwise.
 */
export function AllocationSummaryStrip({
  asignado,
  montoTotal,
}: AllocationSummaryStripProps) {
  const diff = asignado - montoTotal;
  const match = Math.abs(diff) <= 0.01;
  return (
    <div
      className={cn(
        "sticky bottom-0 z-10 mt-2 flex flex-wrap items-center justify-between gap-x-4 gap-y-1 rounded-lg border px-3 py-2 text-sm font-medium shadow-sm backdrop-blur sm:col-span-2",
        match
          ? "border-emerald-500/40 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400"
          : "border-destructive/50 bg-destructive/10 text-destructive",
      )}
    >
      <span>
        Asignado: {MONEY.format(asignado)} de {MONEY.format(montoTotal)}
      </span>
      <span>
        {match
          ? "Asignaciones cuadradas"
          : diff < 0
            ? `Faltan asignar ${MONEY.format(-diff)}`
            : `Sobran ${MONEY.format(diff)}`}
      </span>
    </div>
  );
}
