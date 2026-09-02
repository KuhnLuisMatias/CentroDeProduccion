export interface User {
  usuarioId: string;
  email: string;
  nombre: string;
  apellido: string;
  rol: string;
  debeCambiarPassword: boolean;
}

export interface LoginResponse extends User {
  token: string;
  refreshToken: string;
}

export interface RefreshResponse {
  token: string;
  refreshToken: string;
}

export interface DashboardKPIs {
  produccionDia: number;
  produccionMes: number;
  stockInsumosCriticos: number;
  stockProductosTerminados: number;
  productosProximosAVencer: number;
  ventasDia: number;
  ventasMes: number;
  deudaProveedores: number;
  deudaBares: number;
  costoPromedioPorProducto: CostoPromedioItem[];
}

export interface CostoPromedioItem {
  productoId: string;
  nombre: string;
  costoUnitario: number;
}

export interface ChartDataset {
  label: string;
  data: number[];
  backgroundColor?: string;
}

export interface Chart {
  type: string;
  title: string;
  labels: string[];
  datasets: ChartDataset[];
}

export interface DashboardCharts {
  charts: Chart[];
}

// ---------------------------------------------------------------------------
// Enums (backend serializes enums as their integer values — no string converter)
// ---------------------------------------------------------------------------

export type AmbitoCategoria = 1 | 2; // Insumo=1, ProductoTerminado=2
export type TipoUnidadMedida = 1 | 2 | 3; // Masa=1, Volumen=2, Conteo=3
export type EstadoBar = 1 | 2; // Activo=1, Inactivo=2
export type CargoEmpleado = 1 | 2 | 3 | 4; // Cocinero, Empaquetador, Ayudante, Repartidor
export type CategoriaEmpleado = 1 | 2 | 3; // Produccion, Logistica, Limpieza

export const AMBITO_CATEGORIA_LABELS: Record<AmbitoCategoria, string> = {
  1: "Insumo",
  2: "Producto Terminado",
};

export const TIPO_UNIDAD_MEDIDA_LABELS: Record<TipoUnidadMedida, string> = {
  1: "Masa",
  2: "Volumen",
  3: "Conteo",
};

export const ESTADO_BAR_LABELS: Record<EstadoBar, string> = {
  1: "Activo",
  2: "Inactivo",
};

export const CARGO_EMPLEADO_LABELS: Record<CargoEmpleado, string> = {
  1: "Cocinero",
  2: "Empaquetador",
  3: "Ayudante",
  4: "Repartidor",
};

export const CATEGORIA_EMPLEADO_LABELS: Record<CategoriaEmpleado, string> = {
  1: "Producción",
  2: "Logística",
  3: "Limpieza",
};

// ---------------------------------------------------------------------------
// Paged result
// ---------------------------------------------------------------------------

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ---------------------------------------------------------------------------
// Insumos
// ---------------------------------------------------------------------------

export interface Insumo {
  id: string;
  nombre: string;
  codigoSku: string;
  categoriaId: string;
  categoria: { id: string; nombre: string } | null;
  unidadCompraId: string;
  unidadCompra: { id: string; nombre: string; simbolo: string } | null;
  unidadConsumoId: string;
  unidadConsumo: { id: string; nombre: string; simbolo: string } | null;
  factorConversion: number;
  stockMinimo: number;
  stockActual: number;
  precioUltimaCompra: number;
  proveedorPrincipalId: string | null;
  proveedorPrincipal: { id: string; nombreRazonSocial: string } | null;
  observaciones: string | null;
  activo: boolean;
  rowVersion: string;
}

export interface CreateInsumoCommand {
  nombre: string;
  codigoSku: string;
  categoriaId: string;
  unidadCompraId: string;
  unidadConsumoId: string;
  factorConversion: number;
  stockMinimo: number;
  precioUltimaCompra?: number | null;
  proveedorPrincipalId: string | null;
  observaciones: string | null;
}

export interface UpdateInsumoCommand extends CreateInsumoCommand {
  id: string;
  rowVersion: string;
}

// ---------------------------------------------------------------------------
// Proveedores
// ---------------------------------------------------------------------------

