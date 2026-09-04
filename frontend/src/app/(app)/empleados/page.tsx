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
  CargoEmpleado,
  CategoriaEmpleado,
  Empleado,
  CreateEmpleadoCommand,
  UpdateEmpleadoCommand,
} from "@/lib/types";
import { CARGO_EMPLEADO_LABELS, CATEGORIA_EMPLEADO_LABELS } from "@/lib/types";
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

type ActivoFilter = "todos" | "true" | "false";

const empleadoSchema = z.object({
  nombre: z.string().trim().min(1, "El nombre es requerido.").max(100, "Máximo 100 caracteres."),
  apellido: z.string().trim().min(1, "El apellido es requerido.").max(100, "Máximo 100 caracteres."),
  dni: z.string().trim().min(1, "El DNI es requerido.").max(20, "Máximo 20 caracteres."),
  cargo: z.enum(["1", "2", "3", "4"]),
  tarifaPorHora: z.coerce
    .number({ message: "Ingresá un número válido." })
    .positive("La tarifa por hora debe ser mayor a cero."),
  categoria: z.enum(["1", "2", "3"]),
});

type EmpleadoFormInput = z.input<typeof empleadoSchema>;
type EmpleadoFormValues = z.output<typeof empleadoSchema>;

const EMPTY_FORM: EmpleadoFormInput = {
  nombre: "",
  apellido: "",
  dni: "",
  cargo: "1",
  tarifaPorHora: "",
  categoria: "1",
};

const CARGO_OPTIONS = (Object.keys(CARGO_EMPLEADO_LABELS) as unknown as CargoEmpleado[]).map((v) => ({
  value: String(v),
  label: CARGO_EMPLEADO_LABELS[v],
}));

