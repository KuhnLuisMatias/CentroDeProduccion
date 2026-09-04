"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useAuth } from "@/context/AuthContext";
import { LogOut, Menu, Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { isActive, NAV_GROUPS, type NavGroup, type Rol } from "@/lib/nav";

const COLLAPSED_KEY = "cdp_sidebar_collapsed";

const HOVER_INTENT_MS = 150;
const HOVER_CLOSE_MS = 120;

const ROLE_BADGE_CLASS: Record<string, string> = {
  Administrador: "bg-slate-200 text-slate-800 dark:bg-slate-800 dark:text-slate-200 hover:bg-slate-300",
  EncargadoProduccion: "bg-slate-200 text-slate-800 dark:bg-slate-800 dark:text-slate-200 hover:bg-slate-300",
  EncargadoCompras: "bg-slate-200 text-slate-800 dark:bg-slate-800 dark:text-slate-200 hover:bg-slate-300",
  EncargadoVentas: "bg-slate-200 text-slate-800 dark:bg-slate-800 dark:text-slate-200 hover:bg-slate-300",
};

function isVisible(roles: Rol[] | undefined, rol: string): boolean {
  if (!roles) return true;
  if (rol === "Administrador") return true;
  return roles.includes(rol as Rol);
}

function groupHasActiveRoute(group: NavGroup, pathname: string): boolean {
  return group.items.some((item) => isActive(pathname, item.href));
}

function useIsMobile(): boolean {
  const [isMobile, setIsMobile] = useState(false);

  useEffect(() => {
    const query = window.matchMedia("(max-width: 1023px)");
    const update = () => {
      setIsMobile(query.matches);
    };
    update();
    query.addEventListener("change", update);
    return () => query.removeEventListener("change", update);
  }, []);

  return isMobile;
}

export default function AppShell({ children }: { children: ReactNode }) {
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const pathname = usePathname();

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- hydration: sync persisted sidebar state after mount
    setCollapsed(window.localStorage.getItem(COLLAPSED_KEY) === "1");
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- close mobile drawer on route change
    setMobileOpen(false);
  }, [pathname]);

  const toggleCollapsed = () => {
    setCollapsed((prev) => {
      const next = !prev;
      window.localStorage.setItem(COLLAPSED_KEY, next ? "1" : "0");
      return next;
    });
  };

  return (
    <div className="flex min-h-screen bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-slate-100">
      <Sidebar
        collapsed={collapsed}
        mobileOpen={mobileOpen}
        onToggleCollapse={toggleCollapsed}
        onCloseMobile={() => setMobileOpen(false)}
      />
      {mobileOpen && (
        <div
          className="fixed inset-0 z-35 bg-slate-950/60 backdrop-blur-xs lg:hidden"
          onClick={() => setMobileOpen(false)}
          aria-hidden="true"
        />
      )}
      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar onMenuClick={() => setMobileOpen(true)} />
        <main className="flex-1 p-4 sm:p-6">{children}</main>
      </div>
    </div>
  );
}