export interface Proveedor {
  id: string;
  nombreRazonSocial: string;
  cuit: string;
  direccion: string;
  telefono: string | null;
  whatsapp: string | null;
  email: string | null;
  personaContacto: string | null;
  horarioAtencion: string | null;
  categoriasProvee: string;
  tipoFactura: string;
  observaciones: string | null;
  activo: boolean;
}

export interface CreateProveedorCommand {
  nombreRazonSocial: string;
  cuit: string;
  direccion: string;
  telefono: string | null;
  whatsapp: string | null;
  email: string | null;
  personaContacto: string | null;
  horarioAtencion: string | null;
  categoriasProvee: string;
  tipoFactura: string;
  observaciones: string | null;
}

export interface UpdateProveedorCommand extends CreateProveedorCommand {
  id: string;
}

// ---------------------------------------------------------------------------
// Categorías
// ---------------------------------------------------------------------------

export interface Categoria {
  id: string;
  nombre: string;
  ambito: AmbitoCategoria;
  activo: boolean;
}

export interface CreateCategoriaCommand {
  nombre: string;
  ambito: AmbitoCategoria;
}

export interface UpdateCategoriaCommand extends CreateCategoriaCommand {
  id: string;
}

export interface CategoriasGrouped {
  insumos: Categoria[];
  productosTerminados: Categoria[];
}

// ---------------------------------------------------------------------------
// Unidades de medida
// ---------------------------------------------------------------------------

export interface UnidadMedida {
  id: string;
  nombre: string;
  simbolo: string;
  tipo: TipoUnidadMedida;
  activo: boolean;
}

export interface CreateUnidadMedidaCommand {
  nombre: string;
  simbolo: string;
  tipo: TipoUnidadMedida;
}

export interface UpdateUnidadMedidaCommand extends CreateUnidadMedidaCommand {
  id: string;
}

// ---------------------------------------------------------------------------
// Bares
// ---------------------------------------------------------------------------

export interface BarListItem {
  id: string;
  nombre: string;
  direccion: string;
  encargado: string | null;
  estado: EstadoBar;
  margenReventaPorcentaje: number;
}

export interface Bar {
  id: string;
  nombre: string;
  direccion: string;
  encargado: string | null;
  telefono: string | null;
  horarioRecepcion: string | null;
  margenReventaPorcentaje: number;
  estado: EstadoBar;
  fechaCreacion: string;
  rowVersion: string;
}

export interface CreateBarCommand {
  nombre: string;
  direccion: string;
  encargado: string | null;
  telefono: string | null;
  horarioRecepcion: string | null;
  margenReventaPorcentaje: number;
}

export interface UpdateBarCommand extends CreateBarCommand {
  id: string;
  rowVersion: string;
}

export interface DeleteBarCommand {
  id: string;
  rowVersion: string;
}

export interface ReactivateBarCommand {
  id: string;
  rowVersion: string;
}

// ---------------------------------------------------------------------------
// Empleados
// ---------------------------------------------------------------------------

export interface Empleado {
  id: string;
  nombre: string;
  apellido: string;
  dni: string;
  cargo: CargoEmpleado;
  tarifaPorHora: number;
  categoria: CategoriaEmpleado;
  activo: boolean;
  rowVersion: string;
}

export interface CreateEmpleadoCommand {
  nombre: string;
  apellido: string;
  dni: string;
  cargo: CargoEmpleado;
  tarifaPorHora: number;
  categoria: CategoriaEmpleado;
}

export interface UpdateEmpleadoCommand extends CreateEmpleadoCommand {
  id: string;
  activo: boolean;
  rowVersion: string;
}

export interface DeleteEmpleadoCommand {
  id: string;
  rowVersion: string;
}

// ---------------------------------------------------------------------------
// Productos terminados
// ---------------------------------------------------------------------------

export type EstadoProductoTerminado = 1 | 2 | 3 | 4; // Disponible=1, Reservado=2, EnTransito=3, Vencido=4

export const ESTADO_PRODUCTO_TERMINADO_LABELS: Record<EstadoProductoTerminado, string> = {
  1: "Disponible",
  2: "Reservado",
  3: "En tránsito",
  4: "Vencido",
};

