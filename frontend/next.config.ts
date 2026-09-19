import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  allowedDevOrigins: ["localhost", "192.168.1.4"],
  async redirects() {
    return [
      { source: "/produccion", destination: "/ordenes-produccion", permanent: true },
      { source: "/produccion/:path*", destination: "/ordenes-produccion/:path*", permanent: true },
      { source: "/pagos", destination: "/facturas-pagos", permanent: true },
      { source: "/pagos/:path*", destination: "/facturas-pagos/:path*", permanent: true },
      { source: "/remitos", destination: "/pedidos", permanent: true },
      { source: "/remitos/:path*", destination: "/pedidos/:path*", permanent: true },
      { source: "/cuenta-corriente", destination: "/cuentas-corrientes", permanent: true },
      { source: "/cuenta-corriente/:path*", destination: "/cuentas-corrientes/:path*", permanent: true },
      { source: "/unidades", destination: "/unidades-medida", permanent: true },
      { source: "/unidades/:path*", destination: "/unidades-medida/:path*", permanent: true },
    ];
  },
};

export default nextConfig;
