import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { finalize } from 'rxjs';
import { RolDto, UsuarioDto } from '../../core/models/api.models';
import { ApiErrorService } from '../../core/services/api-error.service';
import { DialogService } from '../../core/services/dialog.service';
import { RolesService } from '../../core/services/roles.service';
import { UsuariosService } from '../../core/services/usuarios.service';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTableModule,
    HasPermissionDirective,
  ],
  templateUrl: './usuarios.html',
  styleUrl: './usuarios.scss',
})
export class UsuariosComponent implements OnInit {
  private readonly service = inject(UsuariosService);
  private readonly rolesService = inject(RolesService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly errores = inject(ApiErrorService);
  private readonly dialog = inject(DialogService);

  readonly cargando = signal(true);
  readonly usuarios = signal<UsuarioDto[]>([]);
  readonly total = signal(0);
  readonly pagina = signal(1);
  readonly tamano = signal(10);
  readonly roles = signal<RolDto[]>([]);

  readonly editando = signal<UsuarioDto | null>(null);
  readonly nuevo = signal(false);
  readonly email = signal('');
  readonly nombre = signal('');
  readonly password = signal('');
  readonly activo = signal(true);
  readonly rolesSeleccionados = signal<Set<string>>(new Set());
  readonly guardando = signal(false);

  readonly columnas = ['email', 'nombre', 'roles', 'estado', 'acciones'];

  ngOnInit(): void {
    this.cargar();
    this.rolesService.list().subscribe({
      next: (roles) => this.roles.set(roles),
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
    });
  }

  cargar(pagina = this.pagina()): void {
    this.cargando.set(true);
    this.service.list(pagina, this.tamano()).pipe(finalize(() => this.cargando.set(false))).subscribe({
      next: (resultado) => {
        this.usuarios.set(resultado.items);
        this.total.set(resultado.total);
        this.pagina.set(resultado.page);
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
    });
  }

  cambiarPagina(evento: PageEvent): void {
    this.tamano.set(evento.pageSize);
    this.cargar(evento.pageIndex + 1);
  }

  abrirNuevo(): void {
    this.editando.set(null);
    this.nuevo.set(true);
    this.email.set('');
    this.nombre.set('');
    this.password.set('');
    this.activo.set(true);
    this.rolesSeleccionados.set(new Set());
  }

  editar(usuario: UsuarioDto): void {
    this.editando.set(usuario);
    this.nuevo.set(true);
    this.email.set(usuario.email);
    this.nombre.set(usuario.nombreCompleto);
    this.password.set('');
    this.activo.set(usuario.activo);
    this.rolesSeleccionados.set(new Set(usuario.roles));
  }

  guardar(): void {
    const email = this.email().trim();
    const nombre = this.nombre().trim();
    if (!email || !nombre) {
      this.snackBar.open('Correo y nombre son obligatorios.', 'Cerrar', { duration: 4000 });
      return;
    }
    this.guardando.set(true);
    const roles = [...this.rolesSeleccionados()];
    const operacion = this.editando()
      ? this.service.update(this.editando()!.id, { email, nombreCompleto: nombre, activo: this.activo(), roles })
      : this.service.create({ email, nombreCompleto: nombre, password: this.password(), roles });
    operacion.pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: () => {
        this.snackBar.open('Usuario guardado.', 'Cerrar', { duration: 4000 });
        this.nuevo.set(false);
        this.editando.set(null);
        this.cargar(this.pagina());
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
    });
  }

  eliminar(usuario: UsuarioDto): void {
    this.dialog.confirmar({
      titulo: 'Eliminar usuario',
      mensaje: `¿Eliminar el usuario ${usuario.email}?`,
      textoConfirmar: 'Eliminar',
    }).subscribe((confirmado) => {
      if (!confirmado) return;
      this.service.delete(usuario.id).subscribe({
        next: () => {
          this.snackBar.open('Usuario eliminado.', 'Cerrar', { duration: 4000 });
          this.cargar(this.pagina());
        },
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
      });
    });
  }

  alternarRol(nombre: string, activo: boolean): void {
    const actual = new Set(this.rolesSeleccionados());
    if (activo) actual.add(nombre); else actual.delete(nombre);
    this.rolesSeleccionados.set(actual);
  }
}