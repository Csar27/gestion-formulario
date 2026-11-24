import { Component, computed, inject, OnInit, signal, ViewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormularioDto } from '../../core/models/api.models';
import { FormulariosService } from '../../core/services/formularios.service';
import { ReportesService } from '../../core/services/reportes.service';
import { ReporteRespuestaDto } from '../../core/models/api.models';

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatCardModule,
    DatePipe,
  ],
  templateUrl: './reportes.html',
  styleUrl: './reportes.scss',
})
export class ReportesComponent implements OnInit {
  private readonly reportesService = inject(ReportesService);
  private readonly formulariosService = inject(FormulariosService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  @ViewChild(MatPaginator) paginator?: MatPaginator;

  readonly formularios = signal<FormularioDto[]>([]);
  readonly formularioSeleccionado = signal<FormularioDto | null>(null);
  readonly cargando = signal(false);
  readonly datos = signal<ReporteRespuestaDto[]>([]);
  readonly total = signal(0);
  readonly pagina = signal(1);
  readonly tamano = signal(20);

  readonly filtroFormularioId = signal('');
  readonly filtroEstado = signal('');
  readonly filtroDesde = signal('');
  readonly filtroHasta = signal('');

  readonly campos = computed(() => this.formularioSeleccionado()?.secciones.flatMap((s) => s.campos) ?? []);

  readonly displayedColumns = computed(() => [
    'formulario',
    'fecha',
    'usuario',
    'estado',
    ...this.campos().map((c) => c.id),
    'acciones',
  ]);

  ngOnInit(): void {
    this.formulariosService.list().subscribe({
      next: (formularios) => this.formularios.set(formularios),
      error: () => this.snackBar.open('No se pudieron cargar los formularios.', 'Cerrar', { duration: 5000 }),
    });
  }

  alCambiarFormulario(id: string): void {
    this.filtroFormularioId.set(id);
    const formulario = this.formularios().find((item) => item.id === id) ?? null;
    this.formularioSeleccionado.set(formulario);
  }

  consultar(pagina = 1): void {
    this.cargando.set(true);
    this.pagina.set(pagina);
    this.reportesService
      .list({
        formularioId: this.filtroFormularioId() || undefined,
        estado: this.filtroEstado() || undefined,
        desde: this.filtroDesde() || undefined,
        hasta: this.filtroHasta() || undefined,
        page: pagina,
        pageSize: this.tamano(),
      })
      .subscribe({
        next: (resultado) => {
          this.datos.set(resultado.items);
          this.total.set(resultado.total);
          this.cargando.set(false);
        },
        error: () => {
          this.cargando.set(false);
          this.snackBar.open('No se pudo generar el reporte.', 'Cerrar', { duration: 7000 });
        },
      });
  }

  cambiarPagina(evento: PageEvent): void {
    this.tamano.set(evento.pageSize);
    this.consultar(evento.pageIndex + 1);
  }

  exportar(formato: 'excel' | 'pdf'): void {
    this.reportesService
      .exportar(formato, {
        formularioId: this.filtroFormularioId() || undefined,
        estado: this.filtroEstado() || undefined,
        desde: this.filtroDesde() || undefined,
        hasta: this.filtroHasta() || undefined,
      })
      .subscribe({
        next: (respuesta) => {
          const blob = respuesta.body;
          if (!blob) return;
          const nombre = this.extraerNombre(respuesta.headers.get('content-disposition')) ??
            (formato === 'excel' ? 'reporte.xlsx' : 'reporte.pdf');
          const url = URL.createObjectURL(blob);
          const enlace = document.createElement('a');
          enlace.href = url;
          enlace.download = nombre;
          enlace.click();
          URL.revokeObjectURL(url);
        },
        error: () => this.snackBar.open('No se pudo exportar el reporte.', 'Cerrar', { duration: 7000 }),
      });
  }

  valorCampo(fila: ReporteRespuestaDto, campoId: string): string {
    return fila.valores.find((valor) => valor.campoId === campoId)?.valorMostrado ?? '';
  }

  etiquetaCampo(campoId: string): string {
    return this.campos().find((campo) => campo.id === campoId)?.etiqueta ?? campoId;
  }

  ver(respuesta: ReporteRespuestaDto): void {
    this.router.navigate(['/app/mis-formularios', respuesta.formularioId, 'llenar'], {
      queryParams: { respuesta: respuesta.id, modo: 'ver' },
    });
  }

  editar(respuesta: ReporteRespuestaDto): void {
    this.router.navigate(['/app/mis-formularios', respuesta.formularioId, 'llenar'], {
      queryParams: { respuesta: respuesta.id, modo: 'editar' },
    });
  }

  historial(respuesta: ReporteRespuestaDto): void {
    this.router.navigate(['/app/respuestas', respuesta.id, 'historial']);
  }

  private extraerNombre(contentDisposition: string | null): string | null {
    if (!contentDisposition) return null;
    const coincidencia = /filename="?([^";]+)"?/i.exec(contentDisposition);
    return coincidencia?.[1] ?? null;
  }
}