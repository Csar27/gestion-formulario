import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiErrorService } from '../../core/services/api-error.service';
import { FormulariosService } from '../../core/services/formularios.service';
import { FormularioDto } from '../../core/models/api.models';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog';

@Component({
  selector: 'app-formularios-list',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatTooltipModule,
    RouterLink,
    HasPermissionDirective,
  ],
  templateUrl: './formularios-list.html',
  styleUrl: './formularios-list.scss',
})
export class FormulariosListComponent implements OnInit {
  private readonly service = inject(FormulariosService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly errores = inject(ApiErrorService);

  readonly columnas = ['nombre', 'estructura', 'acciones'];
  readonly formularios = signal<FormularioDto[]>([]);
  readonly cargando = signal(true);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.service
      .list()
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (datos) => this.formularios.set(datos),
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
      });
  }

  totalCampos(formulario: FormularioDto): number {
    return formulario.secciones.reduce((total, seccion) => total + seccion.campos.length, 0);
  }

  eliminar(formulario: FormularioDto): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          titulo: 'Eliminar formulario',
          mensaje: `¿Eliminar "${formulario.nombre}"? Esta acción no se puede deshacer.`,
        },
      })
      .afterClosed()
      .subscribe((respuesta) => {
        if (!respuesta) {
          return;
        }
        this.service.delete(formulario.id).subscribe({
          next: () => {
            this.snackBar.open('Formulario eliminado.', 'Cerrar', { duration: 4000 });
            this.cargar();
          },
          error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
        });
      });
  }
}
