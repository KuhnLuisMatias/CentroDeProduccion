"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { ColumnDef } from "@tanstack/react-table";
import { Plus, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { apiClient, ApiError, fetchAllPages } from "@/lib/api";
import { MONEY } from "@/lib/utils";
import type {
  Receta,
  RecetaInsumo,
  CosteoReceta,
  RecetaVersion,
  Categoria,
  Insumo,
  UnidadMedida,
  CreateRecetaCommand,
  UpdateRecetaCommand,
  EstadoReceta,
} from "@/lib/types";
import { ESTADO_RECETA_LABELS } from "@/lib/types";
import PageHeader from "@/components/shared/PageHeader";
import DataTable from "@/components/shared/DataTable";
import RecipeLinesEditor, { type RecipeLineDraft } from "@/components/shared/RecipeLinesEditor";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

const lineSchema = z.object({
  id: z.string(),
  tipo: z.enum(["insumo", "subreceta"]),
  insumoId: z.string(),
  recetaOrigenId: z.string(),
  cantidadNecesaria: z.string(),
  unidadMedidaId: z.string(),
});

const recetaSchema = z
  .object({
    nombre: z.string().trim().min(1, "El nombre es obligatorio.").max(200, "Máximo 200 caracteres."),
    codigoSku: z.string().trim().min(1, "El código SKU es obligatorio.").max(50, "Máximo 50 caracteres."),
    categoriaId: z.string(),
    unidadMedidaId: z.string().min(1, "La unidad de medida resultante es obligatoria."),
    descripcion: z.string().max(1000, "Máximo 1000 caracteres."),
    estado: z.string(),
    lines: z.array(lineSchema),
  })
  .superRefine((values, ctx) => {
    const hasValidLine = values.lines.some(
      (l) =>
        (l.insumoId || l.recetaOrigenId) && l.unidadMedidaId && Number(l.cantidadNecesaria) > 0,
    );
    if (!hasValidLine) {
      ctx.addIssue({
        code: "custom",
        path: ["lines"],
        message:
          "Agregá al menos una línea válida (insumo o sub-receta con cantidad y unidad).",
      });
    }
  });

type RecetaFormInput = z.input<typeof recetaSchema>;
type RecetaFormValues = z.output<typeof recetaSchema>;

const EMPTY_FORM: RecetaFormInput = {
  nombre: "",
  codigoSku: "",
  categoriaId: "",
  unidadMedidaId: "",
  descripcion: "",
  estado: "1",
  lines: [],
};

interface FieldErrorProps {
  message?: string;
}

function FieldError({ message }: FieldErrorProps) {
  if (!message) return null;
  return <p className="text-xs font-medium text-destructive">{message}</p>;
}

export default function RecetasPage() {
  const [rows, setRows] = useState<Receta[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [unidades, setUnidades] = useState<UnidadMedida[]>([]);
  const [insumos, setInsumos] = useState<Insumo[]>([]);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<Receta | null>(null);
  const [editLoading, setEditLoading] = useState<string | null>(null);

  const [detail, setDetail] = useState<Receta | null>(null);
  const [costeo, setCosteo] = useState<CosteoReceta | null>(null);
  const [versiones, setVersiones] = useState<RecetaVersion[]>([]);
  const [detailLoading, setDetailLoading] = useState(false);

  const load = useCallback(async () => {
    try {
      const result = await apiClient<Receta[]>("/recetas");
      setRows(result);
      setError(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudieron cargar las recetas.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      try {
        const [recetas, cat, uni, ins] = await Promise.all([
          apiClient<Receta[]>("/recetas"),
          apiClient<Categoria[]>("/categorias?ambito=2"),
          apiClient<UnidadMedida[]>("/unidadesmedida"),
          fetchAllPages<Insumo>("/insumos?pageSize=100"),
        ]);
        if (cancelled) return;
        setRows(recetas);
        setCategorias(cat);
        setUnidades(uni);
        setInsumos(ins);
        setError(null);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof ApiError ? err.message : "No se pudieron cargar las recetas.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, []);

  const form = useForm<RecetaFormInput, unknown, RecetaFormValues>({
    resolver: zodResolver(recetaSchema),
    defaultValues: EMPTY_FORM,
  });

  const openCreate = () => {
    setEditing(null);
    form.reset(EMPTY_FORM);
    setDialogOpen(true);
  };

  const openEdit = async (row: Receta) => {
    if (editLoading) return;
    setEditLoading(row.id);
    try {
      const det = await apiClient<Receta>(`/recetas/${row.id}`);
      setEditing(row);
      form.reset({
        nombre: det.nombre ?? row.nombre,
        codigoSku: det.codigoSku ?? row.codigoSku,
        categoriaId: det.categoriaId || row.categoriaId,
        unidadMedidaId: det.unidadMedidaId || row.unidadMedidaId || "",
        descripcion: det.descripcion ?? "",
        estado: String(det.estado ?? row.estado),
        lines: (det.insumos ?? []).map((ri: RecetaInsumo): RecipeLineDraft => ({
          id: ri.id,
          tipo: ri.insumoId ? "insumo" : "subreceta",
          insumoId: ri.insumoId ?? "",
          recetaOrigenId: ri.recetaOrigenId ?? "",
          cantidadNecesaria: String(ri.cantidadNecesaria),
          unidadMedidaId: ri.unidadMedidaId,
        })),
      });
      setDialogOpen(true);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : "No se pudo cargar el detalle de la receta.";
      setError(msg);
      toast.error(msg);
    } finally {
      setEditLoading(null);
    }
  };

  const handleSave = form.handleSubmit(async (values) => {
    const insumosPayload = values.lines
      .filter((l) => l.insumoId || l.recetaOrigenId)
      .map((l) => ({
        insumoId: l.insumoId || null,
        recetaOrigenId: l.recetaOrigenId || null,
        cantidadNecesaria: parseFloat(l.cantidadNecesaria) || 0,
        unidadMedidaId: l.unidadMedidaId,
      }));
    const base = {
      nombre: values.nombre.trim(),
      codigoSku: values.codigoSku.trim(),
      categoriaId: values.categoriaId,
      unidadMedidaId: values.unidadMedidaId,
      descripcion: values.descripcion.trim() || null,
      insumos: insumosPayload,
    };
    try {
      if (editing) {
        const payload: UpdateRecetaCommand = {
          ...base,
          id: editing.id,
          estado: Number(values.estado) as EstadoReceta,
        };
        // TODO(diag): remove after concurrency investigation
        console.log("[CONCURRENCY-DIAG] submitting PUT", editing.id, new Date().toISOString());
        await apiClient<void>(`/recetas/${editing.id}`, { method: "PUT", body: payload });
        toast.success(`Receta "${base.nombre}" actualizada.`);
      } else {
        await apiClient<unknown>("/recetas", { method: "POST", body: base as CreateRecetaCommand });
        toast.success(`Receta "${base.nombre}" creada.`);
      }
      setDialogOpen(false);
      await load();
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "No se pudo guardar la receta.");
    }
  });

  const openDetail = async (row: Receta) => {
    setDetail(row);
    setCosteo(null);
    setVersiones([]);
    setDetailLoading(true);
    try {
      const [det, cost, vers] = await Promise.all([
        apiClient<Receta>(`/recetas/${row.id}`),
        apiClient<CosteoReceta>(`/recetas/${row.id}/costeo`).catch(() => null),
        apiClient<RecetaVersion[]>(`/recetas/${row.id}/versions`).catch(() => []),
      ]);
      setDetail(det);
      setCosteo(cost);
      setVersiones(vers);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo cargar el detalle.");
    } finally {
      setDetailLoading(false);
    }
  };

  const columns: ColumnDef<Receta, unknown>[] = [
    { accessorKey: "nombre", header: "Nombre" },
    { accessorKey: "codigoSku", header: "SKU" },
    {
      id: "categoria",
      header: "Categoría",
      cell: ({ row }) => row.original.categoria?.nombre ?? "—",
    },
    {
      id: "unidad",
      header: "Unidad",
      cell: ({ row }) =>
        row.original.unidadMedida?.simbolo ??
        unidades.find((u) => u.id === row.original.unidadMedidaId)?.simbolo ??
        "—",
    },
    {
      id: "estado",
      header: "Estado",
      cell: ({ row }) =>
        row.original.estado === 1 ? (
          <Badge variant="outline" className="border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400">
            {ESTADO_RECETA_LABELS[row.original.estado] ?? String(row.original.estado)}
          </Badge>
        ) : (
          <Badge variant="outline">
            {ESTADO_RECETA_LABELS[row.original.estado] ?? String(row.original.estado)}
          </Badge>
        ),
    },
  ];

  const {
    register,
    control,
    formState: { errors, isSubmitting },
  } = form;

  return (
    <div>
      <PageHeader
        actions={
          <>
            <Button size="sm" onClick={openCreate} aria-label="Nueva receta" title="Nueva receta">
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
        emptyMessage="No hay recetas."
        actions={(row) => (
          <>
            <Button variant="outline" size="sm" onClick={() => void openDetail(row)}>
              Ver
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => void openEdit(row)}
              disabled={editLoading !== null}
            >
              {editLoading === row.id ? "Cargando…" : "Editar"}
            </Button>
          </>
        )}
      />

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>{editing ? "Editar receta" : "Nueva receta"}</DialogTitle>
            <DialogDescription>
              {editing
                ? "Modificá los datos de la receta y guardá los cambios."
                : "Completá los datos para crear una nueva receta."}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSave} className="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="receta-nombre">Nombre</Label>
              <Input id="receta-nombre" {...register("nombre")} />
              <FieldError message={errors.nombre?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="receta-codigoSku">Código SKU</Label>
              <Input id="receta-codigoSku" {...register("codigoSku")} />
              <FieldError message={errors.codigoSku?.message} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="receta-categoria">Categoría</Label>
              <Controller
                control={control}
                name="categoriaId"
                render={({ field }) => (
                  <Select value={field.value || undefined} onValueChange={field.onChange}>
                    <SelectTrigger id="receta-categoria" className="w-full">
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
                )}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="receta-unidadMedida">Unidad de medida resultante</Label>
              <Controller
                control={control}
                name="unidadMedidaId"
                render={({ field }) => (
                  <Select value={field.value || undefined} onValueChange={field.onChange}>
                    <SelectTrigger id="receta-unidadMedida" className="w-full">
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
                )}
              />
              <FieldError message={errors.unidadMedidaId?.message} />
            </div>

            {editing && (
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="receta-estado">Estado</Label>
                <Controller
                  control={control}
                  name="estado"
                  render={({ field }) => (
                    <Select value={field.value} onValueChange={field.onChange}>
                      <SelectTrigger id="receta-estado" className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {(Object.keys(ESTADO_RECETA_LABELS) as unknown as string[]).map((v) => (
                          <SelectItem key={v} value={v}>
                            {ESTADO_RECETA_LABELS[Number(v) as EstadoReceta]}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </div>
            )}

            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label htmlFor="receta-descripcion">Descripción</Label>
              <Input id="receta-descripcion" {...register("descripcion")} />
              <FieldError message={errors.descripcion?.message} />
            </div>

            <Controller
              control={control}
              name="lines"
              render={({ field }) => (
                <RecipeLinesEditor
                  lines={field.value}
                  insumos={insumos}
                  recetas={rows}
                  unidades={unidades}
                  onChange={field.onChange}
                />
              )}
            />
            <FieldError message={errors.lines?.message} />

            <DialogFooter className="sm:col-span-2">
              <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Guardando…" : "Guardar"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={detail !== null} onOpenChange={(open) => !open && setDetail(null)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Receta: {detail?.nombre}</DialogTitle>
            <DialogDescription>Detalle, costeo e historial de versiones.</DialogDescription>
          </DialogHeader>

          {detailLoading ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Cargando detalle…</p>
          ) : detail ? (
            <div className="flex flex-col gap-4">
              <Card>
                <CardHeader>
                  <CardTitle className="text-sm">Datos</CardTitle>
                </CardHeader>
                <CardContent className="grid grid-cols-1 gap-x-4 gap-y-2 text-sm sm:grid-cols-2">
                  <div>
                    <span className="font-medium">SKU:</span> {detail.codigoSku}
                  </div>
                  <div>
                    <span className="font-medium">Versión:</span> {detail.version}
                  </div>
                  <div>
                    <span className="font-medium">Unidad resultante:</span>{" "}
                    {detail.unidadMedida?.nombre ??
                      unidades.find((u) => u.id === detail.unidadMedidaId)?.nombre ??
                      "—"}
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
                        <TableHead className="text-right">Cantidad</TableHead>
                        <TableHead className="text-right">Unidad</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {detail.insumos.map((ri) => (
                        <TableRow key={ri.id}>
                          <TableCell>{ri.insumo?.nombre ?? ri.recetaOrigen?.nombre ?? "—"}</TableCell>
                          <TableCell className="text-right">{ri.cantidadNecesaria}</TableCell>
                          <TableCell className="text-right">{ri.unidadMedida?.simbolo ?? ""}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </CardContent>
              </Card>

              {versiones.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle className="text-sm">Versiones</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <Table>
                      <TableHeader>
                        <TableRow>
                      <TableHead>Versión</TableHead>
                      <TableHead>Nombre</TableHead>
                      <TableHead className="text-right">Fecha</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {versiones.map((v) => (
                      <TableRow key={v.id}>
                        <TableCell>{v.version}</TableCell>
                        <TableCell>{v.nombre}</TableCell>
                        <TableCell className="text-right">
                              {new Date(v.fechaCreacion).toLocaleDateString("es-AR")}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </CardContent>
                </Card>
              )}
            </div>
          ) : null}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setDetail(null)}>
              Cerrar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