export interface CreateProductoTerminadoCommand {
  nombre: string;
  codigoSku: string;
  categoriaId: string;
  unidadMedidaId: string;
}

export interface UpdateProductoTerminadoCommand extends CreateProductoTerminadoCommand {
  id: string;
  stockMinimo: number;
  rowVersion: string;
}

export interface ProductoTerminado {
  id: string;
  nombre: string;
  codigoSku: string;
  categoriaId: string;
  unidadMedidaId: string;
  stockActual: number;
  stockMinimo: number;
  costoUnitario: number;
  fechaProduccion: string;
  fechaVencimiento: string;
  lote: string;
  estado: number;
  activo: boolean;
  rowVersion: string;
  categoria?: Categoria | null;
  unidadMedida?: UnidadMedida | null;
  recetaId?: string | null;
  receta?: { id: string; nombre: string } | null;
}

// ---------------------------------------------------------------------------
// Recetas
// ---------------------------------------------------------------------------

export type EstadoReceta = 1 | 2 | 3; // Activa=1, Inactiva=2, EnDesarrollo=3

export const ESTADO_RECETA_LABELS: Record<EstadoReceta, string> = {
  1: "Activa",
  2: "Inactiva",
  3: "En desarrollo",
};

export interface RecetaInsumo {
  id: string;
  recetaId: string;
  insumoId: string | null;
  insumo: { id: string; nombre: string } | null;
  recetaOrigenId: string | null;
  recetaOrigen: { id: string; nombre: string } | null;
  cantidadNecesaria: number;
  unidadMedidaId: string;
  unidadMedida: { id: string; nombre: string; simbolo: string } | null;
  observaciones: string | null;
}

export interface Receta {
  id: string;
  nombre: string;
  codigoSku: string;
  categoriaId: string;
  categoria: { id: string; nombre: string } | null;
  unidadMedidaId: string | null;
  unidadMedida: { id: string; nombre: string; simbolo: string } | null;
  descripcion: string | null;
  estado: EstadoReceta;
  version: number;
  activo: boolean;
  fechaCreacion: string;
  rowVersion: string;
  insumos: RecetaInsumo[];
}

export interface RecetaInsumoDto {
  insumoId: string | null;
  recetaOrigenId: string | null;
  cantidadNecesaria: number;
  unidadMedidaId: string;
  observaciones?: string | null;
}

export interface CreateRecetaCommand {
  nombre: string;
  codigoSku: string;
  categoriaId: string;
  unidadMedidaId: string;
  descripcion: string | null;
  insumos: RecetaInsumoDto[];
}

export interface UpdateRecetaCommand extends CreateRecetaCommand {
  id: string;
  estado: EstadoReceta;
}

export interface CosteoReceta {
  recetaId: string;
  nombre: string;
  costoInsumos: number;
  costoUnitario: number;
  cicloDetectado: boolean;
}

export interface RecetaVersion {
  id: string;
  recetaId: string;
  version: number;
  nombre: string;
  codigoSku: string;
  detallesJson: string;
  fechaCreacion: string;
}

// ---------------------------------------------------------------------------
// Producción
// ---------------------------------------------------------------------------

export type EstadoProduccion = 1 | 2 | 3; // Borrador=1, Confirmada=2, Cancelada=3

export const ESTADO_PRODUCCION_LABELS: Record<EstadoProduccion, string> = {
  1: "Borrador",
  2: "Confirmada",
  3: "Cancelada",
};

// Display info of one consumed insumo line (GET /api/produccion/{id})
export interface ProduccionInsumoInsumoInfo {
  id: string;
  nombre: string;
  codigoSku: string;
  unidadConsumoId: string;
}

// One editable consumed-insumo line of the run (Producción simple)
export interface ProduccionInsumoConsumido {
  id: string;
  produccionId: string;
  insumoId: string;
  insumo: ProduccionInsumoInsumoInfo | null;
  cantidad: number;
  observaciones: string | null;
}

export interface Produccion {
  id: string;
  recetaId: string;
  receta: { id: string; nombre: string; unidadMedidaSimbolo?: string | null } | null;
  lote: string;
  fecha: string;
  responsableId: string;
  responsable: { id: string; nombre: string; apellido: string } | null;
  estado: EstadoProduccion;
  observaciones: string | null;
  cantidadProducida: number;
  fechaVencimiento: string | null;
  costoTotalInsumos: number;
  costoTotal: number;
  // Base64 string — System.Text.Json serializes byte[] as base64
  rowVersion: string;
  insumosConsumidos: ProduccionInsumoConsumido[];
}

