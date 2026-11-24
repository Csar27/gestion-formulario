import { Component, Input, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DATE_LOCALE, MatDateFormats, provideNativeDateAdapter } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CampoDto, OpcionDto } from '../../core/models/api.models';
import { ArchivosService } from '../../core/services/archivos.service';

const FORMATOS_FECHA: MatDateFormats = {
  parse: { dateInput: 'YYYY-MM-DD' },
  display: {
    dateInput: 'DD/MM/YYYY',
    monthYearLabel: 'MMM Y',
    dateA11yLabel: 'LL',
    monthYearA11yLabel: 'MMMM Y',
  },
};

@Component({
  selector: 'app-campo-renderer',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatButtonModule,
    MatIconModule,
  ],
  providers: [provideNativeDateAdapter(FORMATOS_FECHA), { provide: MAT_DATE_LOCALE, useValue: 'es-ES' }],
  template: `
    <mat-form-field appearance="outline" class="campo">
      <mat-label>{{ campo.etiqueta }}{{ campo.requerido ? ' *' : '' }}</mat-label>

      @switch (campo.tipo) {
        @case ('Lista') {
          <mat-select [formControl]="control">
            @for (opcion of opciones; track opcion.valor) {
              <mat-option [value]="opcion.valor">{{ opcion.etiqueta }}</mat-option>
            }
          </mat-select>
        }
        @case ('Entero') {
          <input matInput type="number" step="1" [formControl]="control" />
        }
        @case ('Decimal') {
          <input matInput type="number" step="0.01" [formControl]="control" />
        }
        @case ('Moneda') {
          <input matInput type="number" step="0.01" [formControl]="control" />
        }
        @case ('Porcentaje') {
          <input matInput type="number" step="0.01" min="0" max="100" [formControl]="control" />
        }
        @case ('Email') {
          <input matInput type="email" [formControl]="control" />
        }
        @case ('Fecha') {
          <input matInput [matDatepicker]="selectorFecha" [formControl]="control" />
        }
        @case ('Hora') {
          <input matInput type="time" [formControl]="control" />
        }
        @case ('Archivo') {
          <input type="file" (change)="subir($event)" />
        }
        @default {
          <input matInput type="text" [formControl]="control" />
        }
      }

      @if (campo.tipo === 'Archivo' && control.value) {
        <a [href]="urlArchivo(control.value)" target="_blank" matSuffix>
          <mat-icon>attach_file</mat-icon>
        </a>
      }

      @if (campo.tipo === 'Fecha') {
        <mat-datepicker-toggle matIconSuffix [for]="selectorFecha"></mat-datepicker-toggle>
      }
      <mat-datepicker #selectorFecha></mat-datepicker>

      @if (control.hasError('required')) {
        <mat-error>Este campo es obligatorio.</mat-error>
      }
      @if (control.hasError('email') || control.hasError('pattern')) {
        <mat-error>El formato del valor no es válido.</mat-error>
      }
      @if (control.hasError('matDatepickerParse')) {
        <mat-error>Introduce una fecha válida.</mat-error>
      }
    </mat-form-field>
  `,
  styles: ['.campo, .mat-mdc-form-field { margin: 0 !important; width: 100%; } .mat-mdc-text-field-wrapper { padding-top: 0 !important; padding-bottom: 0 !important; } .mat-mdc-form-field-subscript-wrapper { min-height: 0 !important; height: auto !important; }'],
})
export class CampoRendererComponent {
  @Input({ required: true }) campo!: CampoDto;
  @Input({ required: true }) control!: FormControl;
  @Input() opciones: OpcionDto[] = [];

  private readonly archivosService = inject(ArchivosService);
  private readonly snackBar = inject(MatSnackBar);

  subir(evento: Event): void {
    const entrada = evento.target as HTMLInputElement;
    const archivo = entrada.files?.item(0);
    if (!archivo) return;
    this.archivosService.subir(archivo).subscribe({
      next: (resultado) => {
        this.control.setValue(resultado.url);
        this.snackBar.open('Archivo subido.', 'Cerrar', { duration: 3000 });
      },
      error: () => this.snackBar.open('No se pudo subir el archivo.', 'Cerrar', { duration: 5000 }),
    });
  }

  urlArchivo(valor: unknown): string {
    const texto = String(valor ?? '');
    if (!texto) return '#';
    if (texto.startsWith('http')) return texto;
    return environment.apiUrl.replace('/api', '') + texto;
  }
}