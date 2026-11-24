import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { CatalogoItemDto } from '../../core/models/api.models';

export interface CatalogoItemDialogData {
  item?: CatalogoItemDto;
}

export interface CatalogoItemDialogResult {
  codigo: string;
  texto: string;
  orden: number;
  activo: boolean;
  padreItemId: string | null;
}

@Component({
  selector: 'app-catalogo-item-dialog',
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
    <h2 mat-dialog-title>{{ data.item ? 'Editar ítem' : 'Nuevo ítem' }}</h2>
    <mat-dialog-content>
      <div class="campos">
        <div class="fila">
          <mat-form-field appearance="outline">
            <mat-label>Código</mat-label>
            <input matInput [(ngModel)]="codigo" autofocus />
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Orden</mat-label>
            <input matInput type="number" [(ngModel)]="orden" />
          </mat-form-field>
        </div>

        <mat-form-field appearance="outline">
          <mat-label>Texto</mat-label>
          <input matInput [(ngModel)]="texto" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Ítem padre (Id opcional)</mat-label>
          <input matInput [(ngModel)]="padreItemId" />
        </mat-form-field>

        <mat-checkbox [(ngModel)]="activo">Activo</mat-checkbox>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="!codigo.trim() || !texto.trim()" (click)="guardar()">
        Guardar
      </button>
    </mat-dialog-actions>
  `,
  styles: [
    `.campos { display: grid; gap: 0.5rem; min-width: min(26rem, 70vw); padding: 0.25rem 0 0.5rem; }
     .fila { display: grid; grid-template-columns: 1fr 8rem; gap: 0.75rem; }
     mat-checkbox { margin: 0.25rem 0; }`,
  ],
})
export class CatalogoItemDialogComponent {
  readonly data = inject<CatalogoItemDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<CatalogoItemDialogComponent, CatalogoItemDialogResult>);

  codigo = this.data.item?.codigo ?? '';
  texto = this.data.item?.texto ?? '';
  orden = this.data.item?.orden ?? 0;
  padreItemId = this.data.item?.padreItemId ?? '';
  activo = this.data.item?.activo ?? true;

  guardar(): void {
    if (!this.codigo.trim() || !this.texto.trim()) return;
    this.dialogRef.close({
      codigo: this.codigo.trim(),
      texto: this.texto.trim(),
      orden: Number(this.orden) || 0,
      activo: this.activo,
      padreItemId: this.padreItemId.trim() || null,
    });
  }
}