// POST /api/produccion — creates a Borrador run pre-loaded with the receta BOM
export interface CreateProduccionCommand {
  recetaId: string;
  observaciones: string | null;
}

export interface CreateProduccionResponse {
  id: string;
  estado: EstadoProduccion;
}

// One line of PUT /api/produccion/{id}/insumos — replaces the FULL consumption list
export interface LineaInsumoProduccionDto {
  insumoId: string;
  cantidad: number; // must be > 0
  observaciones: string | null;
}

export interface UpdateProduccionInsumosCommand {
  produccionId: string;
  lineas: LineaInsumoProduccionDto[];
}

// POST /api/produccion/{id}/confirm — consumes edited lines, creates PT + lote
export interface ConfirmProduccionCommand {
  produccionId: string;
  cantidadProducida: number; // must be > 0
  // Base64 rowVersion fetched FRESH from GET /api/produccion/{id} right before submit
  rowVersion: string;
}

export interface ConfirmProduccionResponse {
  produccionId: string;
  productoTerminadoId: string;
  lote: string;
  estado: EstadoProduccion;
}

export interface CancelProduccionCommand {
  produccionId: string;
  motivo: string | null;
}

// ---------------------------------------------------------------------------
// Stock
// ---------------------------------------------------------------------------

export type TipoMovimientoStock =
  | 1
  | 2
  | 3
  | 4
  | 5
  | 6
  | 7
  | 8
  | 9
  | 10; // Compra, ConsumoProduccion, Reventa, AjustePositivo, AjusteNegativo, DevolucionProveedor, Produccion, VentaBar, DevolucionBar, BajaPorVencimiento

export const TIPO_MOVIMIENTO_STOCK_LABELS: Record<TipoMovimientoStock, string> = {
  1: "Compra",
  2: "Consumo producción",
  3: "Reventa",
  4: "Ajuste positivo",
  5: "Ajuste negativo",
  6: "Devolución proveedor",
  7: "Producción",
  8: "Venta bar",
  9: "Devolución bar",
  10: "Baja por vencimiento",
};

export interface StockOverview {
  totalInsumosActivos: number;
  insumosCriticos: number;
}

export interface StockAlert {
  id: string;
  nombre: string;
  codigoSku: string;
  stockActual: number;
  stockMinimo: number;
  unidadConsumoSimbolo: string | null;
  proveedorPrincipalNombreRazonSocial: string | null;
  proveedorPrincipalTelefono: string | null;
  proveedorPrincipalWhatsApp: string | null;
  precioUltimaCompra: number;
}

export interface MovimientoStock {
  id: string;
  insumoId: string | null;
  productoTerminadoId: string | null;
  produccionId: string | null;
  tipo: TipoMovimientoStock;
  cantidad: number;
  cantidadOriginal: number;
  unidadOriginalId: string;
  unidadOriginal: { id: string; nombre: string; simbolo: string } | null;
  factorConversionAplicado: number;
  precioUnitario: number | null;
  motivo: string;
  documentoOrigen: string | null;
  usuarioId: string;
  usuario: { id: string; nombre: string; apellido: string } | null;
  fecha: string;
}

export interface RegisterMovementCommand {
  insumoId: string | null;
  productoTerminadoId: string | null;
  tipo: TipoMovimientoStock;
  cantidad: number;
  unidadOriginalId: string;
  precioUnitario: number | null;
  motivo: string;
  documentoOrigen: string | null;
}

// ---------------------------------------------------------------------------
// Órdenes de compra
// ---------------------------------------------------------------------------

// Referential document: lifecycle is Borrador → Enviada → Cancelada (no stock/receptions).
export type EstadoOrdenCompra = 1 | 2 | 6; // Borrador, Enviada, Cancelada
export type MetodoPago = 1 | 2 | 3 | 4 | 5; // Efectivo, Transferencia, TarjetaDebito, TarjetaCredito, Cheque

