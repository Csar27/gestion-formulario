export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expira: string;
  debeCambiarPassword: boolean;
  usuarioId: string;
  nombreCompleto: string;
  roles: string[];
  permisos: string[];
}

export interface OpcionDto {
  valor: string;
  etiqueta: string;
}

export interface CampoDto {
  id: string;
  seccionId: string;
  etiqueta: string;
  tipo: string;
  requerido: boolean;
  orden: number;
  min: number | null;
  max: number | null;
  fuenteOpciones: 'Manual' | 'Catalogo';
  opciones: OpcionDto[];
  catalogoId: string | null;
  campoPadreId: string | null;
}

export interface SeccionDto {
  id: string;
  formularioId: string;
  titulo: string;
  orden: number;
  campos: CampoDto[];
}

export interface FormularioDto {
  id: string;
  nombre: string;
  esPasoAPaso: boolean;
  secciones: SeccionDto[];
  roles: string[];
}

export interface CampoRequest {
  etiqueta: string;
  tipo: string;
  requerido: boolean;
  orden: number;
  min: number | null;
  max: number | null;
  fuenteOpciones: 'Manual' | 'Catalogo';
  opciones: OpcionDto[];
  catalogoId: string | null;
  campoPadreId: string | null;
}

export interface SeccionRequest {
  titulo: string;
  orden: number;
  campos: CampoRequest[];
}

export interface FormularioRequest {
  nombre: string;
  esPasoAPaso: boolean;
  secciones: SeccionRequest[];
  roles: string[];
}

export interface CatalogoResumenDto {
  id: string;
  nombre: string;
  descripcion: string | null;
  activo: boolean;
  catalogoPadreId: string | null;
  cantidadItems: number;
}

export interface CatalogoItemDto {
  id: string;
  catalogoId: string;
  codigo: string;
  texto: string;
  orden: number;
  activo: boolean;
  padreItemId: string | null;
}

export interface CatalogoItemDto {
  id: string;
  catalogoId: string;
  codigo: string;
  texto: string;
  orden: number;
  activo: boolean;
  padreItemId: string | null;
}

export interface CatalogoDto extends CatalogoResumenDto {
  items: CatalogoItemDto[];
}

export interface OpcionCatalogoDto {
  id: string;
  codigo: string;
  texto: string;
}

export interface RolDto {
  id: string;
  nombre: string;
  descripcion: string | null;
  esSistema: boolean;
  permisos: string[];
}

export interface UsuarioDto {
  id: string;
  email: string;
  nombreCompleto: string;
  activo: boolean;
  debeCambiarPassword: boolean;
  roles: string[];
}

export interface UsuarioCreateRequest {
  email: string;
  nombreCompleto: string;
  password: string;
  roles: string[] | null;
}

export interface UsuarioUpdateRequest {
  email: string;
  nombreCompleto: string;
  activo: boolean;
  roles: string[] | null;
}

export interface PermisoDto {
  id: string;
  codigo: string;
  modulo: string;
  accion: string;
}

export interface RolCreateRequest {
  nombre: string;
  descripcion: string | null;
  permisos: string[] | null;
}

export interface RolUpdateRequest {
  descripcion: string | null;
}

export interface AsignarPermisosRequest {
  codigos: string[];
}

export interface ImportacionResultadoDto {
  procesadas: number;
  creadas: number;
  actualizadas: number;
  desactivadas: number;
  errores: string[];
}

export interface ValorRequest {
  campoId: string;
  valor: string | null;
}

export interface GuardarRespuestaRequest {
  id: string | null;
  formularioId: string;
  seccionId: string | null;
  valores: ValorRequest[];
  finalizar: boolean;
}

export interface RespuestaValorDto {
  campoId: string;
  etiqueta: string;
  valor: string;
  valorTexto: string | null;
}

export interface RespuestaDto {
  id: string;
  formularioId: string;
  formularioNombre: string;
  fecha: string;
  usuarioId: string;
  estado: string;
  valores: RespuestaValorDto[];
}

export interface RespuestaListItemDto {
  id: string;
  formularioId: string;
  formularioNombre: string;
  fecha: string;
  estado: string;
  cantidadValores: number;
  usuarioId: string;
}

export interface RespuestaValorHistorialDto {
  id: string;
  campoId: string;
  etiqueta: string;
  valorAnterior: string | null;
  valorAnteriorTexto: string | null;
  valorNuevo: string;
  valorNuevoTexto: string | null;
  fecha: string;
  usuarioId: string;
}

export interface ReporteValorDto {
  campoId: string;
  etiqueta: string;
  valor: string;
  valorTexto: string | null;
  valorMostrado: string;
}

export interface DashboardFormularioDto {
  formularioId: string;
  nombre: string;
  total: number;
  borradores: number;
  completas: number;
}

export interface DashboardResumenDto {
  totalRespuestas: number;
  borradores: number;
  completas: number;
  porFormulario: DashboardFormularioDto[];
}

export interface ReporteRespuestaDto {
  id: string;
  formularioId: string;
  formularioNombre: string;
  fecha: string;
  usuarioId: string;
  usuarioNombre: string;
  estado: string;
  cantidadValores: number;
  valores: ReporteValorDto[];
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  errors?: Record<string, string[]>;
}

export const TIPOS_CAMPO = [
  'Texto',
  'Entero',
  'Decimal',
  'Email',
  'Fecha',
  'Hora',
  'Moneda',
  'Porcentaje',
  'Lista',
  'Archivo',
] as const;

export interface ProductoDto {
  id: string;
  nombre: string;
  descripcion: string | null;
  activo: boolean;
  imagenUrl: string | null;
  formularioId: string | null;
  formularioNombre: string | null;
}

export interface ProductoCreateRequest {
  nombre: string;
  descripcion: string | null;
  activo: boolean;
  formularioId: string | null;
}

export interface ProductoUpdateRequest {
  nombre: string;
  descripcion: string | null;
  activo: boolean;
  formularioId: string | null;
}

export const PERMISOS = {
  formulariosVer: 'Formularios.Ver',
  formulariosCrear: 'Formularios.Crear',
  formulariosEditar: 'Formularios.Editar',
  formulariosEliminar: 'Formularios.Eliminar',
  catalogosVer: 'Catalogos.Ver',
  catalogosCrear: 'Catalogos.Crear',
  catalogosEditar: 'Catalogos.Editar',
  catalogosEliminar: 'Catalogos.Eliminar',
  respuestasVer: 'Respuestas.Ver',
  respuestasCrear: 'Respuestas.Crear',
  respuestasEliminar: 'Respuestas.Eliminar',
  reportesVer: 'Reportes.Ver',
  reportesExportar: 'Reportes.Exportar',
  rolesVer: 'Roles.Ver',
  rolesCrear: 'Roles.Crear',
  rolesEditar: 'Roles.Editar',
  rolesEliminar: 'Roles.Eliminar',
  productosVer: 'Productos.Ver',
  productosCrear: 'Productos.Crear',
  productosEditar: 'Productos.Editar',
  productosEliminar: 'Productos.Eliminar',
  usuariosVer: 'Usuarios.Ver',
  usuariosCrear: 'Usuarios.Crear',
  usuariosEditar: 'Usuarios.Editar',
  usuariosEliminar: 'Usuarios.Eliminar',
} as const;
