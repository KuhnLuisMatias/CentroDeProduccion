"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { Search } from "lucide-react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

export interface SearchComboboxOption {
  id: string;
  /** Texto principal. */
  label: string;
  /** Segunda línea (ej. SKU, fecha de elaboración). */
  sublabel?: string | null;
  /** Texto a la derecha (ej. stock, unidad). */
  meta?: string | null;
  /** Texto extra para la búsqueda (no visible). */
  keywords?: string | null;
}

interface SearchComboboxProps {
  options: SearchComboboxOption[];
  value: string;
  onChange: (id: string) => void;
  placeholder?: string;
  ariaLabel?: string;
  id?: string;
  emptyText?: string;
  /**
   * Opcional: texto gris a la derecha dentro del input (ej. presentación del
   * insumo). Solo visible con la lista cerrada; se oculta al filtrar.
   */
  suffix?: string | null;
  /**
   * Opcional: si se escribe texto libre y se sale sin elegir opción (blur/Tab),
   * se llama con el texto para conservarlo (ej. lote manual). No cambia el id.
   */
  onFreeText?: (text: string) => void;
}

function matchesQuery(o: SearchComboboxOption, q: string): boolean {
  const haystack = `${o.label} ${o.sublabel ?? ""} ${o.meta ?? ""} ${o.keywords ?? ""}`.toLowerCase();
  return haystack.includes(q);
}

/**
 * Combobox genérico con búsqueda por teclado + navegación ↑ ↓ + Enter.
 * Misma interacción que InsumoCombobox, con opciones label/sublabel/meta.
 */
export default function SearchCombobox({
  options,
  value,
  onChange,
  placeholder = "Buscar…",
  ariaLabel = "Buscar",
  id,
  emptyText = "Sin resultados.",
  suffix = null,
  onFreeText,
}: SearchComboboxProps) {
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);
  const listRef = useRef<HTMLDivElement>(null);
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const queryRef = useRef("");
  const freeTextRef = useRef(onFreeText);
  useEffect(() => {
    freeTextRef.current = onFreeText;
  }, [onFreeText]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return options;
    return options.filter((o) => matchesQuery(o, q));
  }, [options, query]);

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

  const selected = options.find((o) => o.id === value) ?? null;

  const commit = (optionId: string) => {
    onChange(optionId);
    setOpen(false);
    setQuery("");
    queryRef.current = "";
  };

  const showSuffix = Boolean(suffix) && !open;

  return (
    <div className="relative min-w-0">
      <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
      <Input
        id={id}
        className={cn("pl-8", showSuffix && "pr-24")}
        placeholder={placeholder}
        value={open ? query : (selected?.label ?? "")}
        onFocus={() => {
          if (closeTimer.current) clearTimeout(closeTimer.current);
          setOpen(true);
          setQuery("");
          queryRef.current = "";
          setActiveIndex(0);
        }}
        onChange={(e) => {
          setOpen(true);
          setQuery(e.target.value);
          queryRef.current = e.target.value;
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
            queryRef.current = "";
          }
        }}
        onBlur={() => {
          // Diferido para permitir el click (onMouseDown) en las opciones.
          closeTimer.current = setTimeout(() => setOpen(false), 100);
          const typed = queryRef.current.trim();
          queryRef.current = "";
          if (typed) freeTextRef.current?.(typed);
        }}
        autoComplete="off"
        role="combobox"
        aria-expanded={open}
        aria-autocomplete="list"
        aria-label={ariaLabel}
      />
      {showSuffix && (
        <span
          aria-hidden="true"
          className="pointer-events-none absolute right-2.5 top-1/2 max-w-[45%] -translate-y-1/2 truncate text-xs text-muted-foreground"
        >
          {suffix}
        </span>
      )}
      {open && (
        <div
          ref={listRef}
          role="listbox"
          className="absolute z-20 mt-1 max-h-48 w-full overflow-y-auto rounded-md border border-border bg-popover shadow-md"
        >
          {filtered.length === 0 ? (
            <p className="px-3 py-4 text-center text-sm text-muted-foreground">{emptyText}</p>
          ) : (
            filtered.map((o, idx) => {
              const active = idx === activeIndex;
              return (
                <button
                  key={o.id}
                  type="button"
                  role="option"
                  aria-selected={o.id === value}
                  data-active={active}
                  onMouseDown={(e) => {
                    e.preventDefault();
                    commit(o.id);
                  }}
                  onMouseEnter={() => setActiveIndex(idx)}
                  className={cn(
                    "flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-sm",
                    active ? "bg-accent text-accent-foreground" : "hover:bg-muted/60",
                  )}
                >
                  <span className="min-w-0">
                    <span className="block truncate">{o.label}</span>
                    {o.sublabel ? (
                      <span className="block truncate text-xs text-muted-foreground">
                        {o.sublabel}
                      </span>
                    ) : null}
                  </span>
                  {o.meta ? (
                    <span className="shrink-0 text-xs text-muted-foreground">{o.meta}</span>
                  ) : null}
                </button>
              );
            })
          )}
        </div>
      )}
    </div>
  );
}
