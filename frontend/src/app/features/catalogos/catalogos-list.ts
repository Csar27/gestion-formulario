import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import {
  CatalogoDto,
  CatalogoItemDto,
  CatalogoResumenDto,
} from '../../core/models/api.models';
import { ApiErrorService } from '../../core/services/api-error.service';
import { CatalogosService } from '../../core/services/catalogos.service';
import { DialogService } from '../../core/services/dialog.service';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-catalogos-list',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
    HasPermissionDirective,
  ],
  templateUrl: './catalogos-list.html',
  styleUrl: './catalogos-list.scss',
})
export class CatalogosListComponent implements OnInit {
  private readonly service = inject(CatalogosService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly errores = inject(ApiErrorService);
  private readonly dialog = inject(DialogService);

  readonly columnas = ['nombre', 'items', 'acciones'];
  readonly catalogos = signal<CatalogoResumenDto[]>([]);
  readonly seleccionado = signal<CatalogoDto | null>(null);
  readonly archivo = signal<File | null>(null);
  readonly reemplazar = signal(false);
  readonly cargando = signal(true);
  readonly importando = signal(false);

  readonly editandoCatalogo = signal<CatalogoResumenDto | null>(null);
  readonly nuevoCatalogo = signal(false);
  readonly nombreCatalogo = signal('');
  readonly descripcionCatalogo = signal('');
  readonly activoCatalogo = signal(true);
  readonly padreCatalogoId = signal('');
  readonly guardandoCatalogo = signal(false);

  readonly editandoItem = signal<CatalogoItemDto | null>(null);
  readonly nuevoItem = signal(false);
  readonly codigoItem = signal('');
  readonly textoItem = signal('');
  readonly ordenItem = signal(0);
  readonly activoItem = signal(true);
  readonly padreItemId = signal('');
  readonly guardandoItem = signal(false);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.service
      .list()
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (datos) => this.catalogos.set(datos),
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
      });
  }

  ver(catalogo: CatalogoResumenDto): void {
    this.service.get(catalogo.id).subscribe({
      next: (detalle) => this.seleccionado.set(detalle),
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
    });
  }

  abrirNuevoCatalogo(): void {
    this.editandoCatalogo.set(null);
    this.nuevoCatalogo.set(true);
    this.nombreCatalogo.set('');
    this.descripcionCatalogo.set('');
    this.activoCatalogo.set(true);
    this.padreCatalogoId.set('');
  }

  editarCatalogo(catalogo: CatalogoResumenDto): void {
    this.editandoCatalogo.set(catalogo);
    this.nuevoCatalogo.set(true);
    this.nombreCatalogo.set(catalogo.nombre);
    this.descripcionCatalogo.set(catalogo.descripcion ?? '');
    this.activoCatalogo.set(catalogo.activo);
    this.padreCatalogoId.set(catalogo.catalogoPadreId ?? '');
  }

  guardarCatalogo(): void {
    const nombre = this.nombreCatalogo().trim();
    if (!nombre) {
      this.snackBar.open('El nombre del catálogo es obligatorio.', 'Cerrar', { duration: 4000 });
      return;
    }
    const request = {
      nombre,
      descripcion: this.descripcionCatalogo().trim() || null,
      activo: this.activoCatalogo(),
      catalogoPadreId: this.padreCatalogoId() || null,
    };
    this.guardandoCatalogo.set(true);
    const operacion = this.editandoCatalogo()
      ? this.service.update(this.editandoCatalogo()!.id, request)
      : this.service.create(request);
    operacion.pipe(finalize(() => this.guardandoCatalogo.set(false))).subscribe({
      next: (detalle) => {
        this.snackBar.open('Catálogo guardado.', 'Cerrar', { duration: 4000 });
        this.nuevoCatalogo.set(false);
        this.editandoCatalogo.set(null);
        this.cargar();
        this.seleccionado.set(detalle);
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
    });
  }

  eliminarCatalogo(catalogo: CatalogoResumenDto): void {
    this.dialog.confirmar({
      titulo: 'Eliminar catálogo',
      mensaje: `¿Eliminar el catálogo ${catalogo.nombre}?`,
      textoConfirmar: 'Eliminar',
    }).subscribe((confirmado) => {
      if (!confirmado) return;
      this.service.delete(catalogo.id).subscribe({
        next: () => {
          this.snackBar.open('Catálogo eliminado.', 'Cerrar', { duration: 4000 });
          if (this.seleccionado()?.id === catalogo.id) this.seleccionado.set(null);
          this.cargar();
        },
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
      });
    });
  }

  abrirNuevoItem(): void {
    this.editandoItem.set(null);
    this.nuevoItem.set(true);
    this.codigoItem.set('');
    this.textoItem.set('');
    this.ordenItem.set(0);
    this.activoItem.set(true);
    this.padreItemId.set('');
  }

  editarItem(item: CatalogoItemDto): void {
    this.editandoItem.set(item);
    this.nuevoItem.set(true);
    this.codigoItem.set(item.codigo);
    this.textoItem.set(item.texto);
    this.ordenItem.set(item.orden);
    this.activoItem.set(item.activo);
    this.padreItemId.set(item.padreItemId ?? '');
  }

  guardarItem(): void {
    const catalogo = this.seleccionado();
    if (!catalogo) return;
    const codigo = this.codigoItem().trim();
    const texto = this.textoItem().trim();
    if (!codigo || !texto) {
      this.snackBar.open('Código y texto son obligatorios.', 'Cerrar', { duration: 4000 });
      return;
    }
    const request = {
      codigo,
      texto,
      orden: this.ordenItem(),
      activo: this.activoItem(),
      padreItemId: this.padreItemId() || null,
    };
    this.guardandoItem.set(true);
    const operacion = this.editandoItem()
      ? this.service.updateItem(this.editandoItem()!.id, request)
      : this.service.createItem(catalogo.id, request);
    operacion.pipe(finalize(() => this.guardandoItem.set(false))).subscribe({
      next: () => {
        this.snackBar.open('Ítem guardado.', 'Cerrar', { duration: 4000 });
        this.nuevoItem.set(false);
        this.editandoItem.set(null);
        this.ver(catalogo as unknown as CatalogoResumenDto);
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
    });
  }

  eliminarItem(item: CatalogoItemDto): void {
    this.dialog.confirmar({
      titulo: 'Eliminar ítem',
      mensaje: `¿Eliminar el ítem ${item.codigo}?`,
      textoConfirmar: 'Eliminar',
    }).subscribe((confirmado) => {
      if (!confirmado) return;
      this.service.deleteItem(item.id).subscribe({
        next: () => {
          this.snackBar.open('Ítem eliminado.', 'Cerrar', { duration: 4000 });
          const catalogo = this.seleccionado();
          if (catalogo) this.ver(catalogo as unknown as CatalogoResumenDto);
        },
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
      });
    });
  }

  alSeleccionarArchivo(evento: Event): void {
    const entrada = evento.target as HTMLInputElement;
    this.archivo.set(entrada.files?.item(0) ?? null);
  }

  importar(): void {
    const catalogo = this.seleccionado();
    const archivo = this.archivo();
    if (!catalogo || !archivo) {
      this.snackBar.open('Selecciona un catálogo y un archivo.', 'Cerrar', { duration: 4000 });
      return;
    }

    this.importando.set(true);
    this.service
      .importar(catalogo.id, archivo, this.reemplazar())
      .pipe(finalize(() => this.importando.set(false)))
      .subscribe({
        next: (resultado) => {
          this.snackBar.open(
            `Procesadas: ${resultado.procesadas}. Creadas: ${resultado.creadas}. Actualizadas: ${resultado.actualizadas}. Desactivadas: ${resultado.desactivadas}.`,
            'Cerrar',
            { duration: 7000 },
          );
          this.cargar();
          this.ver(catalogo as unknown as CatalogoResumenDto);
          this.archivo.set(null);
        },
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
      });
  }
}