import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { DashboardResumenDto, FormularioDto } from '../../core/models/api.models';
import { FormulariosService } from '../../core/services/formularios.service';
import { ReportesService } from '../../core/services/reportes.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent implements OnInit {
  private readonly service = inject(ReportesService);
  private readonly formulariosService = inject(FormulariosService);
  private readonly router = inject(Router);

  readonly cargando = signal(true);
  readonly datos = signal<DashboardResumenDto | null>(null);
  readonly formularios = signal<FormularioDto[]>([]);
  readonly filtroFormularioId = signal('');
  readonly filtroDesde = signal('');
  readonly filtroHasta = signal('');
  readonly columnas = ['nombre', 'total', 'borradores', 'completas'];

  ngOnInit(): void {
    this.formulariosService.list().subscribe({
      next: (formularios) => this.formularios.set(formularios),
    });
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.service
      .dashboard({
        formularioId: this.filtroFormularioId() || undefined,
        desde: this.filtroDesde() || undefined,
        hasta: this.filtroHasta() || undefined,
      })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (datos) => this.datos.set(datos),
        error: () => this.datos.set(null),
      });
  }

  irReportes(): void {
    this.router.navigate(['/app/reportes']);
  }

  porcentaje(valor: number): number {
    const total = this.datos()?.totalRespuestas || 1;
    return Math.round((valor / total) * 100);
  }
}