export const ESTADO_ORDEN_COMPRA_LABELS: Record<EstadoOrdenCompra, string> = {
  1: "Borrador",
  2: "Enviada",
  6: "Cancelada",
};

export const METODO_PAGO_LABELS: Record<MetodoPago, string> = {
  1: "Efectivo",
  2: "Transferencia",
  3: "Tarjeta de débito",
  4: "Tarjeta de crédito",
  5: "Cheque",
};

export interface OrdenCompraItem {
  id: string;
  insumoId: string;
  insumoNombre: string;
  cantidadPedida: number;
  precioUnitario: number;
  subtotal: number;
}

export interface OrdenCompra {
  id: string;
  numero: number;
  proveedorId: string;
  proveedorNombre: string;
  estado: EstadoOrdenCompra;
  fechaCreacion: string;
  fechaEnvio: string | null;
  observaciones: string | null;
  total: number;
  items: OrdenCompraItem[];
  rowVersion: string;
}

export interface CreateOrdenCompraItemCommand {
  insumoId: string;
  cantidadPedida: number;
  precioUnitario: number;
}

export interface CreateOrdenCompraCommand {
  proveedorId: string;
  observaciones: string | null;
  items: CreateOrdenCompraItemCommand[];
}

export interface UpdateOrdenCompraCommand extends CreateOrdenCompraCommand {
  id: string;
  // Base64 string — System.Text.Json serializes byte[] as base64
  rowVersion: string;
}

// ---------------------------------------------------------------------------
// Facturas de compra (antes "Pagos a proveedores") — ruta API: /pagos-proveedor
// ---------------------------------------------------------------------------

export interface PagoMetodo {
  tipo: MetodoPago;
  monto: number;
  referencia: string | null;
}

export interface PagoInsumo {
  insumoId: string;
  insumoNombre: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
}

export interface PagoProveedor {
  id: string;
  numero: number;
  proveedorId: string;
  proveedorNombre: string;
  fechaPago: string;
  montoTotal: number;
  observaciones: string | null;
  metodos: PagoMetodo[];
  insumos: PagoInsumo[];
}

export interface PagoInsumoCommand {
  insumoId: string;
  cantidad: number;
  precioUnitario: number;
}

export interface CreatePagoProveedorCommand {
  proveedorId: string;
  fechaPago: string;
  montoTotal: number;
  observaciones: string | null;
  insumos: PagoInsumoCommand[];
}

// ---------------------------------------------------------------------------
// Remitos
// ---------------------------------------------------------------------------

export type EstadoRemito = 1 | 2 | 3 | 4; // Pendiente, EnProceso, Enviado, Cancelado
export type TipoLineaRemito = 1 | 2; // ProductoTerminado, Insumo

export const ESTADO_REMITO_LABELS: Record<EstadoRemito, string> = {
  1: "Pendiente",
  2: "En proceso",
  3: "Enviado",
  4: "Cancelado",
};

export const TIPO_LINEA_REMITO_LABELS: Record<TipoLineaRemito, string> = {
  1: "Producto terminado",
  2: "Insumo",
};

export interface RemitoLinea {
  id: string;
  tipoLinea: TipoLineaRemito;
  productoTerminadoId: string | null;
  productoTerminadoNombre: string;
  insumoId: string | null;
  insumoNombre: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
  lote: string | null;
}

export interface Remito {
  id: string;
  numeroRemito: number;
  fecha: string;
  barId: string;
  barNombre: string;
  barDireccion: string;
  estado: EstadoRemito;
  observaciones: string | null;
  entregadoPor: string | null;
  recibidoPor: string | null;
  fechaEnvio: string | null;
  total: number;
  lineas: RemitoLinea[];
  rowVersion: string;
}

export interface RemitoListItem {
  id: string;
  numeroRemito: number;
  fecha: string;
  barId: string;
  barNombre: string;
  estado: EstadoRemito;
  total: number;
}

export interface CreateRemitoLineaCommand {
  tipoLinea: TipoLineaRemito;
  productoTerminadoId: string | null;
  insumoId: string | null;
  cantidad: number;
  lote: string | null;
}

