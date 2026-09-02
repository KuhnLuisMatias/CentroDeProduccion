import type { Metadata } from "next";
import "./globals.css";
import { AuthProvider } from "@/context/AuthContext";
import { Toaster } from "@/components/ui/sonner";

export const metadata: Metadata = {
  title: "Centro de Producción",
  description: "Sistema de gestión del centro de producción",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es">
      <body>
        <AuthProvider>{children}</AuthProvider>
        <Toaster richColors position="top-right" />
        <div aria-hidden="true" className="kuchi-soft-watermark">
          Kuchi Soft
        </div>
      </body>
    </html>
  );
}
