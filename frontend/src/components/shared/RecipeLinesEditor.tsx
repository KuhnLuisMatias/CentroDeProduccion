"use client";

import { Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

export interface RecipeLineDraft {
  id: string;
  tipo: "insumo" | "subreceta";
  insumoId: string;
  recetaOrigenId: string;
  cantidadNecesaria: string;
  unidadMedidaId: string;
}

interface RecipeLinesEditorProps {
  lines: RecipeLineDraft[];
  insumos: { id: string; nombre: string; unidadConsumoId: string }[];
  recetas: { id: string; nombre: string }[];
  unidades: { id: string; nombre: string; simbolo: string }[];
  onChange: (lines: RecipeLineDraft[]) => void;
}

function newId() {
  return `line-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
}

export default function RecipeLinesEditor({
  lines,
  insumos,
  recetas,
  unidades,
  onChange,
}: RecipeLinesEditorProps) {
  const updateLine = (id: string, patch: Partial<RecipeLineDraft>) => {
    onChange(lines.map((l) => (l.id === id ? { ...l, ...patch } : l)));
  };

  const addLine = (tipo: RecipeLineDraft["tipo"]) => {
    const first = lines[0];
    onChange([
      ...lines,
      {
        id: newId(),
        tipo,
        insumoId: "",
        recetaOrigenId: "",
        cantidadNecesaria: "1",
        unidadMedidaId: first?.unidadMedidaId ?? "",
      },
    ]);
  };

  const removeLine = (id: string) => {
    onChange(lines.filter((l) => l.id !== id));
  };

  return (
    <div className="sm:col-span-2">
      <Label>Insumos de la receta</Label>
      <div className="mt-2 flex flex-col gap-2">
        {lines.map((line, idx) => (
          <div
            key={line.id}
            className="grid grid-cols-1 items-end gap-2 rounded-lg border border-border p-3 sm:grid-cols-[1.4fr_0.7fr_0.8fr_auto]"
          >
            {line.tipo === "insumo" ? (
              <div className="flex flex-col gap-1.5">
                <Label className="text-xs text-muted-foreground">Insumo</Label>
                <Select
                  value={line.insumoId || undefined}
                  onValueChange={(v) => {
                    const insumo = insumos.find((i) => i.id === v);
                    updateLine(line.id, {
                      insumoId: v,
                      unidadMedidaId: insumo?.unidadConsumoId ?? line.unidadMedidaId,
                    });
                  }}
                >
                  <SelectTrigger className="w-full">
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
              </div>
            ) : (
              <div className="flex flex-col gap-1.5">
                <Label className="text-xs text-muted-foreground">Sub-receta</Label>
                <Select
                  value={line.recetaOrigenId || undefined}
                  onValueChange={(v) => updateLine(line.id, { recetaOrigenId: v })}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Seleccionar…" />
                  </SelectTrigger>
                  <SelectContent>
                    {recetas.map((r) => (
                      <SelectItem key={r.id} value={r.id}>
                        {r.nombre}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="flex flex-col gap-1.5">
              <Label className="text-xs text-muted-foreground">Cantidad</Label>
              <Input
                type="number"
                step="any"
                min="0"
                value={line.cantidadNecesaria}
                onChange={(e) => updateLine(line.id, { cantidadNecesaria: e.target.value })}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label className="text-xs text-muted-foreground">Unidad</Label>
              <Select
                value={line.unidadMedidaId || undefined}
                onValueChange={(v) => updateLine(line.id, { unidadMedidaId: v })}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="…" />
                </SelectTrigger>
                <SelectContent>
                  {unidades.map((u) => (
                    <SelectItem key={u.id} value={u.id}>
                      {u.simbolo || u.nombre}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <Button
              type="button"
              variant="destructive"
              size="icon"
              onClick={() => removeLine(line.id)}
              aria-label={`Eliminar línea ${idx + 1}`}
            >
              <X className="size-4" />
            </Button>
          </div>
        ))}
      </div>
      <div className="mt-2 flex gap-2">
        <Button type="button" variant="outline" size="sm" onClick={() => addLine("insumo")}>
          <Plus className="size-4" />
          Agregar insumo
        </Button>
        <Button type="button" variant="outline" size="sm" onClick={() => addLine("subreceta")}>
          <Plus className="size-4" />
          Agregar receta
        </Button>
      </div>
    </div>
  );
}
