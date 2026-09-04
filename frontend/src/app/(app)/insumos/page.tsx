"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { Plus, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type {
  Categoria,
  Insumo,
  PagedResult,
  Proveedor,
  UnidadMedida,
  CreateInsumoCommand,
  UpdateInsumoCommand,
} from "@/lib/types";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { CurrencyInput } from "@/components/ui/currency-input";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
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
import { Skeleton } from "@/components/ui/skeleton";

const PAGE_SIZE = 20;

const insumoSchema = z.object({
  nombre: z
    .string()
    .trim()
    .min(1, "El nombre es obligatorio.")
    .max(200, "Máximo 200 caracteres."),
  codigoSku: z
    .string()
    .trim()
    .min(1, "El código SKU es obligatorio.")
    .max(50, "Máximo 50 caracteres."),
  categoriaId: z.string().min(1, "Seleccioná una categoría."),
  unidadCompraId: z.string().min(1, "Seleccioná la unidad de compra."),
  stockMinimo: z.coerce
    .number({ message: "Ingresá un número válido." })
    .min(0, "No puede ser negativo."),
  precioUltimaCompra: z.coerce
    .number({ message: "Ingresá un número válido." })
    .min(0, "No puede ser negativo."),
  proveedorPrincipalId: z.string(),
  observaciones: z.string().max(1000, "Máximo 1000 caracteres."),
});

type InsumoFormInput = z.input<typeof insumoSchema>;
type InsumoFormValues = z.output<typeof insumoSchema>;

