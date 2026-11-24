import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RouterLink } from '@angular/router';
import { forkJoin, finalize, Observable } from 'rxjs';
import { FormularioDto, RolDto, SeccionDto } from '../../core/models/api.models';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';
import { FormulariosService } from '../../core/services/formularios.service';
import { RespuestasService } from '../../core/services/respuestas.service';
import { RolesService } from '../../core/services/roles.service';

interface FormularioAcceso {
  formulario: FormularioDto;
  accesos: string[];
  tieneBorrador: boolean;
}

@Component({
  selector: 'app-mis-formularios',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    RouterLink,
  ],
  templateUrl: './mis-formularios.html',
  styleUrl: './mis-formularios.scss',
})
export class MisFormulariosComponent implements OnInit {
  private readonly formulariosService = inject(FormulariosService);
  private readonly rolesService = inject(RolesService);
  private readonly respuestasService = inject(RespuestasService);
  private readonly auth = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly errores = inject(ApiErrorService);

  readonly items = signal<FormularioAcceso[]>([]);
  readonly cargando = signal(true);
  readonly totalCampos = (total: number, seccion: SeccionDto): number => total + seccion.campos.length;

  ngOnInit(): void {
    const cargas: Observable<unknown>[] = [
      this.formulariosService.list(),
      this.rolesService.list(),
      this.respuestasService.list({ pageSize: 100 }),
    ];

    forkJoin(cargas)
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: ([formularios, roles, respuestas]) => {
          const listaFormularios = formularios as FormularioDto[];
          const listaRoles = roles as RolDto[];
          const usuarioId = this.auth.usuario()?.usuarioId;
          const borradores = new Set(
            (respuestas as { items: { formularioId: string; estado: string; usuarioId: string }[] }).items
              .filter(
                (respuesta) =>
                  respuesta.estado === 'Borrador' && respuesta.usuarioId === usuarioId,
              )
              .map((respuesta) => respuesta.formularioId),
          );

          const nombresRoles = this.auth.usuario()?.roles ?? [];
          const esAdmin = nombresRoles.some((nombre) => nombre.toLowerCase() === 'admin');
          const idsRoles = listaRoles
            .filter((rol) => nombresRoles.some((nombre) => nombre.toLowerCase() === rol.nombre.toLowerCase()))
            .map((rol) => rol.id);

          const visibles = esAdmin
            ? listaFormularios
            : listaFormularios.filter(
                (formulario) =>
                  formulario.roles.length === 0 ||
                  formulario.roles.some((id) => idsRoles.includes(id)),
              );

          this.items.set(
            visibles.map((formulario) => ({
              formulario,
              accesos:
                formulario.roles.length === 0
                  ? ['Todos los usuarios']
                  : formulario.roles.map(
                      (id) => listaRoles.find((rol) => rol.id === id)?.nombre ?? id,
                    ),
              tieneBorrador: borradores.has(formulario.id),
            })),
          );
        },
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
      });
  }
}