const CATEGORIA_OPTIONS = (
  Object.keys(CATEGORIA_EMPLEADO_LABELS) as unknown as CategoriaEmpleado[]
).map((v) => ({ value: String(v), label: CATEGORIA_EMPLEADO_LABELS[v] }));

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function EmpleadosPage() {
  const [rows, setRows] = useState<Empleado[]>([]);
  const [activo, setActivo] = useState<ActivoFilter>("true");
  const [cargo, setCargo] = useState<string>("todos");
  const [categoria, setCategoria] = useState<string>("todos");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<Empleado | null>(null);
  const [editActivo, setEditActivo] = useState("true");

  const load = useCallback(async () => {
    try {
      const params = new URLSearchParams();
      if (activo !== "todos") params.set("activo", activo);
      if (cargo !== "todos") params.set("cargo", cargo);
      if (categoria !== "todos") params.set("categoria", categoria);
      const qs = params.toString();
      const result = await apiClient<Empleado[]>(`/empleados${qs ? `?${qs}` : ""}`);
      setRows(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar los empleados.");
    } finally {
      setLoading(false);
    }
  }, [activo, cargo, categoria]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const form = useForm<EmpleadoFormInput, unknown, EmpleadoFormValues>({
    resolver: zodResolver(empleadoSchema),
    defaultValues: EMPTY_FORM,
  });

  const openCreate = () => {
    setEditing(null);
    form.reset(EMPTY_FORM);
    setDialogOpen(true);
  };

  const openEdit = (row: Empleado) => {
    setEditing(row);
    setEditActivo(row.activo ? "true" : "false");
    form.reset({
      nombre: row.nombre,
      apellido: row.apellido,
      dni: row.dni,
      cargo: String(row.cargo) as "1" | "2" | "3" | "4",
      tarifaPorHora: row.tarifaPorHora,
      categoria: String(row.categoria) as "1" | "2" | "3",
    });
    setDialogOpen(true);
  };

  const handleSave = form.handleSubmit(async (values) => {
    const base = {
      nombre: values.nombre.trim(),
      apellido: values.apellido.trim(),
      dni: values.dni.trim(),
      cargo: Number(values.cargo) as CargoEmpleado,
      tarifaPorHora: values.tarifaPorHora,
      categoria: Number(values.categoria) as CategoriaEmpleado,
    };
    try {
      if (editing) {
        const payload: UpdateEmpleadoCommand = {
          ...base,
          id: editing.id,
          activo: editActivo === "true",
          rowVersion: editing.rowVersion,
        };
        await apiClient<void>(`/empleados/${editing.id}`, { method: "PUT", body: payload });
        toast.success(`Empleado "${base.nombre} ${base.apellido}" actualizado.`);
      } else {
        await apiClient<unknown>("/empleados", { method: "POST", body: base as CreateEmpleadoCommand });
        toast.success(`Empleado "${base.nombre} ${base.apellido}" creado.`);
      }
      setDialogOpen(false);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo guardar el empleado.");
    }
  });

  const columns: ColumnDef<Empleado, unknown>[] = [
    {
      id: "nombre",
      header: "Nombre",
      cell: ({ row }) => `${row.original.nombre} ${row.original.apellido}`,
    },
    { accessorKey: "dni", header: "DNI" },
    {
      accessorKey: "cargo",
      header: "Cargo",
      cell: ({ row }) => CARGO_EMPLEADO_LABELS[row.original.cargo],
    },
    {
      accessorKey: "categoria",
      header: "Categoría",
      cell: ({ row }) => CATEGORIA_EMPLEADO_LABELS[row.original.categoria],
    },
    {
      accessorKey: "tarifaPorHora",
      header: "Tarifa/hora",
      cell: ({ getValue }) => MONEY.format(getValue<number>()),
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
            <Button size="sm" onClick={openCreate} aria-label="Nuevo empleado" title="Nuevo empleado">
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
          value={activo}
          onValueChange={(v) => {
            setLoading(true);
            setActivo(v as ActivoFilter);
          }}
        >
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="todos">Todos</SelectItem>
            <SelectItem value="true">Activos</SelectItem>
            <SelectItem value="false">Inactivos</SelectItem>
          </SelectContent>
        </Select>

        <Select
          value={cargo}
          onValueChange={(v) => {
            setLoading(true);
            setCargo(v);
          }}
        >
          <SelectTrigger className="w-48">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="todos">Todos los cargos</SelectItem>
            {CARGO_OPTIONS.map((o) => (
              <SelectItem key={o.value} value={o.value}>
                {o.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={categoria}
          onValueChange={(v) => {
            setLoading(true);
            setCategoria(v);
          }}
        >
          <SelectTrigger className="w-48">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="todos">Todas las categorías</SelectItem>
            {CATEGORIA_OPTIONS.map((o) => (
              <SelectItem key={o.value} value={o.value}>
                {o.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={rows}
        loading={loading}
        error={error}
        emptyMessage="No hay empleados."
        actions={(row) => (
          <Button variant="outline" size="sm" onClick={() => openEdit(row)}>
            Editar
          </Button>
        )}
      />

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{editing ? "Editar empleado" : "Nuevo empleado"}</DialogTitle>
            <DialogDescription>
              {editing
                ? "Modificá los datos del empleado y guardá los cambios."
                : "Completá los datos para dar de alta un nuevo empleado."}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="emp-nombre">Nombre</Label>
              <Input id="emp-nombre" {...register("nombre")} />
              <FieldError message={errors.nombre?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="emp-apellido">Apellido</Label>
              <Input id="emp-apellido" {...register("apellido")} />
              <FieldError message={errors.apellido?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="emp-dni">DNI</Label>
              <Input id="emp-dni" {...register("dni")} />
              <FieldError message={errors.dni?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="emp-tarifa">Tarifa por hora</Label>
              <Input id="emp-tarifa" type="number" step="any" min="0" {...register("tarifaPorHora")} />
              <FieldError message={errors.tarifaPorHora?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="emp-cargo">Cargo</Label>
              <Controller
                control={control}
                name="cargo"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="emp-cargo" className="w-full">
                      <SelectValue placeholder="Seleccionar…" />
                    </SelectTrigger>
                    <SelectContent>
                      {CARGO_OPTIONS.map((o) => (
                        <SelectItem key={o.value} value={o.value}>
                          {o.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              <FieldError message={errors.cargo?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="emp-categoria">Categoría</Label>
              <Controller
                control={control}
                name="categoria"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="emp-categoria" className="w-full">
                      <SelectValue placeholder="Seleccionar…" />
                    </SelectTrigger>
                    <SelectContent>
                      {CATEGORIA_OPTIONS.map((o) => (
                        <SelectItem key={o.value} value={o.value}>
                          {o.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              <FieldError message={errors.categoria?.message} />
            </div>

            {editing && (
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="emp-estado">Estado</Label>
                <Select value={editActivo} onValueChange={setEditActivo}>
                  <SelectTrigger id="emp-estado" className="w-full">
                    <SelectValue placeholder="Seleccionar…" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="true">Activo</SelectItem>
                    <SelectItem value="false">Inactivo</SelectItem>
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
