import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
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
import {
  CatalogoDialogComponent,
  CatalogoDialogResult,
} from './catalogo-dialog';
import {
  CatalogoItemDialogComponent,
  CatalogoItemDialogResult,
} from './catalogo-item-dialog';

@Component({
  selector: 'app-catalogos-list',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatIconModule,
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
  private readonly matDialog = inject(MatDialog);

  readonly columnas = ['nombre', 'items', 'acciones'];
  readonly catalogos = signal<CatalogoResumenDto[]>([]);
  readonly seleccionado = signal<CatalogoDto | null>(null);
  readonly archivo = signal<File | null>(null);
  readonly reemplazar = signal(false);
  readonly cargando = signal(true);
  readonly importando = signal(false);

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

  abrirCatalogo(catalogo?: CatalogoResumenDto): void {
    this.matDialog
      .open<CatalogoDialogComponent, { catalogo?: CatalogoResumenDto }, CatalogoDialogResult>(
        CatalogoDialogComponent,
        { data: { catalogo }, width: '30rem', maxWidth: 'calc(100vw - 2rem)' },
      )
      .afterClosed()
      .subscribe((resultado) => {
        if (!resultado) return;
        const operacion = catalogo
          ? this.service.update(catalogo.id, resultado)
          : this.service.create(resultado);
        operacion.subscribe({
          next: (detalle) => {
            this.snackBar.open('Catálogo guardado.', 'Cerrar', { duration: 4000 });
            this.cargar();
            this.seleccionado.set(detalle);
          },
          error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
        });
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

  abrirItem(item?: CatalogoItemDto): void {
    const catalogo = this.seleccionado();
    if (!catalogo) return;
    this.matDialog
      .open<CatalogoItemDialogComponent, { item?: CatalogoItemDto }, CatalogoItemDialogResult>(
        CatalogoItemDialogComponent,
        { data: { item }, width: '30rem', maxWidth: 'calc(100vw - 2rem)' },
      )
      .afterClosed()
      .subscribe((resultado) => {
        if (!resultado) return;
        const operacion = item
          ? this.service.updateItem(item.id, resultado)
          : this.service.createItem(catalogo.id, resultado);
        operacion.subscribe({
          next: () => {
            this.snackBar.open('Ítem guardado.', 'Cerrar', { duration: 4000 });
            this.ver(catalogo);
          },
          error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
        });
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

  descargarPlantilla(): void {
    const contenido = '\uFEFFCodigo,Texto,Orden,PadreItemCodigo\r\n';
    const url = URL.createObjectURL(new Blob([contenido], { type: 'text/csv;charset=utf-8' }));
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = 'plantilla-catalogo.csv';
    enlace.click();
    URL.revokeObjectURL(url);
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