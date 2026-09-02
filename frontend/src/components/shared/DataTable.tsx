"use client";

import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
} from "@tanstack/react-table";
import { AlertTriangle, ChevronLeft, ChevronRight, Inbox, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
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
import { cn } from "@/lib/utils";

export interface DataTablePagination {
  pageIndex: number;
  pageSize: number;
  totalPages: number;
  totalCount?: number;
  onPageChange: (pageIndex: number) => void;
  /**
   * Optional. When provided, changing the page size in the built-in selector
   * delegates to the parent (which should refetch with the new size and reset
   * to the first page). Without it, the selector slices the currently loaded
   * rows client-side.
   */
  onPageSizeChange?: (pageSize: number) => void;
}

interface DataTableProps<T> {
  columns: ColumnDef<T, unknown>[];
  data: T[];
  loading?: boolean;
  error?: string | null;
  emptyMessage?: string;
  pagination?: DataTablePagination;
   actions?: (row: T) => ReactNode;
   /** Optional action buttons (e.g. refresh/create) rendered to the LEFT of the search input in the toolbar. */
   toolbarActions?: ReactNode;
   className?: string;
  /** Built-in global search box above the table. Default: true. */
  searchable?: boolean;
  /**
   * Controlled (server-side) search mode. When provided, the built-in search
   * input does NOT filter rows locally; it debounces (~300ms) and calls this
   * callback. The parent is responsible for refetching/filtering and for
   * resetting to the first page.
   */
  onSearchChange?: (term: string) => void;
  /**
   * Total number of rows on the server (controlled mode). When provided,
   * the footer shows "X de Y filas" (loaded vs total).
   */
  totalRows?: number;
  /** Options for the page-size selector. Pass null to hide it. Default: [10, 20, 50, 100]. */
  pageSizeOptions?: number[] | null;
}

const SKELETON_ROWS = 6;

/**
 * Global search filters the CURRENTLY LOADED rows client-side. For
 * server-paginated tables, pass `onSearchChange` to switch to controlled
 * (server-side) search mode so the term covers the totality of records.
 */
function matchRow<T>(row: T, columns: ColumnDef<T, unknown>[], term: string): boolean {
  return columns.some((col) => {
    const def = col as ColumnDef<T, unknown> & {
      accessorKey?: string;
      accessorFn?: (row: T, index: number) => unknown;
    };
    let value: unknown;
    if (def.accessorFn) value = def.accessorFn(row, 0);
    else if (def.accessorKey) value = (row as Record<string, unknown>)[def.accessorKey];
    else return false;
    if (value === null || value === undefined) return false;
    return String(value).toLowerCase().includes(term);
  });
}

export default function DataTable<T>({
  columns,
  data,
  loading = false,
  error = null,
  emptyMessage = "No hay registros.",
   pagination,
   actions,
   toolbarActions,
   className,
  searchable = true,
  onSearchChange,
  totalRows,
  pageSizeOptions = [10, 20, 50, 100],
}: DataTableProps<T>) {
  const controlledSearch = typeof onSearchChange === "function";
  const [internalPage, setInternalPage] = useState(0);
  const [searchInput, setSearchInput] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [localPageSize, setLocalPageSize] = useState<number | null>(null);
  const [highlightIndex, setHighlightIndex] = useState<number | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const tableWrapRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, []);

  const handleSearchChange = (value: string) => {
    setSearchInput(value);
    // Reset to the first page as soon as the user starts a new search.
    if (pagination) pagination.onPageChange(0);
    else setInternalPage(0);
    setHighlightIndex(null);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(
      () => {
        if (controlledSearch) onSearchChange(value.trim());
        else setSearchTerm(value.trim().toLowerCase());
      },
      controlledSearch ? 300 : 200,
    );
  };

  const handleSearchKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (!searchInput.trim() || displayData.length === 0) return;
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setHighlightIndex((prev) =>
        prev === null ? 0 : Math.min(prev + 1, displayData.length - 1),
      );
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setHighlightIndex((prev) => (prev === null ? 0 : Math.max(prev - 1, 0)));
    } else if (e.key === "Enter") {
      if (highlightIndex === null || !actions) return;
      e.preventDefault();
      const row = tableWrapRef.current?.querySelector("tbody")?.rows[highlightIndex];
      row?.querySelector<HTMLButtonElement>("button")?.click();
    }
  };

  const filteredData = useMemo(() => {
    if (controlledSearch || !searchTerm) return data;
    return data.filter((row) => matchRow(row, columns, searchTerm));
  }, [data, columns, searchTerm, controlledSearch]);

  // Local mode: no server pagination and no controlled search — everything
  // (filtering, slicing, paging) happens client-side.
  const isLocalMode = !pagination && !controlledSearch;
  const basePageSize = pagination?.pageSize ?? (data.length || 10);
  const effectivePageSize = isLocalMode
    ? (localPageSize ?? 10)
    : (localPageSize ?? basePageSize);
  const options = pageSizeOptions ?? [];
  const parentControlsPageSize = Boolean(pagination?.onPageSizeChange);

  const localPageCount = Math.max(
    1,
    Math.ceil(filteredData.length / effectivePageSize),
  );
  const safePage = Math.min(internalPage, localPageCount - 1);

  let displayData = filteredData;
  if (!parentControlsPageSize) {
    if (isLocalMode) {
      const offset = safePage * effectivePageSize;
      displayData = filteredData.slice(offset, offset + effectivePageSize);
    } else if (localPageSize !== null) {
      const offset = pagination ? 0 : internalPage * localPageSize;
      displayData = filteredData.slice(offset, offset + localPageSize);
    }
  }

  const table = useReactTable({
    data: displayData,
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    pageCount: pagination?.totalPages ?? -1,
    state: {
      pagination: {
        pageIndex: pagination?.pageIndex ?? internalPage,
        pageSize:
          effectivePageSize || displayData.length || basePageSize || 10,
      },
    },
    onPaginationChange: (updater) => {
      const next =
        typeof updater === "function"
          ? updater({
              pageIndex: pagination?.pageIndex ?? internalPage,
              pageSize: effectivePageSize || basePageSize || 10,
            })
          : updater;
      if (pagination) {
        pagination.onPageChange(next.pageIndex);
      } else {
        setInternalPage(next.pageIndex);
      }
    },
  });

  const handlePageSizeChange = (value: string) => {
    const size = Number(value);
    if (!Number.isFinite(size) || size <= 0) return;
    setLocalPageSize(size);
    // Reset to the first page whenever the page size changes.
    if (pagination) {
      pagination.onPageSizeChange?.(size);
      pagination.onPageChange(0);
    } else {
      setInternalPage(0);
    }
  };

  const showSearch = searchable;
  const showSizeSelector = options.length > 0;
  const searching = searchTerm.length > 0;
  const rowCountLabel = controlledSearch && totalRows !== undefined
    ? `${data.length} de ${totalRows} filas`
    : searching
      ? `${filteredData.length} de ${data.length} filas`
      : `${filteredData.length} fila${filteredData.length === 1 ? "" : "s"}`;

  return (
    <div
      className={cn(
        "overflow-hidden rounded-xl border border-border bg-card shadow-sm",
        className,
      )}
    >
      {(showSearch || showSizeSelector || toolbarActions) && (
        <div className="flex flex-wrap items-center gap-2 border-b border-border px-4 py-2.5">
          {toolbarActions && (
            <div className="flex flex-wrap items-center gap-2">{toolbarActions}</div>
          )}
          {showSearch && (
            <div className="relative w-full max-w-xs shrink-0">
              <Search
                className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
                aria-hidden="true"
              />
              <Input
                type="search"
                value={searchInput}
                onChange={(e) => handleSearchChange(e.target.value)}
                onKeyDown={handleSearchKeyDown}
                placeholder="Buscar…"
                aria-label="Buscar en la tabla"
                className="h-8 pl-8"
              />
            </div>
          )}
          {showSizeSelector && (
            <Select
              value={
                options.includes(effectivePageSize)
                  ? String(effectivePageSize)
                  : undefined
              }
              onValueChange={handlePageSizeChange}
            >
              <SelectTrigger
                className="h-8 w-[130px]"
                aria-label="Filas por página"
              >
                <SelectValue placeholder={`${effectivePageSize} filas`} />
              </SelectTrigger>
              <SelectContent>
                {options.map((option) => (
                  <SelectItem key={option} value={String(option)}>
                    {option} filas
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
          <span className="ml-auto text-xs text-muted-foreground">{rowCountLabel}</span>
        </div>
      )}

      <div
        ref={tableWrapRef}
        className="overflow-x-auto"
        onMouseDown={() => setHighlightIndex(null)}
      >
        <Table>
          <TableHeader className="sticky top-0 z-10 bg-muted/60 backdrop-blur">
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id} className="hover:bg-transparent border-b-border">
                {headerGroup.headers.map((header) => (
                  <TableHead
                    key={header.id}
                    className="h-9 px-3 py-2 text-[11px] font-semibold uppercase tracking-[0.04em] text-muted-foreground whitespace-nowrap"
                  >
                    {header.isPlaceholder
                      ? null
                      : flexRender(header.column.columnDef.header, header.getContext())}
                  </TableHead>
                ))}
                {actions && (
                  <TableHead className="h-9 px-3 py-2 text-right text-[11px] font-semibold uppercase tracking-[0.04em] text-muted-foreground">
                    Acciones
                  </TableHead>
                )}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: Math.min(SKELETON_ROWS, Math.max(data.length, 1)) }).map(
                (_, rowIndex) => (
                  <TableRow key={`skeleton-${rowIndex}`} className="hover:bg-transparent">
                    {Array.from({ length: columns.length + (actions ? 1 : 0) }).map(
                      (_, colIndex) => (
                        <TableCell key={colIndex} className="px-3 py-2.5">
                          <Skeleton className="h-4 w-full max-w-32" />
                        </TableCell>
                      ),
                    )}
                  </TableRow>
                ),
              )
            ) : error ? (
              <TableRow className="hover:bg-transparent">
                <TableCell colSpan={columns.length + (actions ? 1 : 0)} className="py-12">
                  <div className="flex flex-col items-center gap-2 text-center">
                    <AlertTriangle className="size-8 text-destructive" aria-hidden="true" />
                    <p className="text-sm font-medium text-destructive">{error}</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : displayData.length === 0 ? (
              <TableRow className="hover:bg-transparent">
                <TableCell colSpan={columns.length + (actions ? 1 : 0)} className="py-12">
                  <div className="flex flex-col items-center gap-2 text-center">
                    <Inbox className="size-8 text-muted-foreground/50" aria-hidden="true" />
                    <p className="text-sm text-muted-foreground">{emptyMessage}</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  className={cn(
                    "transition-colors",
                    searchInput.trim() && highlightIndex === row.index && "bg-accent",
                  )}
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id} className="px-3 py-2.5 text-sm">
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                  {actions && (
                    <TableCell className="px-3 py-2.5 text-right">
                      <div className="flex justify-end gap-1.5">{actions(row.original)}</div>
                    </TableCell>
                  )}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {(pagination || isLocalMode) && !loading && !error && (
        <div className="relative flex items-center justify-center border-t border-border px-4 py-3">
          <div className="flex items-center gap-2">
            {(() => {
              const currentPage = pagination?.pageIndex ?? safePage;
              const pageCount = pagination
                ? Math.max(1, pagination.totalPages)
                : localPageCount;
              const goTo = (page: number) => {
                if (pagination) pagination.onPageChange(page);
                else setInternalPage(page);
              };
              return (
                <>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={currentPage <= 0 || loading}
                    onClick={() => goTo(currentPage - 1)}
                  >
                    <ChevronLeft className="size-4" />
                    Anterior
                  </Button>
                  <span className="text-xs text-muted-foreground">
                    Página {currentPage + 1} de {pageCount}
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={currentPage >= pageCount - 1 || loading}
                    onClick={() => goTo(currentPage + 1)}
                  >
                    Siguiente
                    <ChevronRight className="size-4" />
                  </Button>
                </>
              );
            })()}
          </div>
          <span className="absolute right-4 text-xs text-muted-foreground">
            {pagination
              ? `${pagination.totalCount ?? 0} registro${
                  (pagination.totalCount ?? 0) === 1 ? "" : "s"
                }`
              : rowCountLabel}
          </span>
        </div>
      )}
    </div>
  );
}