function Sidebar({
  collapsed,
  mobileOpen,
  onToggleCollapse,
  onCloseMobile,
}: {
  collapsed: boolean;
  mobileOpen: boolean;
  onToggleCollapse: () => void;
  onCloseMobile: () => void;
}) {
  const { user } = useAuth();
  const pathname = usePathname();
  const rol = user?.rol ?? "";
  const isMobile = useIsMobile();
  const navRef = useRef<HTMLElement>(null);
  const hoverTimerRef = useRef<number | null>(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>({});
  const [flyoutGroup, setFlyoutGroup] = useState<string | null>(null);

  // Filter groups according to role & search query
  const filteredGroups = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();

    return NAV_GROUPS.map((group) => {
      const visibleItems = group.items.filter(
        (item) =>
          isVisible(item.roles, rol) &&
          (query === "" ||
            item.label.toLowerCase().includes(query) ||
            group.label.toLowerCase().includes(query)),
      );
      return { ...group, items: visibleItems };
    }).filter((group) => group.items.length > 0);
  }, [rol, searchQuery]);

  // Expand group that has active route on load or navigation
  useEffect(() => {
    filteredGroups.forEach((group) => {
      if (groupHasActiveRoute(group, pathname)) {
        setOpenGroups((prev) => ({ ...prev, [group.id]: true }));
      }
    });
  }, [pathname, filteredGroups]);

  // Auto-expand groups when searching
  useEffect(() => {
    if (searchQuery.trim() !== "") {
      const allGroupIds: Record<string, boolean> = {};
      filteredGroups.forEach((group) => {
        allGroupIds[group.id] = true;
      });
      // eslint-disable-next-line react-hooks/set-state-in-effect -- auto-expand all groups while searching
      setOpenGroups(allGroupIds);
    }
  }, [searchQuery, filteredGroups]);

  const toggleGroup = useCallback((groupId: string) => {
    setOpenGroups((prev) => ({ ...prev, [groupId]: !prev[groupId] }));
  }, []);

  const clearHoverTimer = useCallback(() => {
    if (hoverTimerRef.current !== null) {
      window.clearTimeout(hoverTimerRef.current);
      hoverTimerRef.current = null;
    }
  }, []);

  const handleMouseEnter = useCallback(
    (id: string) => {
      clearHoverTimer();
      hoverTimerRef.current = window.setTimeout(() => setFlyoutGroup(id), HOVER_INTENT_MS);
    },
    [clearHoverTimer],
  );

  const handleMouseLeave = useCallback(
    (id: string) => {
      clearHoverTimer();
      hoverTimerRef.current = window.setTimeout(() => {
        setFlyoutGroup((current) => (current === id ? null : current));
      }, HOVER_CLOSE_MS);
    },
    [clearHoverTimer],
  );

  return (
    <nav
      ref={navRef}
      className={`sticky top-0 z-40 flex h-screen flex-col border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 transition-all duration-300 shadow-xs max-lg:fixed max-lg:inset-y-0 max-lg:left-0 max-lg:shadow-2xl ${
        collapsed ? "w-[72px]" : "w-[270px]"
      } ${
        mobileOpen ? "max-lg:translate-x-0" : "max-lg:-translate-x-full"
      } max-lg:!w-[270px]`}
      aria-label="Navegación principal"
    >
      {/* Brand Header */}
      <div className="flex h-16 shrink-0 items-center justify-between px-4 py-3 border-b border-slate-100 dark:border-slate-800/60">
        <div className={`flex min-w-0 items-center gap-3 ${collapsed ? "mx-auto" : ""}`}>
          {/* CP Logo Badge — toggles sidebar collapse/expand */}
          <button
            type="button"
            onClick={onToggleCollapse}
            title={collapsed ? "Expandir menú" : "Colapsar menú"}
            aria-label={collapsed ? "Expandir menú" : "Colapsar menú"}
            className="flex size-9 shrink-0 cursor-pointer items-center justify-center rounded-xl bg-slate-900 text-sm font-bold text-white shadow-md transition-opacity hover:opacity-80 dark:bg-slate-100 dark:text-slate-900"
          >
            CP
          </button>
          {!collapsed && (
            <Link
              href="/dashboard"
              onClick={onCloseMobile}
              className="flex min-w-0 flex-col truncate transition-opacity hover:opacity-80"
            >
              <span className="truncate text-sm font-bold tracking-tight text-slate-900 dark:text-white">
                CentroProducción
              </span>
              <span className="truncate text-[10px] font-medium text-slate-400">
                Sistema de Gestión
              </span>
            </Link>
          )}
        </div>

        {/* Close Button (Mobile) */}
        <button
          type="button"
          onClick={onCloseMobile}
          className="flex lg:hidden size-8 shrink-0 items-center justify-center rounded-full bg-slate-100 dark:bg-slate-800 text-slate-500 hover:text-slate-900 dark:hover:text-white"
          aria-label="Cerrar menú"
        >
          <X className="size-4" />
        </button>
      </div>

      {/* Navigation Groups List */}
      <div className="flex-1 overflow-y-auto overflow-x-hidden px-3 py-2 space-y-1">
        {filteredGroups.map((group) => {
          const isOpen = Boolean(openGroups[group.id]);
          const hasActive = groupHasActiveRoute(group, pathname);

          if (collapsed && !isMobile) {
            return (
              <SidebarGroupRail
                key={group.id}
                group={group}
                pathname={pathname}
                isOpen={flyoutGroup === group.id}
                activeGroup={hasActive}
                onMouseEnter={() => handleMouseEnter(group.id)}
                onMouseLeave={() => handleMouseLeave(group.id)}
                onNavigate={onCloseMobile}
              />
            );
          }

          return (
            <SidebarGroupAccordion
              key={group.id}
              group={group}
              pathname={pathname}
              isOpen={isOpen}
              activeGroup={hasActive}
              onToggle={() => toggleGroup(group.id)}
              onNavigate={onCloseMobile}
            />
          );
        })}
      </div>
    </nav>
  );
}

