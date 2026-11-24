import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { CatalogoResumenDto } from '../../core/models/api.models';

export interface CatalogoDialogData {
  catalogo?: CatalogoResumenDto;
}

export interface CatalogoDialogResult {
  nombre: string;
  descripcion: string | null;
  activo: boolean;
  catalogoPadreId: string | null;
}

@Component({
  selector: 'app-catalogo-dialog',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.catalogo ? 'Editar catálogo' : 'Nuevo catálogo' }}</h2>
    <mat-dialog-content>
      <div class="campos">
        <mat-form-field appearance="outline">
          <mat-label>Nombre</mat-label>
          <input matInput [(ngModel)]="nombre" autofocus />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Descripción</mat-label>
          <input matInput [(ngModel)]="descripcion" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Catálogo padre (Id opcional)</mat-label>
          <input matInput [(ngModel)]="catalogoPadreId" />
        </mat-form-field>

        <mat-checkbox [(ngModel)]="activo">Activo</mat-checkbox>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="!nombre.trim()" (click)="guardar()">
        Guardar
      </button>
    </mat-dialog-actions>
  `,
  styles: [
    `.campos { display: grid; gap: 0.5rem; min-width: min(26rem, 70vw); padding: 0.25rem 0 0.5rem; }
     mat-checkbox { margin: 0.25rem 0; }`,
  ],
})
export class CatalogoDialogComponent {
  readonly data = inject<CatalogoDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<CatalogoDialogComponent, CatalogoDialogResult>);

  nombre = this.data.catalogo?.nombre ?? '';
  descripcion = this.data.catalogo?.descripcion ?? '';
  catalogoPadreId = this.data.catalogo?.catalogoPadreId ?? '';
  activo = this.data.catalogo?.activo ?? true;

  guardar(): void {
    if (!this.nombre.trim()) return;
    this.dialogRef.close({
      nombre: this.nombre.trim(),
      descripcion: this.descripcion.trim() || null,
      activo: this.activo,
      catalogoPadreId: this.catalogoPadreId.trim() || null,
    });
  }
}