export interface CreateRemitoCommand {
  barId: string;
  observaciones: string | null;
  entregadoPor: string | null;
  recibidoPor: string | null;
  lineas: CreateRemitoLineaCommand[];
}

export interface UpdateRemitoCommand extends CreateRemitoCommand {
  id: string;
  rowVersion: string;
}

export interface UpdateEstadoRemitoCommand {
  remitoId: string;
  estado: EstadoRemito;
  rowVersion: string;
}

export interface CancelarRemitoCommand {
  remitoId: string;
  rowVersion: string;
}

export interface ConfirmRemitoCommand {
  remitoId: string;
  rowVersion: string;
}

// ---------------------------------------------------------------------------
// Devoluciones
// ---------------------------------------------------------------------------

export interface DevolucionLinea {
  id: string;
  productoTerminadoNombre: string;
  cantidad: number;
  lote: string | null;
  precioUnitarioOriginal: number;
  subtotal: number;
}

export interface Devolucion {
  id: string;
  numero: number;
  remitoId: string;
  remitoNumeroRemito: number;
  fecha: string;
  observaciones: string | null;
  recibidoPor: string | null;
  barId: string;
  barNombre: string;
  totalDevolucion: number;
  lineas: DevolucionLinea[];
}

export interface DevolucionListItem {
  id: string;
  numero: number;
  remitoId: string;
  remitoNumeroRemito: number;
  barId: string;
  barNombre: string;
  fecha: string;
  total: number;
}

export interface CreateDevolucionLineaCommand {
  productoTerminadoId: string;
  cantidad: number;
  lote: string | null;
}

export interface CreateDevolucionCommand {
  remitoId: string;
  observaciones: string | null;
  recibidoPor: string | null;
  lineas: CreateDevolucionLineaCommand[];
}

// ---------------------------------------------------------------------------
// Pagos de bares
// ---------------------------------------------------------------------------

export interface PagoBarMetodo {
  tipo: MetodoPago;
  monto: number;
  referencia: string | null;
}

export interface PagoBarItem {
  remitoId: string;
  remitoNumeroRemito: number;
  montoAplicado: number;
}

export interface PagoBar {
  id: string;
  numero: number;
  barId: string;
  barNombre: string;
  fechaPago: string;
  montoTotal: number;
  observaciones: string | null;
  metodos: PagoBarMetodo[];
  items: PagoBarItem[];
}

export interface PagoBarList {
  id: string;
  numero: number;
  barId: string;
  barNombre: string;
  fechaPago: string;
  montoTotal: number;
  metodoCount: number;
}

export interface PagoBarMetodoCommand {
  tipo: MetodoPago;
  monto: number;
  referencia: string | null;
}

export interface PagoBarItemCommand {
  remitoId: string;
  montoAplicado: number;
}

export interface CreatePagoBarCommand {
  barId: string;
  fechaPago: string | null;
  montoTotal: number;
  observaciones: string | null;
  metodos: PagoBarMetodoCommand[];
  items: PagoBarItemCommand[];
}

// ---------------------------------------------------------------------------
// Cuenta corriente
// ---------------------------------------------------------------------------

export type TipoMovimientoCtaCte = 1 | 2 | 3 | 4; // Compra, Pago, NotaDebito, NotaCredito
export type TipoMovimientoCtaCteBar = 1 | 2 | 3 | 4 | 5 | 6; // Remito, Pago, NotaCredito, NotaDebito, Devolucion, Compensacion

export const TIPO_MOVIMIENTO_CTA_CTE_LABELS: Record<TipoMovimientoCtaCte, string> = {
  1: "Compra",
  2: "Pago",
  3: "Nota de débito",
  4: "Nota de crédito",
};

export const TIPO_MOVIMIENTO_CTA_CTE_BAR_LABELS: Record<TipoMovimientoCtaCteBar, string> = {
  1: "Remito",
  2: "Pago",
  3: "Nota de crédito",
  4: "Nota de débito",
  5: "Devolución",
  6: "Compensación",
};

export interface CuentaCorrienteMovimiento {
  id: string;
  tipoMovimiento: TipoMovimientoCtaCte;
  monto: number;
  fecha: string;
  referencia: string | null;
  ordenCompraId: string | null;
  pagoProveedorId: string | null;
  saldo: number;
}

