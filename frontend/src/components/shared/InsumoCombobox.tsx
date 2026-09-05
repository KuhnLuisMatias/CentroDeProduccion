"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { Search } from "lucide-react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

export interface InsumoComboboxOption {
  id: string;
  nombre: string;
  codigoSku?: string | null;
  unidadCompra?: { nombre: string; simbolo: string } | null;
  unidadConsumo?: { nombre: string; simbolo: string } | null;
  presentacion?: number | null;
}

interface InsumoComboboxProps {
  insumos: InsumoComboboxOption[];
  value: string;
  onChange: (insumoId: string) => void;
  placeholder?: string;
  ariaLabel?: string;
  id?: string;
}

function formatPresentacion(i: InsumoComboboxOption) {
  if (!i.presentacion || i.presentacion === 1) return null;
  const simbolo = i.unidadConsumo?.simbolo ?? "";
  return `Pres.: ${i.presentacion}${simbolo ? ` ${simbolo}` : ""}`;
}

function matchesQuery(i: InsumoComboboxOption, q: string) {
  const nombre = i.nombre.toLowerCase();
  const sku = (i.codigoSku ?? "").toLowerCase();
  const unidad = `${i.unidadCompra?.nombre ?? ""} ${i.unidadCompra?.simbolo ?? ""}`.toLowerCase();
  const pres = i.presentacion != null ? String(i.presentacion) : "";
  return nombre.includes(q) || sku.includes(q) || unidad.includes(q) || (pres !== "" && pres.includes(q));
}

/**
 * Combobox con búsqueda por teclado + navegación ↑ ↓ + Enter.
 * Muestra nombre (que ya incluye la presentación, ej. "x2.75Kg")
 * junto a la unidad de compra.
 */
export default function InsumoCombobox({
  insumos,
  value,
  onChange,
  placeholder = "Buscar insumo por nombre o SKU…",
  ariaLabel = "Buscar insumo",
  id,
}: InsumoComboboxProps) {
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);
  const listRef = useRef<HTMLDivElement>(null);
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return insumos;
    return insumos.filter((i) => matchesQuery(i, q));
  }, [insumos, query]);

  useEffect(() => {
    if (!open) return;
    const el = listRef.current?.querySelector<HTMLElement>('[data-active="true"]');
    el?.scrollIntoView({ block: "nearest" });
  }, [activeIndex, open]);

  useEffect(() => {
    return () => {
      if (closeTimer.current) clearTimeout(closeTimer.current);
    };
  }, []);

  const selected = insumos.find((i) => i.id === value) ?? null;
  const selectedPres = selected ? formatPresentacion(selected) : null;
  const selectedLabel = selected
    ? selectedPres
      ? `${selected.nombre} (${selectedPres.replace("Pres.: ", "x ")})`
      : selected.nombre
    : "";

  const commit = (insumoId: string) => {
    onChange(insumoId);
    setOpen(false);
    setQuery("");
  };

  return (
    <div className="relative">
      <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
      <Input
        id={id}
        className="pl-8"
        placeholder={placeholder}
        value={open ? query : selectedLabel}
        onFocus={() => {
          if (closeTimer.current) clearTimeout(closeTimer.current);
          setOpen(true);
          setQuery("");
          setActiveIndex(0);
        }}
        onChange={(e) => {
          setOpen(true);
          setQuery(e.target.value);
          setActiveIndex(0);
        }}
        onKeyDown={(e) => {
          if (e.key === "ArrowDown" || e.key === "ArrowUp") {
            e.preventDefault();
            if (!open) {
              setOpen(true);
              return;
            }
            if (filtered.length === 0) return;
            setActiveIndex((prev) =>
              e.key === "ArrowDown"
                ? (prev + 1) % filtered.length
                : (prev - 1 + filtered.length) % filtered.length,
            );
          } else if (e.key === "Enter") {
            if (open && filtered.length > 0) {
              e.preventDefault();
              commit(filtered[Math.min(activeIndex, filtered.length - 1)].id);
            }
          } else if (e.key === "Escape") {
            e.preventDefault();
            setOpen(false);
            setQuery("");
          }
        }}
        onBlur={() => {
          // Diferido para permitir el click (onMouseDown) en las opciones.
          closeTimer.current = setTimeout(() => setOpen(false), 100);
        }}
        autoComplete="off"
        role="combobox"
        aria-expanded={open}
        aria-autocomplete="list"
        aria-label={ariaLabel}
      />
      {open && (
        <div
          ref={listRef}
          role="listbox"
          className="absolute z-20 mt-1 max-h-48 w-full overflow-y-auto rounded-md border border-border bg-popover shadow-md"
        >
          {filtered.length === 0 ? (
            <p className="px-3 py-4 text-center text-sm text-muted-foreground">Sin resultados.</p>
          ) : (
            filtered.map((i, idx) => {
              const active = idx === activeIndex;
              return (
                <button
                  key={i.id}
                  type="button"
                  role="option"
                  aria-selected={i.id === value}
                  data-active={active}
                  onMouseDown={(e) => {
                    e.preventDefault();
                    commit(i.id);
                  }}
                  onMouseEnter={() => setActiveIndex(idx)}
                  className={cn(
                    "flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-sm",
                    active ? "bg-accent text-accent-foreground" : "hover:bg-muted/60",
                  )}
                >
                  <span className="min-w-0">
                    <span className="block truncate">{i.nombre}</span>
                    {(() => {
                      const pres = formatPresentacion(i);
                      if (!i.codigoSku && !pres) return null;
                      return (
                        <span className="block truncate text-xs text-muted-foreground">
                          {i.codigoSku ? `SKU: ${i.codigoSku}` : null}
                          {i.codigoSku && pres ? " · " : null}
                          {pres}
                        </span>
                      );
                    })()}
                  </span>
                  <span className="shrink-0 text-xs text-muted-foreground">
                    {i.unidadCompra
                      ? `${i.unidadCompra.nombre} (${i.unidadCompra.simbolo})`
                      : ""}
                  </span>
                </button>
              );
            })
          )}
        </div>
      )}
    </div>
  );
}
