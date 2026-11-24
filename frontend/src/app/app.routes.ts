import { Routes } from '@angular/router';
import { PERMISOS } from './core/models/api.models';
import { authGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login').then((m) => m.LoginComponent),
  },
  {
    path: 'cambiar-password',
    canActivate: [authGuard],
    loadComponent: () => import('./features/auth/cambiar-password').then((m) => m.CambiarPasswordComponent),
  },
  {
    path: 'app',
    loadComponent: () => import('./shared/components/layout').then((m) => m.LayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'productos' },
      {
        path: 'formularios',
        canActivate: [permissionGuard(PERMISOS.formulariosVer)],
        loadComponent: () => import('./features/formularios/formularios-list').then((m) => m.FormulariosListComponent),
      },
      {
        path: 'formularios/nuevo',
        canActivate: [permissionGuard(PERMISOS.formulariosCrear)],
        loadComponent: () => import('./features/formularios/formulario-editor').then((m) => m.FormularioEditorComponent),
      },
      {
        path: 'formularios/:id/editar',
        canActivate: [permissionGuard(PERMISOS.formulariosEditar)],
        loadComponent: () => import('./features/formularios/formulario-editor').then((m) => m.FormularioEditorComponent),
      },
      {
        path: 'formularios/:id/vista-previa',
        canActivate: [permissionGuard(PERMISOS.formulariosVer)],
        data: { vistaPrevia: true },
        loadComponent: () => import('./features/renderer/formulario-renderer').then((m) => m.FormularioRendererComponent),
      },
      {
        path: 'productos',
        canActivate: [permissionGuard(PERMISOS.productosVer)],
        loadComponent: () => import('./features/mis-productos/mis-productos').then((m) => m.MisProductosComponent),
      },
      {
        path: 'configuracion',
        canActivate: [permissionGuard(PERMISOS.productosVer)],
        loadComponent: () => import('./features/configuracion/configuracion').then((m) => m.ConfiguracionComponent),
      },
      {
        path: 'configuracion/productos',
        canActivate: [permissionGuard(PERMISOS.productosVer)],
        loadComponent: () => import('./features/productos/productos').then((m) => m.ProductosComponent),
      },
      {
        path: 'configuracion/productos/nuevo',
        canActivate: [permissionGuard(PERMISOS.productosCrear)],
        loadComponent: () => import('./features/productos/producto-editor').then((m) => m.ProductoEditorComponent),
      },
      {
        path: 'configuracion/productos/:id/editar',
        canActivate: [permissionGuard(PERMISOS.productosEditar)],
        loadComponent: () => import('./features/productos/producto-editor').then((m) => m.ProductoEditorComponent),
      },
      {
        path: 'mis-formularios',
        canActivate: [permissionGuard(PERMISOS.formulariosVer)],
        loadComponent: () => import('./features/mis-formularios/mis-formularios').then((m) => m.MisFormulariosComponent),
      },
      {
        path: 'mis-formularios/:id/llenar',
        canActivate: [permissionGuard(PERMISOS.respuestasCrear)],
        loadComponent: () => import('./features/renderer/formulario-renderer').then((m) => m.FormularioRendererComponent),
      },
      {
        path: 'catalogos',
        canActivate: [permissionGuard(PERMISOS.catalogosVer)],
        loadComponent: () => import('./features/catalogos/catalogos-list').then((m) => m.CatalogosListComponent),
      },
      {
        path: 'reportes',
        canActivate: [permissionGuard(PERMISOS.reportesVer)],
        loadComponent: () => import('./features/reportes/reportes').then((m) => m.ReportesComponent),
      },
      {
        path: 'dashboard',
        canActivate: [permissionGuard(PERMISOS.reportesVer)],
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.DashboardComponent),
      },
      {
        path: 'respuestas/:id/historial',
        canActivate: [permissionGuard(PERMISOS.respuestasVer)],
        loadComponent: () => import('./features/historial/historial').then((m) => m.HistorialComponent),
      },
      {
        path: 'roles',
        canActivate: [permissionGuard(PERMISOS.rolesVer)],
        loadComponent: () => import('./features/roles/roles').then((m) => m.RolesComponent),
      },
      {
        path: 'usuarios',
        canActivate: [permissionGuard(PERMISOS.usuariosVer)],
        loadComponent: () => import('./features/usuarios/usuarios').then((m) => m.UsuariosComponent),
      },
      {
        path: 'acceso-denegado',
        loadComponent: () => import('./features/auth/acceso-denegado').then((m) => m.AccesoDenegadoComponent),
      },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'app/productos' },
  { path: '**', redirectTo: 'app/productos' },
];
