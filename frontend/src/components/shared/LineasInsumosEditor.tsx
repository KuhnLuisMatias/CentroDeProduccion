"use client";

import { Plus, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { CurrencyInput } from "@/components/ui/currency-input";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { MONEY } from "@/lib/utils";
import InsumoCombobox, { type InsumoComboboxOption } from "@/components/shared/InsumoCombobox";

export interface LineaInsumoDraft {
  key: string;
  insumoId: string;
  cantidad: string;
  precioUnitario: string;
}

export interface LineaInsumoErrors {
  insumoId?: string;
  cantidad?: string;
  precioUnitario?: string;
}

interface LineasInsumosEditorProps {
  insumos: InsumoComboboxOption[];
  lines: LineaInsumoDraft[];
  onInsumoChange: (index: number, insumoId: string) => void;
  onCantidadChange: (index: number, value: string) => void;
  onPrecioChange: (index: number, value: string) => void;
  onAdd: () => void;
  onRemove: (index: number) => void;
  fieldErrors?: (LineaInsumoErrors | undefined)[];
  rootError?: string;
  addLabel?: string;
}

function FieldError({ message }: { message?: string }) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

/**
 * Editor de líneas insumo-cantidad-precio compartido por Facturas y Órdenes.
 * Usa divs (sin Table/TableRow) para no heredar el hover gris,
 * y no renderiza título: la sección queda sin encabezado "Insumos".
 */
export default function LineasInsumosEditor({
  insumos,
  lines,
  onInsumoChange,
  onCantidadChange,
  onPrecioChange,
  onAdd,
  onRemove,
  fieldErrors,
  rootError,
  addLabel = "Agregar insumo",
}: LineasInsumosEditorProps) {
  const total = lines.reduce(
    (s, l) =>
      s + (parseFloat(String(l.cantidad)) || 0) * (parseFloat(String(l.precioUnitario)) || 0),
    0,
  );

  return (
    <div className="flex flex-col gap-2">
      {lines.map((line, index) => {
        return (
        <div
          key={line.key}
          className="grid grid-cols-[1.6fr_0.8fr_0.9fr_auto] items-end gap-2"
        >
          <div className="flex flex-col gap-1">
            <Label className="text-xs text-muted-foreground">Insumo</Label>
            <InsumoCombobox
              insumos={insumos}
              value={line.insumoId}
              onChange={(id) => onInsumoChange(index, id)}
            />
            <FieldError message={fieldErrors?.[index]?.insumoId} />
          </div>
          <div className="flex flex-col gap-1">
            <Label className="text-xs text-muted-foreground">Cantidad</Label>
            <Input
              type="number"
              step="any"
              min="0"
              value={line.cantidad}
              onChange={(e) => onCantidadChange(index, e.target.value)}
            />
            <FieldError message={fieldErrors?.[index]?.cantidad} />
          </div>
          <div className="flex flex-col gap-1">
            <Label className="text-xs text-muted-foreground">Precio unitario</Label>
            <CurrencyInput
              value={line.precioUnitario}
              onChange={(v) => onPrecioChange(index, v)}
            />
            <FieldError message={fieldErrors?.[index]?.precioUnitario} />
          </div>
          <Button
            type="button"
            variant="destructive"
            size="icon"
            className="mb-0.5"
            onClick={() => onRemove(index)}
            aria-label="Eliminar insumo"
          >
            <Trash2 className="size-4" />
          </Button>
        </div>
        );
      })}
      <div className="flex items-center justify-between rounded-md border bg-muted/50 px-3 py-2">
        <span className="text-sm font-medium">Total de la factura</span>
        <span className="text-sm font-semibold">{MONEY.format(total)}</span>
      </div>
      <div>
        <Button type="button" variant="outline" size="sm" onClick={onAdd}>
          <Plus className="size-4" />
          {addLabel}
        </Button>
      </div>
      <FieldError message={rootError} />
    </div>
  );
}
