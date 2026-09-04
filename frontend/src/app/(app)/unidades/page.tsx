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
  TipoUnidadMedida,
  UnidadMedida,
  CreateUnidadMedidaCommand,
  UpdateUnidadMedidaCommand,
} from "@/lib/types";
import { TIPO_UNIDAD_MEDIDA_LABELS } from "@/lib/types";
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

const unidadSchema = z.object({
  nombre: z
    .string()
    .trim()
    .min(1, "El nombre es requerido.")
    .max(50, "Máximo 50 caracteres."),
  simbolo: z
    .string()
    .trim()
    .min(1, "El símbolo es requerido.")
    .max(10, "Máximo 10 caracteres."),
  tipo: z.enum(["1", "2", "3"]),
});

type UnidadFormInput = z.input<typeof unidadSchema>;
type UnidadFormValues = z.output<typeof unidadSchema>;

const EMPTY_FORM: UnidadFormInput = { nombre: "", simbolo: "", tipo: "1" };

const TIPO_OPTIONS = (Object.keys(TIPO_UNIDAD_MEDIDA_LABELS) as unknown as TipoUnidadMedida[]).map(
  (v) => ({ value: String(v), label: TIPO_UNIDAD_MEDIDA_LABELS[v] }),
);

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function UnidadesPage() {
  const { user } = useAuth();
  const isAdmin = user?.rol === "Administrador";

  const [rows, setRows] = useState<UnidadMedida[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<UnidadMedida | null>(null);
  const [editActivo, setEditActivo] = useState("true");

  const load = useCallback(async () => {
    try {
      const result = await apiClient<UnidadMedida[]>("/unidadesmedida");
      setRows(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar las unidades de medida.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const form = useForm<UnidadFormInput, unknown, UnidadFormValues>({
    resolver: zodResolver(unidadSchema),
    defaultValues: EMPTY_FORM,
  });

  const openCreate = () => {
    setEditing(null);
    form.reset(EMPTY_FORM);
    setDialogOpen(true);
  };

  const openEdit = (row: UnidadMedida) => {
    setEditing(row);
    setEditActivo(row.activo ? "true" : "false");
    form.reset({ nombre: row.nombre, simbolo: row.simbolo, tipo: String(row.tipo) as "1" | "2" | "3" });
    setDialogOpen(true);
  };

  const handleSave = form.handleSubmit(async (values) => {
    const base = {
      nombre: values.nombre.trim(),
      simbolo: values.simbolo.trim(),
      tipo: Number(values.tipo) as TipoUnidadMedida,
    };
    try {
      if (editing) {
        const payload: UpdateUnidadMedidaCommand = {
          ...base,
          id: editing.id,
          activo: editActivo === "true",
        };
        await apiClient<void>(`/unidadesmedida/${editing.id}`, { method: "PUT", body: payload });
        toast.success(`Unidad "${base.nombre}" actualizada.`);
      } else {
        const payload: CreateUnidadMedidaCommand = base;
        await apiClient<unknown>("/unidadesmedida", { method: "POST", body: payload });
        toast.success(`Unidad "${base.nombre}" creada.`);
      }
      setDialogOpen(false);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo guardar la unidad de medida.");
    }
  });

  const columns: ColumnDef<UnidadMedida, unknown>[] = [
    { accessorKey: "nombre", header: "Nombre" },
    { accessorKey: "simbolo", header: "Símbolo" },
    {
      accessorKey: "tipo",
      header: "Tipo",
      cell: ({ row }) => TIPO_UNIDAD_MEDIDA_LABELS[row.original.tipo],
    },
    {
      accessorKey: "activo",
      header: "Estado",
      cell: ({ row }) =>
        row.original.activo ? (
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
            {isAdmin && (
              <Button size="sm" onClick={openCreate} aria-label="Nueva unidad" title="Nueva unidad">
                <Plus className="size-5" />
              </Button>
            )}
            <Button variant="outline" size="sm" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={`size-5 ${loading ? "animate-spin" : ""}`} />
              Actualizar
            </Button>
          </>
        }
      />

      <DataTable
        columns={columns}
        data={rows}
        loading={loading}
        error={error}
        emptyMessage="No hay unidades de medida."
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
            <DialogTitle>{editing ? "Editar unidad de medida" : "Nueva unidad de medida"}</DialogTitle>
            <DialogDescription>
              {editing
                ? "Modificá los datos de la unidad y guardá los cambios."
                : "Completá los datos para dar de alta una nueva unidad."}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="flex flex-col gap-3" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="uni-nombre">Nombre</Label>
              <Input id="uni-nombre" {...register("nombre")} />
              <FieldError message={errors.nombre?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="uni-simbolo">Símbolo</Label>
              <Input id="uni-simbolo" {...register("simbolo")} />
              <FieldError message={errors.simbolo?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="uni-tipo">Tipo</Label>
              <Controller
                control={control}
                name="tipo"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="uni-tipo" className="w-full">
                      <SelectValue placeholder="Seleccionar…" />
                    </SelectTrigger>
                    <SelectContent>
                      {TIPO_OPTIONS.map((o) => (
                        <SelectItem key={o.value} value={o.value}>
                          {o.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              <FieldError message={errors.tipo?.message} />
            </div>

            {editing && (
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="uni-estado">Estado</Label>
                <Select value={editActivo} onValueChange={setEditActivo}>
                  <SelectTrigger id="uni-estado" className="w-full">
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
