import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-acceso-denegado',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, RouterLink],
  template: `
    <div class="contenedor">
      <mat-card appearance="outlined">
        <mat-card-header><mat-card-title>Acceso denegado</mat-card-title></mat-card-header>
        <mat-card-content>
          <p>No tienes permiso para acceder a este recurso.</p>
          <a mat-flat-button color="primary" routerLink="/app/productos">Volver a productos</a>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: ['.contenedor { max-width: 30rem; margin: 4rem auto; padding: 1rem; }'],
})
export class AccesoDenegadoComponent {}