export interface CuentaCorrienteBarMovimiento {
  id: string;
  tipoMovimiento: TipoMovimientoCtaCteBar;
  monto: number;
  referencia: string | null;
  fecha: string;
  saldoAcumulado: number;
  remitoId: string | null;
  devolucionId: string | null;
  pagoBarId: string | null;
}

export interface RegisterNotaDebitoProveedorCommand {
  proveedorId: string;
  monto: number;
  referencia: string | null;
}

export interface RegisterNotaCreditoProveedorCommand {
  proveedorId: string;
  monto: number;
  referencia: string | null;
}

export interface RegisterNotaDebitoBarCommand {
  barId: string;
  monto: number;
  referencia: string | null;
  fecha: string | null;
}

export interface RegisterNotaCreditoBarCommand {
  barId: string;
  monto: number;
  referencia: string | null;
  fecha: string | null;
}

export interface RegisterCompensacionBarCommand {
  barId: string;
  monto: number;
  referencia: string | null;
  fecha: string | null;
}

// ---------------------------------------------------------------------------
// Reportes
// ---------------------------------------------------------------------------

export interface ReportMetadata {
  generatedAt: string;
  dateRangeFrom: string | null;
  dateRangeTo: string | null;
  filterDescription: string | null;
  reportType: string | null;
  reportTitle: string | null;
}

export interface PlanillaCostosRecetaInfo {
  id: string;
  nombre: string;
  categoria: string;
}

export interface PlanillaCostosResumen {
  costoInsumosLote: number;
  costoUnitario: number;
}

export interface ReportEnvelope<T = Record<string, unknown>> {
  items: T[];
  metadata: ReportMetadata;
  totalValorizado?: number;
  saldoFinal?: number;
  totalGeneral?: number;
  receta?: PlanillaCostosRecetaInfo;
  costos?: PlanillaCostosResumen;
  totales?: Record<string, number>;
}

// ---------------------------------------------------------------------------
// Inventario (toma de inventario)
// ---------------------------------------------------------------------------

export type TipoInventario = 1 | 2; // Insumo=1, ProductoTerminado=2
export type EstadoInventario = 1 | 2 | 3; // Abierta=1, EnProceso=2, Cerrada=3

export const TIPO_INVENTARIO_LABELS: Record<TipoInventario, string> = {
  1: "Insumos",
  2: "Productos terminados",
};

export const ESTADO_INVENTARIO_LABELS: Record<EstadoInventario, string> = {
  1: "Abierta",
  2: "En proceso",
  3: "Cerrada",
};

export interface InventarioSesionListItem {
  id: string;
  fecha: string;
  tipo: TipoInventario;
  estado: EstadoInventario;
  totalItems: number;
  diferenciaTotal: number;
}

export interface InventarioConteoDto {
  id: string;
  insumoId: string | null;
  insumoNombre: string | null;
  productoTerminadoId: string | null;
  productoTerminadoNombre: string | null;
  cantidadSistema: number;
  cantidadContada: number;
  diferencia: number;
  conteoOk: boolean;
  observaciones: string | null;
}

export interface InventarioSesionDetail {
  id: string;
  tipo: TipoInventario;
  fecha: string;
  estado: EstadoInventario;
  responsableId: string;
  notas: string | null;
  diferenciaTotal: number;
  conteos: InventarioConteoDto[];
  rowVersion: string;
}

export interface CreateInventarioSesionResponse {
  id: string;
  tipo: TipoInventario;
  fecha: string;
  estado: EstadoInventario;
  totalItems: number;
}

export interface RegistrarConteoCommand {
  inventarioSesionId: string;
  conteoId: string;
  cantidadContada: number;
  observaciones?: string | null;
}

export interface RegistrarConteoResponse {
  conteoId: string;
  cantidadSistema: number;
  cantidadContada: number;
  diferencia: number;
  conteoOk: boolean;
}

export interface ConfirmInventarioSesionCommand {
  inventarioSesionId: string;
  rowVersion: string;
}

export interface ConfirmInventarioSesionResponse {
  sesionId: string;
  estado: EstadoInventario;
  ajustesGenerados: number;
  diferenciaTotal: number;
}
