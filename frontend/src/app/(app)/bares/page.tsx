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
  Bar,
  BarListItem,
  CreateBarCommand,
  EstadoBar,
  UpdateBarCommand,
} from "@/lib/types";
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

type EstadoFilter = "todos" | "1" | "2";

const barSchema = z.object({
  nombre: z
    .string()
    .trim()
    .min(1, "El nombre es requerido.")
    .max(100, "Máximo 100 caracteres."),
  direccion: z
    .string()
    .trim()
    .min(1, "La dirección es requerida.")
    .max(200, "Máximo 200 caracteres."),
  encargado: z.string().trim().max(100, "Máximo 100 caracteres."),
  telefono: z.string().trim().max(20, "Máximo 20 caracteres."),
  horarioRecepcion: z.string().trim().max(100, "Máximo 100 caracteres."),
  margenReventaPorcentaje: z.coerce
    .number({ message: "Ingresá un número válido." })
    .min(0, "No puede ser negativo."),
});

type BarFormInput = z.input<typeof barSchema>;
type BarFormValues = z.output<typeof barSchema>;

const EMPTY_FORM: BarFormInput = {
  nombre: "",
  direccion: "",
  encargado: "",
  telefono: "",
  horarioRecepcion: "",
  margenReventaPorcentaje: 0,
};

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function BaresPage() {
  const [rows, setRows] = useState<BarListItem[]>([]);
  const [estado, setEstado] = useState<EstadoFilter>("todos");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<Bar | null>(null);
  const [editEstado, setEditEstado] = useState<EstadoBar>(1);

  const load = useCallback(async () => {
    try {
      const params = new URLSearchParams();
      if (estado !== "todos") params.set("estado", estado);
      const qs = params.toString();
      const result = await apiClient<BarListItem[]>(`/bares${qs ? `?${qs}` : ""}`);
      setRows(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar los bares.");
    } finally {
      setLoading(false);
    }
  }, [estado]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const fetchFull = async (id: string): Promise<Bar> => {
    return await apiClient<Bar>(`/bares/${id}`);
  };

  const form = useForm<BarFormInput, unknown, BarFormValues>({
    resolver: zodResolver(barSchema),
    defaultValues: EMPTY_FORM,
  });

  const openCreate = () => {
    setEditing(null);
    form.reset(EMPTY_FORM);
    setDialogOpen(true);
  };

  const openEdit = async (row: BarListItem) => {
    try {
      const bar = await fetchFull(row.id);
      setEditing(bar);
      setEditEstado(bar.estado);
      form.reset({
        nombre: bar.nombre,
        direccion: bar.direccion,
        encargado: bar.encargado ?? "",
        telefono: bar.telefono ?? "",
        horarioRecepcion: bar.horarioRecepcion ?? "",
        margenReventaPorcentaje: bar.margenReventaPorcentaje,
      });
      setDialogOpen(true);
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo cargar el bar.");
    }
  };

  const handleSave = form.handleSubmit(async (values) => {
    const base = {
      nombre: values.nombre.trim(),
      direccion: values.direccion.trim(),
      encargado: values.encargado.trim() || null,
      telefono: values.telefono.trim() || null,
      horarioRecepcion: values.horarioRecepcion.trim() || null,
      margenReventaPorcentaje: values.margenReventaPorcentaje,
    };
    try {
      if (editing) {
        const payload: UpdateBarCommand = {
          ...base,
          id: editing.id,
          rowVersion: editing.rowVersion,
          estado: editEstado,
        };
        await apiClient<void>(`/bares/${editing.id}`, { method: "PUT", body: payload });
        toast.success(`Bar "${base.nombre}" actualizado.`);
      } else {
        await apiClient<unknown>("/bares", { method: "POST", body: base as CreateBarCommand });
        toast.success(`Bar "${base.nombre}" creado.`);
      }
      setDialogOpen(false);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo guardar el bar.");
    }
  });

  const columns: ColumnDef<BarListItem, unknown>[] = [
    { accessorKey: "nombre", header: "Nombre" },
    {
      accessorKey: "direccion",
      header: "Dirección",
      cell: ({ getValue }) => getValue<string>() || "—",
    },
    {
      accessorKey: "encargado",
      header: "Encargado",
      cell: ({ getValue }) => getValue<string>() || "—",
    },
    {
      accessorKey: "margenReventaPorcentaje",
      header: "Margen %",
      cell: ({ getValue }) => `${getValue<number>()}%`,
    },
    {
      accessorKey: "estado",
      header: "Estado",
      cell: ({ row }) =>
        row.original.estado === 1 ? (
          <Badge variant="outline" className="border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400">
            Activo
          </Badge>
        ) : (
          <Badge variant="outline" className="border-red-600/30 bg-red-500/10 text-red-700 dark:text-red-400">
            Inactivo
          </Badge>
        ),
    },
  ];

  const { register, control, formState: { errors } } = form;

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nuevo bar" title="Nuevo bar">
              <Plus className="size-5" />
            </Button>
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>          </>
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Select
          value={estado}
          onValueChange={(v) => {
            setLoading(true);
            setEstado(v as EstadoFilter);
          }}
        >
          <SelectTrigger className="w-44">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="todos">Todos los estados</SelectItem>
            <SelectItem value="1">Activo</SelectItem>
            <SelectItem value="2">Inactivo</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={rows}
        loading={loading}
        error={error}
        emptyMessage="No hay bares."
        actions={(row) => (
          <Button variant="outline" size="sm" onClick={() => void openEdit(row)}>
            Editar
          </Button>
        )}
      />

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{editing ? "Editar bar" : "Nuevo bar"}</DialogTitle>
            <DialogDescription>
              {editing
                ? "Modificá los datos del bar y guardá los cambios."
                : "Completá los datos para dar de alta un nuevo bar."}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="bar-nombre">Nombre</Label>
              <Input id="bar-nombre" {...register("nombre")} />
              <FieldError message={errors.nombre?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="bar-direccion">Dirección</Label>
              <Input id="bar-direccion" {...register("direccion")} />
              <FieldError message={errors.direccion?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="bar-encargado">Encargado</Label>
              <Input id="bar-encargado" {...register("encargado")} />
              <FieldError message={errors.encargado?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="bar-telefono">Teléfono</Label>
              <Input id="bar-telefono" {...register("telefono")} />
              <FieldError message={errors.telefono?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="bar-horarioRecepcion">Horario de recepción</Label>
              <Input id="bar-horarioRecepcion" {...register("horarioRecepcion")} />
              <FieldError message={errors.horarioRecepcion?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="bar-margen">Margen de reventa (%)</Label>
              <Input id="bar-margen" type="number" step="any" min="0" {...register("margenReventaPorcentaje")} />
              <FieldError message={errors.margenReventaPorcentaje?.message} />
            </div>

            {editing && (
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="bar-estado">Estado</Label>
                <Select value={String(editEstado)} onValueChange={(v) => setEditEstado(Number(v) as EstadoBar)}>
                  <SelectTrigger id="bar-estado" className="w-full">
                    <SelectValue placeholder="Seleccionar…" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1">Activo</SelectItem>
                    <SelectItem value="2">Inactivo</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            )}

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
