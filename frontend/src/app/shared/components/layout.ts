import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { FormularioDto } from '../../core/models/api.models';
import { AuthService } from '../../core/services/auth.service';
import { FormulariosService } from '../../core/services/formularios.service';
import { ThemeService } from '../../core/services/theme.service';
import { HasPermissionDirective } from '../directives/has-permission.directive';

interface MenuItem {
  etiqueta: string;
  ruta: string;
  icono: string;
  permiso?: string;
  submenu?: boolean;
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatSidenavModule,
    MatToolbarModule,
    MatTooltipModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class LayoutComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly formulariosService = inject(FormulariosService);
  private readonly router = inject(Router);
  private readonly breakpoints = inject(BreakpointObserver);
  readonly theme = inject(ThemeService);

  readonly esMovil = computed(() => this.movil());
  readonly abierto = computed(() => !this.esMovil());
  readonly nombreUsuario = computed(() => this.auth.usuario()?.nombreCompleto ?? 'Usuario');
  readonly formulariosMenu = signal<FormularioDto[]>([]);
  readonly submenuAbierto = signal(false);
  readonly movil = signal(false);

  readonly menu = computed<MenuItem[]>(() => [
    { etiqueta: 'Mis productos', ruta: '/app/productos', icono: 'inventory_2', permiso: 'Productos.Ver' },
    {
      etiqueta: 'Mis formularios',
      ruta: '/app/mis-formularios',
      icono: 'fact_check',
      permiso: 'Formularios.Ver',
      submenu: true,
    },
    { etiqueta: 'Configuración', ruta: '/app/configuracion', icono: 'settings', permiso: 'Productos.Ver' },
    { etiqueta: 'Cambiar contraseña', ruta: '/cambiar-password', icono: 'lock' },
  ]);

  ngOnInit(): void {
    this.formulariosService.list().subscribe({
      next: (formularios) => this.formulariosMenu.set(formularios),
      error: () => this.formulariosMenu.set([]),
    });
  }

  alternarSubmenu(): void {
    this.submenuAbierto.update((abierto) => !abierto);
  }

  constructor() {
    this.breakpoints
      .observe('(max-width: 900px)')
      .pipe(takeUntilDestroyed())
      .subscribe((estado) => this.movil.set(estado.matches));
  }

  puedeVer(permiso?: string): boolean {
    return !permiso || this.auth.tienePermiso(permiso);
  }

  cerrarSesion(): void {
    this.auth.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => {
        this.auth.limpiar();
        this.router.navigate(['/login']);
      },
    });
  }
}