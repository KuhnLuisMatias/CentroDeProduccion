"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import type { ColumnDef } from "@tanstack/react-table";
import { RefreshCw } from "lucide-react";
import { apiClient, ApiError } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import { ESTADO_PRODUCTO_TERMINADO_LABELS, type EstadoProductoTerminado } from "@/lib/types";
import type { ProductoTerminado, CosteoReceta, Receta } from "@/lib/types";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

const PAGE_SIZE = 20;

const ESTADO_BADGE_CLASS: Record<EstadoProductoTerminado, string> = {
  1: "bg-emerald-100 text-emerald-700 hover:bg-emerald-100",
  2: "bg-blue-100 text-blue-700 hover:bg-blue-100",
  3: "bg-amber-100 text-amber-700 hover:bg-amber-100",
  4: "bg-red-100 text-red-700 hover:bg-red-100",
};

function EstadoBadge({ estado }: { estado: number }) {
  const label =
    ESTADO_PRODUCTO_TERMINADO_LABELS[estado as EstadoProductoTerminado] ?? `Estado ${estado}`;
  const cls = ESTADO_BADGE_CLASS[estado as EstadoProductoTerminado];
  return (
    <Badge variant="outline" className={cls}>
      {label}
    </Badge>
  );
}

export default function ProductosTerminadosPage() {
  const [rows, setRows] = useState<ProductoTerminado[]>([]);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Read-only recipe detail dialog ("Ver" in the Receta column).
  const [recetaOpen, setRecetaOpen] = useState(false);
  const [receta, setReceta] = useState<Receta | null>(null);
  const [costeo, setCosteo] = useState<CosteoReceta | null>(null);
  const [recetaLoading, setRecetaLoading] = useState(false);

  const load = useCallback(async () => {
    try {
      // GET /api/productoterminado returns a plain array (not paged).
      const result = await apiClient<ProductoTerminado[]>("/productoterminado");
      setRows(result);
      setError(null);
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "No se pudieron cargar los productos terminados.",
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const filtered = useMemo(() => {
    if (!search) return rows;
    const q = search.toLowerCase();
    return rows.filter(
      (r) =>
        r.nombre.toLowerCase().includes(q) ||
        r.codigoSku.toLowerCase().includes(q) ||
        r.categoria?.nombre.toLowerCase().includes(q),
    );
  }, [rows, search]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const pagedRows = useMemo(() => {
    const safePage = Math.min(page, totalPages);
    return filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);
  }, [filtered, page, totalPages]);

  const handleSearchChange = (term: string) => {
    setPage(1);
    setSearch(term);
  };

  // Same detail rendering pattern as recetas/page.tsx: fresh GET /recetas/{id} + costeo.
  const openReceta = useCallback(async (recetaId: string) => {
    setRecetaOpen(true);
    setReceta(null);
    setCosteo(null);
    setRecetaLoading(true);
    try {
      const [det, cos] = await Promise.all([
        apiClient<Receta>(`/recetas/${recetaId}`),
        apiClient<CosteoReceta>(`/recetas/${recetaId}/costeo`).catch(() => null),
      ]);
      setReceta(det);
      setCosteo(cos);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo cargar la receta.");
    } finally {
      setRecetaLoading(false);
    }
  }, []);

  const columns: ColumnDef<ProductoTerminado, unknown>[] = [
    { accessorKey: "nombre", header: "Nombre" },
    { accessorKey: "codigoSku", header: "SKU" },
    {
      id: "categoria",
      header: "Categoría",
      cell: ({ row }) =>
        row.original.categoria?.nombre ? (
          <Badge variant="outline">{row.original.categoria.nombre}</Badge>
        ) : (
          "—"
        ),
    },
    {
      id: "receta",
      header: "Receta",
      cell: ({ row }) =>
        row.original.recetaId && row.original.receta ? (
          <Button
            variant="outline"
            size="sm"
            onClick={() => void openReceta(row.original.recetaId!)}
          >
            Ver
          </Button>
        ) : (
          "—"
        ),
    },
    {
      accessorKey: "stockActual",
      header: "Stock",
      cell: ({ row }) => (
        <span className={row.original.stockActual <= 0 ? "font-medium text-red-600" : undefined}>
          {row.original.stockActual}
        </span>
      ),
    },
    {
      id: "unidad",
      header: "Unidad de medida",
      cell: ({ row }) => {
        const u = row.original.unidadMedida;
        return u ? `${u.nombre} (${u.simbolo})` : "—";
      },
    },
    {
      accessorKey: "costoUnitario",
      header: "Costo unitario",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
    {
      accessorKey: "estado",
      header: "Estado",
      cell: ({ row }) => <EstadoBadge estado={row.original.estado} />,
    },
  ];

  return (
    <div>
      <PageHeader
        actions={
          <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
            <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
            Actualizar
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={pagedRows}
        loading={loading}
        error={error}
        emptyMessage={
          search
            ? `No hay productos terminados para "${search}".`
            : "No hay productos terminados."
        }
        pagination={{
          pageIndex: Math.min(page, totalPages) - 1,
          pageSize: PAGE_SIZE,
          totalPages,
          totalCount: filtered.length,
          onPageChange: (pageIndex) => setPage(pageIndex + 1),
        }}
        onSearchChange={handleSearchChange}
        totalRows={rows.length}
      />

      {/* Read-only recipe detail (same rendering pattern as recetas/page.tsx). */}
      <Dialog open={recetaOpen} onOpenChange={(open) => !open && setRecetaOpen(false)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Receta: {receta?.nombre ?? "—"}</DialogTitle>
            <DialogDescription>Detalle de solo lectura de la receta del producto.</DialogDescription>
          </DialogHeader>

          {recetaLoading ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Cargando receta…</p>
          ) : receta ? (
            <div className="flex flex-col gap-4">
              <Card>
                <CardHeader>
                  <CardTitle className="text-sm">Datos</CardTitle>
                </CardHeader>
                <CardContent className="grid grid-cols-1 gap-x-4 gap-y-2 text-sm sm:grid-cols-2">
                  <div>
                    <span className="font-medium">SKU:</span> {receta.codigoSku}
                  </div>
                  <div>
                    <span className="font-medium">Versión:</span> {receta.version}
                  </div>
                </CardContent>
              </Card>

              {costeo && (
                <Card>
                  <CardHeader>
                    <CardTitle className="text-sm">Costeo</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <Table>
                      <TableBody>
                        <TableRow>
                          <TableCell>Costo unitario</TableCell>
                          <TableCell className="text-left">{MONEY.format(costeo.costoUnitario)}</TableCell>
                        </TableRow>
                        {costeo.cicloDetectado && (
                          <TableRow>
                            <TableCell colSpan={2} className="text-destructive">
                              Se detectó un ciclo en la BOM
                            </TableCell>
                          </TableRow>
                        )}
                      </TableBody>
                    </Table>
                  </CardContent>
                </Card>
              )}

              <Card>
                <CardHeader>
                  <CardTitle className="text-sm">Insumos</CardTitle>
                </CardHeader>
                <CardContent>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Insumo</TableHead>
                        <TableHead>Cantidad</TableHead>
                        <TableHead>Unidad</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {receta.insumos.map((ri) => (
                        <TableRow key={ri.id}>
                          <TableCell>{ri.insumo?.nombre ?? ri.recetaOrigen?.nombre ?? "—"}</TableCell>
                          <TableCell>{ri.cantidadNecesaria}</TableCell>
                          <TableCell>{ri.unidadMedida?.simbolo ?? ""}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </CardContent>
              </Card>
            </div>
          ) : null}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setRecetaOpen(false)}>
              Cerrar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
