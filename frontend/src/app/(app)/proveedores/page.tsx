"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { Plus, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api";
import type { Proveedor, CreateProveedorCommand, UpdateProveedorCommand } from "@/lib/types";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import { Button } from "@/components/ui/button";
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

const proveedorSchema = z.object({
  nombreRazonSocial: z
    .string()
    .trim()
    .min(1, "El nombre/razón social es requerido.")
    .max(200, "Máximo 200 caracteres."),
  cuit: z
    .string()
    .trim()
    .regex(/^\d{2}-\d{8}-\d$/, "El CUIT debe tener formato XX-XXXXXXXX-X."),
  direccion: z
    .string()
    .trim()
    .min(1, "La dirección es requerida.")
    .max(300, "Máximo 300 caracteres."),
  telefono: z.string(),
  whatsapp: z.string(),
  email: z.string(),
  personaContacto: z.string(),
  horarioAtencion: z.string(),
  categoriasProvee: z
    .string()
    .trim()
    .min(1, "Las categorías que provee son requeridas."),
  tipoFactura: z.enum(["A", "B", "C"]),
  observaciones: z.string(),
});

type ProveedorFormInput = z.input<typeof proveedorSchema>;
type ProveedorFormValues = z.output<typeof proveedorSchema>;

const EMPTY_FORM: ProveedorFormInput = {
  nombreRazonSocial: "",
  cuit: "",
  direccion: "",
  telefono: "",
  whatsapp: "",
  email: "",
  personaContacto: "",
  horarioAtencion: "",
  categoriasProvee: "",
  tipoFactura: "A",
  observaciones: "",
};

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function ProveedoresPage() {
  const [rows, setRows] = useState<Proveedor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<Proveedor | null>(null);
  const [editActivo, setEditActivo] = useState("true");

  const load = useCallback(async () => {
    try {
      const result = await apiClient<Proveedor[]>("/proveedores");
      setRows(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar los proveedores.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const form = useForm<ProveedorFormInput, unknown, ProveedorFormValues>({
    resolver: zodResolver(proveedorSchema),
    defaultValues: EMPTY_FORM,
  });

  const openCreate = () => {
    setEditing(null);
    form.reset(EMPTY_FORM);
    setDialogOpen(true);
  };

  const openEdit = (row: Proveedor) => {
    setEditing(row);
    setEditActivo(row.activo ? "true" : "false");
    form.reset({
      nombreRazonSocial: row.nombreRazonSocial,
      cuit: row.cuit,
      direccion: row.direccion,
      telefono: row.telefono ?? "",
      whatsapp: row.whatsapp ?? "",
      email: row.email ?? "",
      personaContacto: row.personaContacto ?? "",
      horarioAtencion: row.horarioAtencion ?? "",
      categoriasProvee: row.categoriasProvee,
      tipoFactura: (["A", "B", "C"] as const).includes(row.tipoFactura as "A" | "B" | "C")
        ? (row.tipoFactura as "A" | "B" | "C")
        : "A",
      observaciones: row.observaciones ?? "",
    });
    setDialogOpen(true);
  };

  const handleSave = form.handleSubmit(async (values) => {
    const base = {
      nombreRazonSocial: values.nombreRazonSocial.trim(),
      cuit: values.cuit.trim(),
      direccion: values.direccion.trim(),
      telefono: values.telefono.trim() || null,
      whatsapp: values.whatsapp.trim() || null,
      email: values.email.trim() || null,
      personaContacto: values.personaContacto.trim() || null,
      horarioAtencion: values.horarioAtencion.trim() || null,
      categoriasProvee: values.categoriasProvee.trim(),
      tipoFactura: values.tipoFactura,
      observaciones: values.observaciones.trim() || null,
    };
    try {
      if (editing) {
        const payload: UpdateProveedorCommand = {
          ...base,
          id: editing.id,
          activo: editActivo === "true",
        };
        await apiClient<void>(`/proveedores/${editing.id}`, { method: "PUT", body: payload });
        toast.success(`Proveedor "${base.nombreRazonSocial}" actualizado.`);
      } else {
        await apiClient<unknown>("/proveedores", { method: "POST", body: base as CreateProveedorCommand });
        toast.success(`Proveedor "${base.nombreRazonSocial}" creado.`);
      }
      setDialogOpen(false);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo guardar el proveedor.");
    }
  });

  const columns: ColumnDef<Proveedor, unknown>[] = [
    { accessorKey: "nombreRazonSocial", header: "Razón social" },
    { accessorKey: "cuit", header: "CUIT" },
    {
      accessorKey: "direccion",
      header: "Dirección",
      cell: ({ getValue }) => getValue<string>() || "—",
    },
    {
      accessorKey: "telefono",
      header: "Teléfono",
      cell: ({ getValue }) => getValue<string>() || "—",
    },
    {
      accessorKey: "email",
      header: "Email",
      cell: ({ getValue }) => getValue<string>() || "—",
    },
    {
      accessorKey: "personaContacto",
      header: "Contacto",
      cell: ({ getValue }) => getValue<string>() || "—",
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
            <Button size="sm" onClick={openCreate} aria-label="Nuevo proveedor" title="Nuevo proveedor">
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
        emptyMessage="No hay proveedores."
        actions={(row) => (
          <Button variant="outline" size="sm" onClick={() => openEdit(row)}>
            Editar
          </Button>
        )}
      />

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>{editing ? "Editar proveedor" : "Nuevo proveedor"}</DialogTitle>
            <DialogDescription>
              {editing
                ? "Modificá los datos del proveedor y guardá los cambios."
                : "Completá los datos para dar de alta un nuevo proveedor."}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prov-nombreRazonSocial">Razón social</Label>
              <Input id="prov-nombreRazonSocial" {...register("nombreRazonSocial")} />
              <FieldError message={errors.nombreRazonSocial?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prov-cuit">CUIT</Label>
              <Input id="prov-cuit" placeholder="XX-XXXXXXXX-X" {...register("cuit")} />
              <FieldError message={errors.cuit?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prov-direccion">Dirección</Label>
              <Input id="prov-direccion" {...register("direccion")} />
              <FieldError message={errors.direccion?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prov-telefono">Teléfono</Label>
              <Input id="prov-telefono" {...register("telefono")} />
              <FieldError message={errors.telefono?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prov-whatsapp">WhatsApp</Label>
              <Input id="prov-whatsapp" {...register("whatsapp")} />
              <FieldError message={errors.whatsapp?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prov-email">Email</Label>
              <Input id="prov-email" type="email" {...register("email")} />
              <FieldError message={errors.email?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prov-personaContacto">Persona de contacto</Label>
              <Input id="prov-personaContacto" {...register("personaContacto")} />
              <FieldError message={errors.personaContacto?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prov-horarioAtencion">Horario de atención</Label>
              <Input id="prov-horarioAtencion" {...register("horarioAtencion")} />
              <FieldError message={errors.horarioAtencion?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prov-categoriasProvee">Categorías que provee</Label>
              <Input id="prov-categoriasProvee" {...register("categoriasProvee")} />
              <FieldError message={errors.categoriasProvee?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="prov-tipoFactura">Tipo de factura</Label>
              <Controller
                control={control}
                name="tipoFactura"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="prov-tipoFactura" className="w-full">
                      <SelectValue placeholder="Seleccionar…" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="A">A</SelectItem>
                      <SelectItem value="B">B</SelectItem>
                      <SelectItem value="C">C</SelectItem>
                    </SelectContent>
                  </Select>
                )}
              />
              <FieldError message={errors.tipoFactura?.message} />
            </div>

            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label htmlFor="prov-observaciones">Observaciones</Label>
              <Textarea id="prov-observaciones" rows={3} {...register("observaciones")} />
              <FieldError message={errors.observaciones?.message} />
            </div>

            {editing && (
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="prov-estado">Estado</Label>
                <Select value={editActivo} onValueChange={setEditActivo}>
                  <SelectTrigger id="prov-estado" className="w-full">
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
