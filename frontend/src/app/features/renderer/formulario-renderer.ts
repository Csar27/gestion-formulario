import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import {
  CampoDto,
  FormularioDto,
  GuardarRespuestaRequest,
  OpcionDto,
  RespuestaDto,
  ValorRequest,
} from '../../core/models/api.models';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';
import { CatalogosService } from '../../core/services/catalogos.service';
import { FormulariosService } from '../../core/services/formularios.service';
import { RespuestasService } from '../../core/services/respuestas.service';
import { CampoRendererComponent } from '../../shared/components/campo-renderer';

@Component({
  selector: 'app-formulario-renderer',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatStepperModule,
    CampoRendererComponent,
  ],
  templateUrl: './formulario-renderer.html',
  styleUrl: './formulario-renderer.scss',
})
export class FormularioRendererComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly formulariosService = inject(FormulariosService);
  private readonly respuestasService = inject(RespuestasService);
  private readonly catalogosService = inject(CatalogosService);
  private readonly auth = inject(AuthService);
  private readonly ruta = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly errores = inject(ApiErrorService);
  private readonly destroyRef = inject(DestroyRef);

  readonly esVistaPrevia = this.ruta.snapshot.data['vistaPrevia'] === true;
  readonly soloLectura = this.ruta.snapshot.queryParamMap.get('modo') === 'ver';
  readonly respuestaSolicitada = this.ruta.snapshot.queryParamMap.get('respuesta');
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly formulario = signal<FormularioDto | null>(null);
  readonly respuestaId = signal<string | null>(null);
  readonly opcionesPorCampo = signal<Record<string, OpcionDto[]>>({});

  readonly form = this.fb.group({
    secciones: this.fb.array([]),
  });

  ngOnInit(): void {
    const id = this.ruta.snapshot.paramMap.get('id');
    if (!id) {
      this.snackBar.open('No se indicó el formulario.', 'Cerrar', { duration: 5000 });
      this.volver();
      return;
    }

    this.formulariosService
      .get(id)
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (formulario) => {
          this.formulario.set(formulario);
          this.construir(formulario);
          if (this.respuestaSolicitada) {
            this.respuestasService.get(this.respuestaSolicitada).subscribe({
              next: (detalle) => {
                this.aplicarValores(formulario, detalle);
                this.cargarOpciones(formulario);
                if (this.soloLectura) {
                  this.form.disable({ emitEvent: false });
                }
              },
              error: (error) => {
                this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 });
                this.cargarOpciones(formulario);
              },
            });
            return;
          }
          this.cargarBorrador(formulario);
        },
        error: (error) => {
          this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 });
          this.volver();
        },
      });
  }

  get secciones(): FormArray {
    return this.form.controls.secciones as FormArray;
  }

  seccion(indice: number): FormGroup {
    return this.secciones.at(indice) as FormGroup;
  }

  control(indiceSeccion: number, campo: CampoDto): FormControl {
    return this.seccion(indiceSeccion).get(campo.id) as FormControl;
  }

  opciones(campoId: string): OpcionDto[] {
    return this.opcionesPorCampo()[campoId] ?? [];
  }

  guardarBorrador(seccionId?: string): void {
    this.enviar(false, seccionId ?? null);
  }

  finalizar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snackBar.open('Revisa los campos obligatorios antes de finalizar.', 'Cerrar', { duration: 6000 });
      return;
    }
    this.enviar(true, null);
  }

  volver(): void {
    this.router.navigate([this.esVistaPrevia ? '/app/formularios' : '/app/mis-formularios']);
  }

  private enviar(finalizar: boolean, seccionId: string | null): void {
    const formulario = this.formulario();
    if (!formulario || this.esVistaPrevia) {
      return;
    }

    const valores: ValorRequest[] = this.secciones.controls.flatMap((controlSeccion) =>
      Object.entries((controlSeccion as FormGroup).getRawValue()).map(([campoId, valor]) => ({
        campoId,
        valor: valor === null || valor === undefined || valor === '' ? null : String(valor),
      })),
    );

    const solicitud: GuardarRespuestaRequest = {
      id: this.respuestaId(),
      formularioId: formulario.id,
      seccionId,
      valores,
      finalizar,
    };

    this.guardando.set(true);
    this.respuestasService
      .guardar(solicitud)
      .pipe(finalize(() => this.guardando.set(false)))
      .subscribe({
        next: (respuesta) => {
          this.respuestaId.set(respuesta.id);
          if (finalizar) {
            this.snackBar.open('Formulario finalizado correctamente.', 'Cerrar', { duration: 5000 });
            this.volver();
          } else {
            this.snackBar.open('Borrador guardado.', 'Cerrar', { duration: 4000 });
          }
        },
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
      });
  }

  private cargarBorrador(formulario: FormularioDto): void {
    if (this.esVistaPrevia) {
      this.cargarOpciones(formulario);
      return;
    }

    this.respuestasService
      .list({ formularioId: formulario.id, estado: 'Borrador', pageSize: 1 })
      .subscribe({
        next: (resultado) => {
          const usuarioId = this.auth.usuario()?.usuarioId;
          const borrador = resultado.items.find(
            (item) => item.usuarioId === usuarioId && item.estado === 'Borrador',
          );
          if (!borrador) {
            this.cargarOpciones(formulario);
            return;
          }
          this.respuestasService.get(borrador.id).subscribe({
            next: (detalle) => {
              this.aplicarValores(formulario, detalle);
              this.cargarOpciones(formulario);
            },
            error: (error) => {
              this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 });
              this.cargarOpciones(formulario);
            },
          });
        },
        error: (error) => {
          this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 });
          this.cargarOpciones(formulario);
        },
      });
  }

  private aplicarValores(formulario: FormularioDto, respuesta: RespuestaDto): void {
    this.respuestaId.set(respuesta.id);
    formulario.secciones.forEach((seccion, indiceSeccion) => {
      seccion.campos.forEach((campo) => {
        const valor = respuesta.valores.find((item) => item.campoId === campo.id);
        if (valor) {
          this.control(indiceSeccion, campo).setValue(valor.valor, { emitEvent: false });
        }
      });
    });
  }

  private construir(formulario: FormularioDto): void {
    this.secciones.clear();
    formulario.secciones.forEach((seccion) => {
      const controles: Record<string, FormControl> = {};
      seccion.campos.forEach((campo) => {
        const validadores: ValidatorFn[] = [];
        if (campo.requerido && !this.esVistaPrevia) {
          validadores.push(Validators.required);
        }
        if (campo.tipo === 'Email') {
          validadores.push(Validators.email);
        }
        if (campo.tipo === 'Fecha') {
          validadores.push(Validators.pattern(/^\d{4}-\d{2}-\d{2}$/));
        }
        if (campo.tipo === 'Hora') {
          validadores.push(Validators.pattern(/^\d{2}:\d{2}$/));
        }
        controles[campo.id] = this.fb.control<string | null>(null, validadores);
      });
      this.secciones.push(this.fb.group(controles));
    });
  }

  private cargarOpciones(formulario: FormularioDto): void {
    formulario.secciones.forEach((seccion, indiceSeccion) => {
      seccion.campos.forEach((campo) => {
        if (campo.tipo !== 'Lista') {
          return;
        }

        if (campo.fuenteOpciones === 'Manual') {
          this.ponerOpciones(campo.id, campo.opciones);
          return;
        }

        if (!campo.catalogoId) {
          this.ponerOpciones(campo.id, []);
          return;
        }

        if (!campo.campoPadreId) {
          this.catalogosService
            .opciones(campo.catalogoId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (items) => this.ponerOpciones(campo.id, this.mapear(items)),
              error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
            });
          return;
        }

        const controlPadre = this.seccion(indiceSeccion).get(campo.campoPadreId);
        if (!controlPadre) {
          return;
        }

        const cargar = (padreItemId: string | null): void => {
          if (!padreItemId) {
            this.ponerOpciones(campo.id, []);
            return;
          }
          this.catalogosService
            .opciones(campo.catalogoId!, padreItemId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (items) => {
                const opciones = this.mapear(items);
                this.ponerOpciones(campo.id, opciones);
                const actual = this.seccion(indiceSeccion).get(campo.id)?.value as string | null;
                if (actual && !opciones.some((opcion) => opcion.valor === actual)) {
                  this.seccion(indiceSeccion).get(campo.id)?.setValue(null);
                }
              },
              error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
            });
        };

        cargar(controlPadre.value as string | null);
        controlPadre.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(cargar);
      });
    });
  }

  private ponerOpciones(campoId: string, opciones: OpcionDto[]): void {
    this.opcionesPorCampo.update((actual) => ({ ...actual, [campoId]: opciones }));
  }

  private mapear(items: { id: string; codigo: string; texto: string }[]): OpcionDto[] {
    return items.map((item) => ({ valor: item.id, etiqueta: `${item.codigo} · ${item.texto}` }));
  }
}
