import type { ComponentType } from "react";
import {
  BookOpen,
  Boxes,
  ChartColumn,
  CreditCard,
  Factory,
  Folder,
  NotebookText,
  Package,
  Receipt,
  Ruler,
  Scale,
  ShoppingCart,
  Store,
  Tag,
  Truck,
  Undo2,
  Users,
} from "lucide-react";

export type Rol =
  | "Administrador"
  | "EncargadoProduccion"
  | "EncargadoCompras"
  | "EncargadoVentas";

export interface NavItem {
  href: string;
  label: string;
  icon?: ComponentType<{ className?: string }>;
  roles?: Rol[];
}

export interface NavGroup {
  id: string;
  label: string;
  icon: ComponentType<{ className?: string }>;
  badge?: string | number;
  items: NavItem[];
}

export const NAV_GROUPS: NavGroup[] = [
  {
    id: "produccion",
    label: "Producción",
    icon: Factory,
    items: [{ href: "/produccion", label: "Órdenes de Producción", icon: Factory }],
  },
  {
    id: "stock",
    label: "Stock",
    icon: Boxes,
    items: [
      {
        href: "/productos-terminados",
        label: "Productos Terminados",
        icon: Package,
        roles: ["EncargadoProduccion"],
      },
      {
        href: "/insumos",
        label: "Insumos",
        icon: Scale,
        roles: ["EncargadoProduccion", "EncargadoCompras"],
      },
    ],
  },
  {
    id: "compras",
    label: "Compras",
    icon: ShoppingCart,
    items: [
      { href: "/compras", label: "Órdenes de Compra", icon: ShoppingCart },
      { href: "/pagos", label: "Facturas y Pagos", icon: Receipt },
    ],
  },
  {
    id: "ventas",
    label: "Ventas",
    icon: Receipt,
    items: [
      { href: "/remitos", label: "Pedidos y Remitos", icon: Receipt },
      { href: "/devoluciones", label: "Devoluciones", icon: Undo2 },
      { href: "/pagos-bar", label: "Pagos de Bares", icon: CreditCard },
    ],
  },
  {
    id: "cuenta-corriente",
    label: "Cuentas Corrientes",
    icon: NotebookText,
    items: [
      {
        href: "/cuenta-corriente",
        label: "Cuentas Corrientes",
        icon: NotebookText,
        roles: ["EncargadoCompras", "EncargadoVentas"],
      },
    ],
  },
  {
    id: "reportes",
    label: "Reportes",
    icon: ChartColumn,
    items: [{ href: "/reportes", label: "Reportes", icon: ChartColumn }],
  },
  {
    id: "catalogos",
    label: "Catálogos",
    icon: Folder,
    items: [
      { href: "/recetas", label: "Recetas", icon: BookOpen },
      { href: "/proveedores", label: "Proveedores", icon: Truck },
      {
        href: "/categorias",
        label: "Categorías",
        icon: Tag,
        roles: ["EncargadoProduccion", "EncargadoCompras"],
      },
      {
        href: "/unidades",
        label: "Unidades de Medida",
        icon: Ruler,
        roles: ["EncargadoProduccion", "EncargadoCompras"],
      },
      { href: "/bares", label: "Bares", icon: Store, roles: ["EncargadoVentas"] },
      { href: "/empleados", label: "Empleados", icon: Users, roles: ["EncargadoProduccion"] },
    ],
  },
];

export function isActive(pathname: string, href: string): boolean {
  return pathname === href || pathname.startsWith(`${href}/`);
}
