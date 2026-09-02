"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { apiClient, clearTokens, getRefreshToken, getToken, setTokens } from "@/lib/api";
import type { LoginResponse, User } from "@/lib/types";

interface AuthContextValue {
  user: User | null;
  token: string | null;
  refreshToken: string | null;
  loading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<User>;
  logout: () => void;
  changePassword: (passwordActual: string, passwordNuevo: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

const USER_KEY = "cdp_user";

function readStoredUser(): User | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as User) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [refreshToken, setRefreshToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Restore the persisted session only after hydration. Reading localStorage
    // in a lazy useState initializer (or during render) would make the first
    // client render differ from the server HTML (no localStorage on the server),
    // breaking hydration. This is a one-shot client-only side effect, not a
    // cascading render.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setUser(readStoredUser());
    setToken(getToken());
    setRefreshToken(getRefreshToken());
    setLoading(false);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const result = await apiClient<LoginResponse>("/auth/login", {
      method: "POST",
      body: { email, password },
    });

    const userData: User = {
      usuarioId: result.usuarioId,
      email: result.email,
      nombre: result.nombre,
      apellido: result.apellido,
      rol: result.rol,
      debeCambiarPassword: result.debeCambiarPassword,
    };

    setTokens(result.token, result.refreshToken);
    if (typeof window !== "undefined") {
      window.localStorage.setItem(USER_KEY, JSON.stringify(userData));
    }
    setUser(userData);
    setToken(result.token);
    setRefreshToken(result.refreshToken);
    return userData;
  }, []);

  const logout = useCallback(() => {
    clearTokens();
    if (typeof window !== "undefined") {
      window.localStorage.removeItem(USER_KEY);
    }
    setUser(null);
    setToken(null);
    setRefreshToken(null);
  }, []);

  const changePassword = useCallback(async (passwordActual: string, passwordNuevo: string) => {
    await apiClient<void>("/auth/change-password", {
      method: "POST",
      body: { currentPassword: passwordActual, newPassword: passwordNuevo },
    });
  }, []);

  useEffect(() => {
    const onUnauthorized = () => {
      clearTokens();
      if (typeof window !== "undefined") {
        window.localStorage.removeItem(USER_KEY);
      }
      setUser(null);
      setToken(null);
      setRefreshToken(null);
    };
    window.addEventListener("cdp:unauthorized", onUnauthorized);
    return () => window.removeEventListener("cdp:unauthorized", onUnauthorized);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      refreshToken,
      loading,
      isAuthenticated: !!token && !!user,
      login,
      logout,
      changePassword,
    }),
    [user, token, refreshToken, loading, login, logout, changePassword],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
