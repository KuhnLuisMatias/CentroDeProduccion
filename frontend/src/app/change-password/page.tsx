"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { ApiError } from "@/lib/api";

export default function ChangePasswordPage() {
  const { changePassword, logout } = useAuth();
  const router = useRouter();
  const [passwordActual, setPasswordActual] = useState("");
  const [passwordNuevo, setPasswordNuevo] = useState("");
  const [confirmacion, setConfirmacion] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (passwordNuevo !== confirmacion) {
      setError("Las contraseñas nuevas no coinciden.");
      return;
    }

    setSubmitting(true);
    try {
      await changePassword(passwordActual, passwordNuevo);
      logout();
      router.replace("/login");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "No se pudo cambiar la contraseña.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <form
        className="w-full max-w-sm rounded-lg border border-border bg-card p-8 shadow-sm"
        onSubmit={handleSubmit}
      >
        <h1 className="mb-1 text-xl font-semibold">Cambiar contraseña</h1>
        <p className="mb-6 text-sm text-muted-foreground">
          Debe cambiar su contraseña antes de continuar.
        </p>

        {error && (
          <div className="mb-4 rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {error}
          </div>
        )}

        <label className="mb-4 flex flex-col gap-1.5">
          <span className="text-sm font-medium">Contraseña actual</span>
          <input
            type="password"
            className="w-full rounded-md border border-input bg-background px-3 py-2.5 text-sm outline-none transition focus:border-ring focus:ring-2 focus:ring-ring/30"
            value={passwordActual}
            onChange={(e) => setPasswordActual(e.target.value)}
            required
            autoComplete="current-password"
          />
        </label>

        <label className="mb-4 flex flex-col gap-1.5">
          <span className="text-sm font-medium">Contraseña nueva</span>
          <input
            type="password"
            className="w-full rounded-md border border-input bg-background px-3 py-2.5 text-sm outline-none transition focus:border-ring focus:ring-2 focus:ring-ring/30"
            value={passwordNuevo}
            onChange={(e) => setPasswordNuevo(e.target.value)}
            required
            autoComplete="new-password"
          />
        </label>

        <label className="mb-4 flex flex-col gap-1.5">
          <span className="text-sm font-medium">Confirmar contraseña nueva</span>
          <input
            type="password"
            className="w-full rounded-md border border-input bg-background px-3 py-2.5 text-sm outline-none transition focus:border-ring focus:ring-2 focus:ring-ring/30"
            value={confirmacion}
            onChange={(e) => setConfirmacion(e.target.value)}
            required
            autoComplete="new-password"
          />
        </label>

        <button
          type="submit"
          className="inline-flex w-full items-center justify-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
          disabled={submitting}
        >
          {submitting ? "Guardando…" : "Cambiar contraseña"}
        </button>
      </form>
    </div>
  );
}
