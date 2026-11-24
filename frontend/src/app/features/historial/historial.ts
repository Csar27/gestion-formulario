import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import { RespuestaValorHistorialDto } from '../../core/models/api.models';
import { RespuestasService } from '../../core/services/respuestas.service';

@Component({
  selector: 'app-historial',
  standalone: true,
  imports: [DatePipe, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './historial.html',
  styleUrl: './historial.scss',
})
export class HistorialComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(RespuestasService);

  readonly cargando = signal(true);
  readonly datos = signal<RespuestaValorHistorialDto[]>([]);
  readonly columnas = ['campo', 'anterior', 'nuevo', 'fecha', 'usuario'];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.volver();
      return;
    }
    this.service.historial(id).pipe(finalize(() => this.cargando.set(false))).subscribe({
      next: (datos) => this.datos.set(datos),
      error: () => this.datos.set([]),
    });
  }

  volver(): void {
    this.router.navigate(['/app/reportes']);
  }
}