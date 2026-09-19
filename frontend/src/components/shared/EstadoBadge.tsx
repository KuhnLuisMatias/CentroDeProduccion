import { Badge } from "@/components/ui/badge";

export const ESTADO_ACTIVO_CLASS =
  "border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400";
export const ESTADO_INACTIVO_CLASS =
  "border-red-600/30 bg-red-500/10 text-red-700 dark:text-red-400";

export default function EstadoBadge({
  activo,
  activoLabel = "Activo",
  inactivoLabel = "Inactivo",
}: {
  activo: boolean;
  activoLabel?: string;
  inactivoLabel?: string;
}) {
  return (
    <Badge variant="outline" className={activo ? ESTADO_ACTIVO_CLASS : ESTADO_INACTIVO_CLASS}>
      {activo ? activoLabel : inactivoLabel}
    </Badge>
  );
}
