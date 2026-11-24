import { Component, inject, OnInit, signal } from '@angular/core';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, finalize, Observable } from 'rxjs';
import {
  CampoDto,
  CatalogoResumenDto,
  FormularioDto,
  FormularioRequest,
  RolDto,
  TIPOS_CAMPO,
} from '../../core/models/api.models';
import { ApiErrorService } from '../../core/services/api-error.service';
import { CatalogosService } from '../../core/services/catalogos.service';
import { FormulariosService } from '../../core/services/formularios.service';
import { RolesService } from '../../core/services/roles.service';

@Component({
  selector: 'app-formulario-editor',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
    RouterLink,
  ],
  templateUrl: './formulario-editor.html',
  styleUrl: './formulario-editor.scss',
})
export class FormularioEditorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(FormulariosService);
  private readonly catalogosService = inject(CatalogosService);
  private readonly rolesService = inject(RolesService);
  private readonly ruta = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly errores = inject(ApiErrorService);

  readonly tipos = TIPOS_CAMPO;
  readonly id = this.ruta.snapshot.paramMap.get('id');
  readonly catalogos = signal<CatalogoResumenDto[]>([]);
  readonly roles = signal<RolDto[]>([]);
  readonly cargando = signal(true);
  readonly guardando = signal(false);

  readonly form = this.fb.group({
    nombre: this.fb.control('', Validators.required),
    esPasoAPaso: this.fb.control(false),
    roles: this.fb.control<string[]>([]),
    secciones: this.fb.array([]),
  });

  ngOnInit(): void {
    const cargas: Observable<unknown>[] = [this.catalogosService.list(), this.rolesService.list()];
    if (this.id) {
      cargas.push(this.service.get(this.id));
    }

    forkJoin(cargas)
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: ([catalogos, roles, formulario]) => {
          this.catalogos.set(catalogos as CatalogoResumenDto[]);
          this.roles.set(roles as RolDto[]);
          if (formulario) {
            this.cargarFormulario(formulario as FormularioDto);
          } else {
            this.agregarSeccion();
          }
        },
        error: (error) => {
          this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 });
          this.router.navigate(['/app/formularios']);
        },
      });
  }

  get secciones(): FormArray {
    return this.form.controls.secciones as FormArray;
  }

  seccion(indice: number): FormGroup {
    return this.secciones.at(indice) as FormGroup;
  }

  campos(indiceSeccion: number): FormArray {
    return this.seccion(indiceSeccion).controls['campos'] as FormArray;
  }

  campo(indiceSeccion: number, indiceCampo: number): FormGroup {
    return this.campos(indiceSeccion).at(indiceCampo) as FormGroup;
  }

  opciones(campo: FormGroup): FormArray {
    return campo.controls['opciones'] as FormArray;
  }

  esLista(campo: FormGroup): boolean {
    return campo.controls['tipo'].value === 'Lista';
  }

  esCatalogo(campo: FormGroup): boolean {
    return this.esLista(campo) && campo.controls['fuenteOpciones'].value === 'Catalogo';
  }

  camposComoPadre(campoActual: FormGroup): { id: string; etiqueta: string }[] {
    const resultado: { id: string; etiqueta: string }[] = [];
    this.secciones.controls.forEach((seccionControl) => {
      const lista = (seccionControl as FormGroup).controls['campos'] as FormArray;
      lista.controls.forEach((control) => {
        const grupo = control as FormGroup;
        const id = grupo.controls['id'].value as string | null;
        if (id && grupo !== campoActual) {
          resultado.push({ id, etiqueta: (grupo.controls['etiqueta'].value as string) || id });
        }
      });
    });
    return resultado;
  }

  agregarSeccion(): void {
    this.secciones.push(this.crearSeccion('', this.secciones.length + 1));
  }

  quitarSeccion(indice: number): void {
    this.secciones.removeAt(indice);
    this.renumerar();
  }

  subirSeccion(indice: number): void {
    if (indice === 0) return;
    const actual = this.secciones.at(indice);
    this.secciones.removeAt(indice);
    this.secciones.insert(indice - 1, actual);
    this.renumerar();
  }

  bajarSeccion(indice: number): void {
    if (indice >= this.secciones.length - 1) return;
    const actual = this.secciones.at(indice);
    this.secciones.removeAt(indice);
    this.secciones.insert(indice + 1, actual);
    this.renumerar();
  }

  agregarCampo(indiceSeccion: number): void {
    this.campos(indiceSeccion).push(this.crearCampo(undefined, this.campos(indiceSeccion).length + 1));
  }

  quitarCampo(indiceSeccion: number, indiceCampo: number): void {
    this.campos(indiceSeccion).removeAt(indiceCampo);
    this.renumerar();
  }

  subirCampo(indiceSeccion: number, indiceCampo: number): void {
    if (indiceCampo === 0) return;
    const lista = this.campos(indiceSeccion);
    const actual = lista.at(indiceCampo);
    lista.removeAt(indiceCampo);
    lista.insert(indiceCampo - 1, actual);
    this.renumerar();
  }

  bajarCampo(indiceSeccion: number, indiceCampo: number): void {
    const lista = this.campos(indiceSeccion);
    if (indiceCampo >= lista.length - 1) return;
    const actual = lista.at(indiceCampo);
    lista.removeAt(indiceCampo);
    lista.insert(indiceCampo + 1, actual);
    this.renumerar();
  }

  agregarOpcion(campo: FormGroup): void {
    this.opciones(campo).push(
      this.fb.group({ valor: this.fb.control('', Validators.required), etiqueta: this.fb.control('', Validators.required) }),
    );
  }

  quitarOpcion(campo: FormGroup, indice: number): void {
    this.opciones(campo).removeAt(indice);
  }

  alCambiarTipo(campo: FormGroup): void {
    if (!this.esLista(campo)) {
      campo.patchValue({ fuenteOpciones: 'Manual', catalogoId: null, campoPadreId: null });
      this.opciones(campo).clear();
    }
  }

  alCambiarFuente(campo: FormGroup): void {
    if (this.esCatalogo(campo)) {
      this.opciones(campo).clear();
    } else {
      campo.patchValue({ catalogoId: null, campoPadreId: null });
    }
  }

  probarOpciones(campo: FormGroup): void {
    const catalogoId = campo.controls['catalogoId'].value as string | null;
    if (!catalogoId) {
      this.snackBar.open('Selecciona un catálogo primero.', 'Cerrar', { duration: 4000 });
      return;
    }
    const padreItemId = campo.controls['campoPadreId'].value as string | null;
    this.catalogosService.opciones(catalogoId, padreItemId).subscribe({
      next: (opciones) =>
        this.snackBar.open(`${opciones.length} opción(es) disponible(s).`, 'Cerrar', { duration: 4000 }),
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
    });
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snackBar.open('Revisa los campos obligatorios.', 'Cerrar', { duration: 5000 });
      return;
    }

    const solicitud: FormularioRequest = {
      nombre: this.form.controls.nombre.value ?? '',
      esPasoAPaso: this.form.controls.esPasoAPaso.value ?? false,
      roles: this.form.controls.roles.value ?? [],
      secciones: this.secciones.controls.map((controlSeccion) => {
        const grupoSeccion = controlSeccion as FormGroup;
        const listaCampos = grupoSeccion.controls['campos'] as FormArray;
        return {
          titulo: (grupoSeccion.controls['titulo'].value as string) ?? '',
          orden: Number(grupoSeccion.controls['orden'].value) || 1,
          campos: listaCampos.controls.map((controlCampo) => {
            const grupo = controlCampo as FormGroup;
            const tipo = grupo.controls['tipo'].value as string;
            const fuente = this.esLista(grupo) ? (grupo.controls['fuenteOpciones'].value as 'Manual' | 'Catalogo') : 'Manual';
            const min = grupo.controls['min'].value;
            const max = grupo.controls['max'].value;
            return {
              etiqueta: (grupo.controls['etiqueta'].value as string) ?? '',
              tipo,
              requerido: Boolean(grupo.controls['requerido'].value),
              orden: Number(grupo.controls['orden'].value) || 1,
              min: min === null || min === undefined || min === '' ? null : Number(min),
              max: max === null || max === undefined || max === '' ? null : Number(max),
              fuenteOpciones: fuente,
              opciones:
                fuente === 'Manual'
                  ? this.opciones(grupo).controls.map((opcion) => {
                      const grupoOpcion = opcion as FormGroup;
                      return {
                        valor: (grupoOpcion.controls['valor'].value as string) ?? '',
                        etiqueta: (grupoOpcion.controls['etiqueta'].value as string) ?? '',
                      };
                    })
                  : [],
              catalogoId: fuente === 'Catalogo' ? (grupo.controls['catalogoId'].value as string | null) : null,
              campoPadreId: fuente === 'Catalogo' ? (grupo.controls['campoPadreId'].value as string | null) : null,
            };
          }),
        };
      }),
    };

    this.guardando.set(true);
    const peticion = this.id
      ? this.service.update(this.id, solicitud)
      : this.service.create(solicitud);

    peticion
      .pipe(finalize(() => this.guardando.set(false)))
      .subscribe({
        next: (formulario) => {
          this.snackBar.open('Formulario guardado correctamente.', 'Cerrar', { duration: 4000 });
          if (!this.id) {
            this.router.navigate(['/app/formularios', formulario.id, 'editar']);
          }
        },
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
      });
  }

  volver(): void {
    this.router.navigate(['/app/formularios']);
  }

  private cargarFormulario(formulario: FormularioDto): void {
    this.form.patchValue({
      nombre: formulario.nombre,
      esPasoAPaso: formulario.esPasoAPaso,
      roles: formulario.roles ?? [],
    });
    this.secciones.clear();
    formulario.secciones.forEach((seccion) => {
      const grupo = this.crearSeccion(seccion.titulo, seccion.orden);
      const lista = grupo.controls['campos'] as FormArray;
      seccion.campos.forEach((campo) => lista.push(this.crearCampo(campo, campo.orden)));
      this.secciones.push(grupo);
    });
    if (this.secciones.length === 0) {
      this.agregarSeccion();
    }
  }

  private crearSeccion(titulo: string, orden: number): FormGroup {
    return this.fb.group({
      id: this.fb.control<string | null>(null),
      titulo: this.fb.control(titulo, Validators.required),
      orden: this.fb.control(orden, Validators.required),
      campos: this.fb.array([]),
    });
  }

  private crearCampo(campo: CampoDto | undefined, orden: number): FormGroup {
    return this.fb.group({
      id: this.fb.control<string | null>(campo?.id ?? null),
      etiqueta: this.fb.control(campo?.etiqueta ?? '', Validators.required),
      tipo: this.fb.control(campo?.tipo ?? 'Texto', Validators.required),
      requerido: this.fb.control(campo?.requerido ?? false),
      orden: this.fb.control(orden, Validators.required),
      min: this.fb.control<number | null>(campo?.min ?? null),
      max: this.fb.control<number | null>(campo?.max ?? null),
      fuenteOpciones: this.fb.control<'Manual' | 'Catalogo'>(
        (campo?.fuenteOpciones as 'Manual' | 'Catalogo' | undefined) ?? 'Manual',
      ),
      opciones: this.fb.array(
        (campo?.opciones ?? []).map((opcion) =>
          this.fb.group({
            valor: this.fb.control(opcion.valor, Validators.required),
            etiqueta: this.fb.control(opcion.etiqueta, Validators.required),
          }),
        ),
      ),
      catalogoId: this.fb.control<string | null>(campo?.catalogoId ?? null),
      campoPadreId: this.fb.control<string | null>(campo?.campoPadreId ?? null),
    });
  }

  private renumerar(): void {
    this.secciones.controls.forEach((control, indice) => {
      (control as FormGroup).controls['orden'].setValue(indice + 1);
      const lista = (control as FormGroup).controls['campos'] as FormArray;
      lista.controls.forEach((campo, indiceCampo) => {
        (campo as FormGroup).controls['orden'].setValue(indiceCampo + 1);
      });
    });
  }
}
