import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import { PermisoDto, RolDto } from '../../core/models/api.models';
import { ApiErrorService } from '../../core/services/api-error.service';
import { DialogService } from '../../core/services/dialog.service';
import { RolesService } from '../../core/services/roles.service';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './roles.html',
  styleUrl: './roles.scss',
})
export class RolesComponent implements OnInit {
  private readonly service = inject(RolesService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly errores = inject(ApiErrorService);
  private readonly dialog = inject(DialogService);

  readonly cargando = signal(true);
  readonly roles = signal<RolDto[]>([]);
  readonly permisosDisponibles = signal<PermisoDto[]>([]);
  readonly seleccionado = signal<RolDto | null>(null);

  readonly nuevoNombre = signal('');
  readonly nuevaDescripcion = signal('');
  readonly nuevosPermisos = signal<Set<string>>(new Set());

  readonly descripcionEdicion = signal('');
  readonly permisosEdicion = signal<Set<string>>(new Set());

  readonly columnas = ['nombre', 'descripcion', 'sistema', 'acciones'];
  readonly permisosAgrupados = computed(() => {
    const grupos = new Map<string, PermisoDto[]>();
    for (const permiso of this.permisosDisponibles()) {
      const grupo = grupos.get(permiso.modulo) ?? [];
      grupo.push(permiso);
      grupos.set(permiso.modulo, grupo);
    }
    return [...grupos.entries()];
  });

  ngOnInit(): void {
    this.cargar();
    this.service.permisosDisponibles().subscribe({
      next: (permisos) => this.permisosDisponibles.set(permisos),
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
    });
  }

  cargar(): void {
    this.cargando.set(true);
    this.service.list().pipe(finalize(() => this.cargando.set(false))).subscribe({
      next: (roles) => this.roles.set(roles),
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
    });
  }

  seleccionar(rol: RolDto): void {
    this.seleccionado.set(rol);
    this.descripcionEdicion.set(rol.descripcion ?? '');
    this.service.getPermisos(rol.id).subscribe({
      next: (permisos) => this.permisosEdicion.set(new Set(permisos)),
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
    });
  }

  crear(): void {
    const nombre = this.nuevoNombre().trim();
    if (!nombre) {
      this.snackBar.open('El nombre del rol es obligatorio.', 'Cerrar', { duration: 4000 });
      return;
    }
    this.service
      .create({ nombre, descripcion: this.nuevaDescripcion().trim() || null, permisos: [...this.nuevosPermisos()] })
      .subscribe({
        next: () => {
          this.snackBar.open('Rol creado correctamente.', 'Cerrar', { duration: 4000 });
          this.nuevoNombre.set('');
          this.nuevaDescripcion.set('');
          this.nuevosPermisos.set(new Set());
          this.cargar();
        },
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
      });
  }

  guardarDescripcion(): void {
    const rol = this.seleccionado();
    if (!rol) return;
    this.service.update(rol.id, { descripcion: this.descripcionEdicion().trim() || null }).subscribe({
      next: (actualizado) => {
        this.snackBar.open('Rol actualizado.', 'Cerrar', { duration: 4000 });
        this.seleccionado.set(actualizado);
        this.cargar();
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
    });
  }

  guardarPermisos(): void {
    const rol = this.seleccionado();
    if (!rol) return;
    this.service.setPermisos(rol.id, { codigos: [...this.permisosEdicion()] }).subscribe({
      next: () => {
        this.snackBar.open('Permisos actualizados.', 'Cerrar', { duration: 4000 });
        this.cargar();
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
    });
  }

  eliminar(rol: RolDto): void {
    if (rol.esSistema) {
      this.snackBar.open('No se puede eliminar un rol de sistema.', 'Cerrar', { duration: 5000 });
      return;
    }
    this.dialog.confirmar({
      titulo: 'Eliminar rol',
      mensaje: `¿Eliminar el rol ${rol.nombre}?`,
      textoConfirmar: 'Eliminar',
    }).subscribe((confirmado) => {
      if (!confirmado) return;
      this.service.delete(rol.id).subscribe({
        next: () => {
          this.snackBar.open('Rol eliminado.', 'Cerrar', { duration: 4000 });
          if (this.seleccionado()?.id === rol.id) this.seleccionado.set(null);
          this.cargar();
        },
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
      });
    });
  }

  alternarPermisoNuevo(codigo: string, activo: boolean): void {
    const actual = new Set(this.nuevosPermisos());
    if (activo) actual.add(codigo); else actual.delete(codigo);
    this.nuevosPermisos.set(actual);
  }

  alternarPermisoEdicion(codigo: string, activo: boolean): void {
    const actual = new Set(this.permisosEdicion());
    if (activo) actual.add(codigo); else actual.delete(codigo);
    this.permisosEdicion.set(actual);
  }

  tienePermiso(set: Set<string>, codigo: string): boolean {
    return set.has(codigo);
  }
}