const EMPTY_FORM: InsumoFormValues = {
  nombre: "",
  codigoSku: "",
  categoriaId: "",
  unidadCompraId: "",
  stockMinimo: 0,
  precioUltimaCompra: 0,
  proveedorPrincipalId: "",
  observaciones: "",
};

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function InsumosPage() {
  const [rows, setRows] = useState<Insumo[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState("");

  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [unidades, setUnidades] = useState<UnidadMedida[]>([]);
  const [proveedores, setProveedores] = useState<Proveedor[]>([]);
  const [refsLoaded, setRefsLoaded] = useState(false);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<Insumo | null>(null);
  const [estado, setEstado] = useState<"activo" | "inactivo">("activo");

  const load = useCallback(async () => {
    try {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
        includeInactive: "true",
      });
      if (search) params.set("search", search);
      const result = await apiClient<PagedResult<Insumo>>(`/insumos?${params.toString()}`);
      setRows(result.items);
      setTotalCount(result.totalCount);
      setTotalPages(result.totalPages);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar los insumos.");
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, search]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    let cancelled = false;
    async function loadRefs() {
      try {
        const [cat, uni, prov] = await Promise.all([
          apiClient<Categoria[]>("/categorias?ambito=1"),
          apiClient<UnidadMedida[]>("/unidadesmedida"),
          apiClient<Proveedor[]>("/proveedores"),
        ]);
        if (cancelled) return;
        setCategorias(cat);
        setUnidades(uni);
        setProveedores(prov);
      } catch {
        // refs are optional for the list view
      } finally {
        if (!cancelled) setRefsLoaded(true);
      }
    }
    loadRefs();
    return () => {
      cancelled = true;
    };
  }, []);

  const form = useForm<InsumoFormInput, unknown, InsumoFormValues>({
    resolver: zodResolver(insumoSchema),
    defaultValues: EMPTY_FORM,
  });

  const openCreate = () => {
    setEditing(null);
    setEstado("activo");
    form.reset(EMPTY_FORM);
    setDialogOpen(true);
  };

  const openEdit = (row: Insumo) => {
    setEditing(row);
    setEstado(row.activo ? "activo" : "inactivo");
    form.reset({
      nombre: row.nombre,
      codigoSku: row.codigoSku,
      categoriaId: row.categoriaId,
      unidadCompraId: row.unidadCompraId,
      stockMinimo: row.stockMinimo,
      precioUltimaCompra: row.precioUltimaCompra,
      proveedorPrincipalId: row.proveedorPrincipalId ?? "",
      observaciones: row.observaciones ?? "",
    });
    setDialogOpen(true);
  };

  const handleSave = form.handleSubmit(async (values) => {
    // Unidad de consumo is DERIVED: same as unidad de compra (factor 1:1).
    const base: CreateInsumoCommand = {
      nombre: values.nombre.trim(),
      codigoSku: values.codigoSku.trim(),
      categoriaId: values.categoriaId,
      unidadCompraId: values.unidadCompraId,
      unidadConsumoId: values.unidadCompraId,
      factorConversion: 1,
      stockMinimo: values.stockMinimo,
      precioUltimaCompra: values.precioUltimaCompra || null,
      proveedorPrincipalId: values.proveedorPrincipalId || null,
      observaciones: values.observaciones.trim() || null,
    };
    try {
      if (editing) {
        const payload: UpdateInsumoCommand = { ...base, id: editing.id, rowVersion: editing.rowVersion };
        await apiClient<void>(`/insumos/${editing.id}`, { method: "PUT", body: payload });
        if (estado === "inactivo" && editing.activo) {
          await apiClient<void>(`/insumos/${editing.id}`, { method: "DELETE" });
        } else if (estado === "activo" && !editing.activo) {
          await apiClient<void>(`/insumos/${editing.id}/reactivar`, { method: "POST" });
        }
        toast.success(`Insumo "${base.nombre}" actualizado.`);
      } else {
        await apiClient<unknown>("/insumos", { method: "POST", body: base });
        toast.success(`Insumo "${base.nombre}" creado.`);
      }
      setDialogOpen(false);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo guardar el insumo.");
    }
  });

  const handleSearchChange = (term: string) => {
    setPage(1);
    setSearch(term);
  };

  const handlePageSizeChange = (size: number) => {
    setPage(1);
    setPageSize(size);
  };

  const columns: ColumnDef<Insumo, unknown>[] = [
    { accessorKey: "nombre", header: "Nombre" },
    { accessorKey: "codigoSku", header: "SKU" },
    {
      id: "categoria",
      header: "Categoría",
      cell: ({ row }) => row.original.categoria?.nombre ?? "—",
    },
    {
      accessorKey: "stockActual",
      header: "Stock",
      cell: ({ row }) => (
        <span
          className={
            row.original.stockActual <= row.original.stockMinimo
              ? "font-medium text-red-600"
              : undefined
          }
        >
          {row.original.stockActual}
        </span>
      ),
    },
    {
      accessorKey: "precioUltimaCompra",
      header: "Precio últ. compra",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
    },
    {
      accessorKey: "activo",
      header: "Estado",
      cell: ({ row }) =>
        row.original.activo ? (
          <Badge variant="outline" className="bg-emerald-100 text-emerald-700 hover:bg-emerald-100">
            Activo
          </Badge>
        ) : (
          <Badge variant="outline" className="bg-red-100 text-red-700 hover:bg-red-100">
            Inactivo
          </Badge>
        ),
    },
  ];

  const { register, watch, setValue, control, formState: { errors } } = form;

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nuevo insumo" title="Nuevo insumo">
              <Plus className="size-5" />
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>          </>
        }
      />

      <DataTable
        columns={columns}
        data={rows}
        loading={loading}
        error={error}
        emptyMessage={search ? `No hay insumos para "${search}".` : "No hay insumos."}
        actions={(row) => (
          <Button variant="outline" size="sm" onClick={() => openEdit(row)}>
            Editar
          </Button>
        )}
        pagination={{
          pageIndex: page - 1,
          pageSize,
          totalPages,
          totalCount,
          onPageChange: (pageIndex) => setPage(pageIndex + 1),
          onPageSizeChange: handlePageSizeChange,
        }}
        onSearchChange={handleSearchChange}
        totalRows={totalCount}
      />

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{editing ? "Editar insumo" : "Nuevo insumo"}</DialogTitle>
            <DialogDescription>
              {editing
                ? "Modificá los datos del insumo y guardá los cambios."
                : "Completá los datos para dar de alta un nuevo insumo."}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="insumo-nombre">Nombre</Label>
              <Input id="insumo-nombre" {...register("nombre")} />
              <FieldError message={errors.nombre?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="insumo-codigoSku">Código SKU</Label>
              <Input id="insumo-codigoSku" {...register("codigoSku")} />
              <FieldError message={errors.codigoSku?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="insumo-categoria">Categoría</Label>
              <Controller
                control={control}
                name="categoriaId"
                render={({ field }) =>
                  refsLoaded ? (
                    <Select value={field.value} onValueChange={field.onChange}>
                      <SelectTrigger id="insumo-categoria" className="w-full">
                        <SelectValue placeholder="Seleccionar…" />
                      </SelectTrigger>
                      <SelectContent>
                        {categorias.map((c) => (
                          <SelectItem key={c.id} value={c.id}>
                            {c.nombre}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  ) : (
                    <Skeleton className="h-9 w-full" />
                  )
                }
              />
              <FieldError message={errors.categoriaId?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="insumo-proveedor">Proveedor principal</Label>
              <Controller
                control={control}
                name="proveedorPrincipalId"
                render={({ field }) =>
                  refsLoaded ? (
                    <Select value={field.value} onValueChange={field.onChange}>
                      <SelectTrigger id="insumo-proveedor" className="w-full">
                        <SelectValue placeholder="Sin proveedor" />
                      </SelectTrigger>
                      <SelectContent>
                        {proveedores.map((p) => (
                          <SelectItem key={p.id} value={p.id}>
                            {p.nombreRazonSocial}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  ) : (
                    <Skeleton className="h-9 w-full" />
                  )
                }
              />
              <FieldError message={errors.proveedorPrincipalId?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="insumo-unidadCompra">Unidad de compra</Label>
              <p className="text-xs text-muted-foreground">
                Se usa también como unidad de consumo (1:1).
              </p>
              <Controller
                control={control}
                name="unidadCompraId"
                render={({ field }) =>
                  refsLoaded ? (
                    <Select value={field.value} onValueChange={field.onChange}>
                      <SelectTrigger id="insumo-unidadCompra" className="w-full">
                        <SelectValue placeholder="Seleccionar…" />
                      </SelectTrigger>
                      <SelectContent>
                        {unidades.map((u) => (
                          <SelectItem key={u.id} value={u.id}>
                            {u.nombre} ({u.simbolo})
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  ) : (
                    <Skeleton className="h-9 w-full" />
                  )
                }
              />
              <FieldError message={errors.unidadCompraId?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="insumo-stockMinimo">Stock mínimo</Label>
              <Input id="insumo-stockMinimo" type="number" step="any" min="0" {...register("stockMinimo")} />
              <FieldError message={errors.stockMinimo?.message} />
            </div>

            {editing && (
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="insumo-estado">Estado</Label>
                <Select value={estado} onValueChange={(v) => setEstado(v as "activo" | "inactivo")}>
                  <SelectTrigger id="insumo-estado" className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="activo">Activo</SelectItem>
                    <SelectItem value="inactivo">Inactivo</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="insumo-precioUltimaCompra">Precio última compra</Label>
              <CurrencyInput
                value={String(watch("precioUltimaCompra") ?? "")}
                onChange={(v) => setValue("precioUltimaCompra", v)}
              />
              <FieldError message={errors.precioUltimaCompra?.message} />
            </div>

            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label htmlFor="insumo-observaciones">Observaciones</Label>
              <Textarea id="insumo-observaciones" rows={3} {...register("observaciones")} />
              <FieldError message={errors.observaciones?.message} />
            </div>

            <DialogFooter className="sm:col-span-2">
              <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={form.formState.isSubmitting}>
                {form.formState.isSubmitting ? "Guardando…" : "Guardar"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

    </div>
  );
}