function SidebarGroupAccordion({
  group,
  pathname,
  isOpen,
  activeGroup,
  onToggle,
  onNavigate,
}: {
  group: NavGroup;
  pathname: string;
  isOpen: boolean;
  activeGroup: boolean;
  onToggle: () => void;
  onNavigate: () => void;
}) {
  const GroupIcon = group.icon;

  // Single-item group: render as direct link
  if (group.items.length === 1) {
    const item = group.items[0];
    const active = isActive(pathname, item.href);
    return (
      <div className="my-0.5">
        <Link
          href={item.href}
          onClick={onNavigate}
          className={`flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold transition-all duration-150 ${
            active
              ? "bg-[#0a0a0a] text-white shadow-md"
              : "bg-transparent text-slate-700 hover:bg-gray-100 hover:text-slate-900"
          }`}
        >
          <GroupIcon className="size-4.5 shrink-0" />
          <span className="truncate">{item.label}</span>
        </Link>
      </div>
    );
  }

  // Multi-item group: render main menu item with accordion expansion inside the sidebar
  return (
    <div className="my-0.5">
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={isOpen}
        className="flex w-full items-center justify-between gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold transition-all duration-150 bg-transparent text-slate-700 hover:bg-gray-100 hover:text-slate-900"
      >
        <div className="flex items-center gap-3 min-w-0">
          <GroupIcon className="size-4.5 shrink-0" />
          <span className="truncate">{group.label}</span>
        </div>
        <div className="flex items-center gap-1.5 shrink-0">
          {group.badge && (
            <span
              className={`rounded-full px-2 py-0.5 text-[10px] font-bold ${
                activeGroup
                  ? "bg-slate-800 text-white"
                  : "bg-slate-200 text-slate-700"
              }`}
            >
              {group.badge}
            </span>
          )}
          <Plus
            className={`size-4 transition-transform duration-200 ${
              isOpen ? "rotate-45" : ""
            }`}
          />
        </div>
      </button>

      {/* Submenu Card Container (Inline indented container in grayscale) */}
      {isOpen && (
        <div className="mt-1.5 mb-2 ml-1 space-y-0.5 rounded-2xl border border-slate-200/80 bg-slate-100/80 p-2 shadow-xs dark:border-slate-800 dark:bg-slate-800/60">
          {group.items.map((item) => {
            const active = isActive(pathname, item.href);
            return (
              <Link
                key={item.href}
                href={item.href}
                onClick={onNavigate}
                className={`flex items-center gap-2.5 rounded-xl px-3 py-2 text-xs sm:text-sm transition-all ${
                  active
                    ? "bg-[#0a0a0a] font-bold text-white shadow-xs"
                    : "bg-transparent text-slate-600 hover:bg-gray-100 hover:text-slate-900"
                }`}
              >
                <span className="truncate">{item.label}</span>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}

function SidebarGroupRail({
  group,
  pathname,
  isOpen,
  activeGroup,
  onMouseEnter,
  onMouseLeave,
  onNavigate,
}: {
  group: NavGroup;
  pathname: string;
  isOpen: boolean;
  activeGroup: boolean;
  onMouseEnter: () => void;
  onMouseLeave: () => void;
  onNavigate: () => void;
}) {
  const triggerRef = useRef<HTMLButtonElement>(null);
  const [anchor, setAnchor] = useState<{ left: number; top: number } | null>(null);
  const GroupIcon = group.icon;

  useEffect(() => {
    const raf = requestAnimationFrame(() => {
      if (!isOpen) {
        setAnchor(null);
        return;
      }
      const el = triggerRef.current;
      if (!el) return;
      const rect = el.getBoundingClientRect();
      setAnchor({ left: Math.round(rect.right + 12), top: Math.round(rect.top) });
    });
    return () => cancelAnimationFrame(raf);
  }, [isOpen]);

  const triggerClass = `flex size-11 items-center justify-center rounded-xl transition-all duration-150 ${
    activeGroup || isOpen
      ? "bg-slate-900 text-white shadow-md dark:bg-slate-100 dark:text-slate-900"
      : "text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-slate-900 dark:hover:text-white"
  }`;

  if (group.items.length === 1) {
    const item = group.items[0];
    return (
      <div className="relative my-1 flex justify-center">
        <Link
          href={item.href}
          onClick={onNavigate}
          title={group.label}
          className={triggerClass}
        >
          <GroupIcon className="size-5 shrink-0" />
        </Link>
      </div>
    );
  }

  return (
    <div
      className="relative my-1 flex justify-center"
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
    >
      <button
        ref={triggerRef}
        type="button"
        title={group.label}
        className={triggerClass}
      >
        <GroupIcon className="size-5 shrink-0" />
      </button>

      {anchor && (
        <FlyoutSubmenuPanel
          group={group}
          anchor={anchor}
          pathname={pathname}
          onNavigate={onNavigate}
        />
      )}
    </div>
  );
}

function FlyoutSubmenuPanel({
  group,
  anchor,
  pathname,
  onNavigate,
}: {
  group: NavGroup;
  anchor: { left: number; top: number };
  pathname: string;
  onNavigate: () => void;
}) {
  return (
    <div
      role="menu"
      className="fixed z-50 animate-in fade-in slide-in-from-left-2 duration-150"
      style={{ left: anchor.left, top: anchor.top }}
    >
      <div className="w-52 rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-2 shadow-xl">
        <div className="px-3 py-1.5 text-xs font-bold text-slate-400 uppercase tracking-wider border-b border-slate-100 dark:border-slate-800 mb-1">
          {group.label}
        </div>
        <div className="space-y-0.5">
          {group.items.map((item) => {
            const active = isActive(pathname, item.href);
            return (
              <Link
                key={item.href}
                href={item.href}
                onClick={onNavigate}
                className={`flex items-center gap-2.5 rounded-xl px-3 py-2 text-xs sm:text-sm transition-all ${
                  active
                    ? "bg-[#0a0a0a] font-bold text-white shadow-xs"
                    : "bg-transparent text-slate-600 hover:bg-gray-100 hover:text-slate-900"
                }`}
              >
                <span className="truncate">{item.label}</span>
              </Link>
            );
          })}
        </div>
      </div>
    </div>
  );
}

function Topbar({ onMenuClick }: { onMenuClick: () => void }) {
  const { user, logout } = useAuth();
  const pathname = usePathname();
  const name = user ? `${user.nombre} ${user.apellido}`.trim() : "Usuario";
  let activeLabel: string | null = null;
  for (const group of NAV_GROUPS) {
    const item = group.items.find((navItem) => isActive(pathname, navItem.href));
    if (item) {
      activeLabel = group.label;
      break;
    }
  }
  const initials = name
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");
  const badgeClass = ROLE_BADGE_CLASS[user?.rol ?? ""];

  return (
    <header className="sticky top-0 z-30 flex items-center justify-between gap-4 border-b border-slate-200 dark:border-slate-800 bg-white/95 dark:bg-slate-900/95 px-5 py-3 backdrop-blur-md">
      <div className="flex min-w-0 items-center gap-3">
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="lg:hidden rounded-xl border-slate-200 dark:border-slate-800"
          aria-label="Abrir menú"
          onClick={onMenuClick}
        >
          <Menu className="size-4.5" />
        </Button>
        <span className="hidden truncate text-sm font-bold text-slate-700 dark:text-slate-300 sm:inline">
          {activeLabel ?? "Centro de Producción"}
        </span>
      </div>
      <div className="flex items-center gap-3">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              type="button"
              variant="ghost"
              className="size-9 rounded-full p-0 ring-2 ring-slate-400/20"
              aria-label="Menú de usuario"
            >
              <span className="inline-flex size-9 items-center justify-center rounded-full bg-slate-200 text-xs font-bold text-slate-800 dark:bg-slate-800 dark:text-slate-200">
                {initials || "?"}
              </span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56 rounded-2xl p-1.5 shadow-xl">
            <DropdownMenuLabel className="flex flex-col px-3 py-2">
              <span className="font-bold text-slate-900 dark:text-white">{name}</span>
              <span className="text-xs font-normal text-slate-400">{user?.rol ?? ""}</span>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              variant="destructive"
              onClick={logout}
              className="rounded-xl px-3 py-2 cursor-pointer font-medium"
            >
              <LogOut className="size-4" />
              Cerrar sesión
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
}
