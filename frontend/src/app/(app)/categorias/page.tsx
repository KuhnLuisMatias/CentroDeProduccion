"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { Plus, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api";
import type {
  AmbitoCategoria,
  Categoria,
  CategoriasGrouped,
  CreateCategoriaCommand,
  UpdateCategoriaCommand,
} from "@/lib/types";
import { AMBITO_CATEGORIA_LABELS } from "@/lib/types";
import { useAuth } from "@/context/AuthContext";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
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

type AmbitoFilter = "todos" | AmbitoCategoria;

const categoriaSchema = z.object({
  nombre: z
    .string()
    .trim()
    .min(1, "El nombre es requerido.")
    .max(100, "Máximo 100 caracteres."),
  ambito: z.enum(["1", "2"]),
});

type CategoriaFormInput = z.input<typeof categoriaSchema>;
type CategoriaFormValues = z.output<typeof categoriaSchema>;

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function CategoriasPage() {
  const { user } = useAuth();
  const isAdmin = user?.rol === "Administrador";

  const [grouped, setGrouped] = useState<CategoriasGrouped | null>(null);
  const [filter, setFilter] = useState<AmbitoFilter>("todos");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<Categoria | null>(null);
  const [editActivo, setEditActivo] = useState("true");

  const load = useCallback(async () => {
    try {
      const result = await apiClient<CategoriasGrouped>("/categorias");
      setGrouped(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar las categorías.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const rows: Categoria[] = (() => {
    if (!grouped) return [];
    if (filter === "todos") return [...grouped.insumos, ...grouped.productosTerminados];
    return filter === 1 ? grouped.insumos : grouped.productosTerminados;
  })();

  const defaultAmbito: "1" | "2" = filter === 2 ? "2" : "1";

  const form = useForm<CategoriaFormInput, unknown, CategoriaFormValues>({
    resolver: zodResolver(categoriaSchema),
    defaultValues: { nombre: "", ambito: defaultAmbito },
  });

  const openCreate = () => {
    setEditing(null);
    form.reset({ nombre: "", ambito: defaultAmbito });
    setDialogOpen(true);
  };

  const openEdit = (row: Categoria) => {
    setEditing(row);
    setEditActivo(row.activo ? "true" : "false");
    form.reset({ nombre: row.nombre, ambito: String(row.ambito) as "1" | "2" });
    setDialogOpen(true);
  };

  const handleSave = form.handleSubmit(async (values) => {
    const nombre = values.nombre.trim();
    const ambito = Number(values.ambito) as AmbitoCategoria;
    try {
      if (editing) {
        const payload: UpdateCategoriaCommand = {
          id: editing.id,
          nombre,
          ambito,
          activo: editActivo === "true",
        };
        await apiClient<void>(`/categorias/${editing.id}`, { method: "PUT", body: payload });
        toast.success(`Categoría "${nombre}" actualizada.`);
      } else {
        await apiClient<unknown>("/categorias", { method: "POST", body: { nombre, ambito } as CreateCategoriaCommand });
        toast.success(`Categoría "${nombre}" creada.`);
      }
      setDialogOpen(false);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo guardar la categoría.");
    }
  });

  const columns: ColumnDef<Categoria, unknown>[] = [
    { accessorKey: "nombre", header: "Nombre" },
    {
      accessorKey: "ambito",
      header: "Ámbito",
      cell: ({ row }) =>
        row.original.ambito === 1 ? (
          <Badge variant="outline" className="border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400">
            {AMBITO_CATEGORIA_LABELS[row.original.ambito]}
          </Badge>
        ) : (
          <Badge variant="outline" className="border-sky-600/30 bg-sky-500/10 text-sky-700 dark:text-sky-400">
            {AMBITO_CATEGORIA_LABELS[row.original.ambito]}
          </Badge>
        ),
    },
    {
      accessorKey: "activo",
      header: "Estado",
      cell: ({ row }) => (row.original.activo ? "Activo" : "Inactivo"),
    },
  ];

  const { register, control, formState: { errors } } = form;

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nueva categoría" title="Nueva categoría">
              <Plus className="size-5" />
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>          </>
        }
      />

      <div className="mb-4 flex items-center gap-2">
        <span className="text-sm text-muted-foreground">Ámbito:</span>
        <Select value={String(filter)} onValueChange={(v) => setFilter(v as AmbitoFilter)}>
          <SelectTrigger className="w-52">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="todos">Todas</SelectItem>
            <SelectItem value="1">Insumo</SelectItem>
            <SelectItem value="2">Producto Terminado</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={rows}
        loading={loading}
        error={error}
        emptyMessage="No hay categorías."
        actions={
          isAdmin
            ? (row) => (
                <Button variant="outline" size="sm" onClick={() => openEdit(row)}>
                  Editar
                </Button>
              )
            : undefined
        }
      />

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{editing ? "Editar categoría" : "Nueva categoría"}</DialogTitle>
            <DialogDescription>
              {editing
                ? "Modificá los datos de la categoría y guardá los cambios."
                : "Completá los datos para dar de alta una nueva categoría."}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="flex flex-col gap-3" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="cat-nombre">Nombre</Label>
              <Input id="cat-nombre" {...register("nombre")} />
              <FieldError message={errors.nombre?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="cat-ambito">Ámbito</Label>
              <Controller
                control={control}
                name="ambito"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="cat-ambito" className="w-full">
                      <SelectValue placeholder="Seleccionar…" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="1">Insumo</SelectItem>
                      <SelectItem value="2">Producto Terminado</SelectItem>
                    </SelectContent>
                  </Select>
                )}
              />
              <FieldError message={errors.ambito?.message} />
            </div>

            {editing && (
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="cat-estado">Estado</Label>
                <Select value={editActivo} onValueChange={setEditActivo}>
                  <SelectTrigger id="cat-estado" className="w-full">
                    <SelectValue placeholder="Seleccionar…" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="true">Activo</SelectItem>
                    <SelectItem value="false">Inactivo</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            )}

            <DialogFooter>
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
