import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FormularioDto, ProductoDto } from '../../core/models/api.models';
import { ApiErrorService } from '../../core/services/api-error.service';
import { DialogService } from '../../core/services/dialog.service';
import { FormulariosService } from '../../core/services/formularios.service';
import { ProductosService } from '../../core/services/productos.service';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-productos',
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
    MatSelectModule,
    MatTableModule,
    HasPermissionDirective,
  ],
  templateUrl: './productos.html',
  styleUrl: './productos.scss',
})
export class ProductosComponent implements OnInit {
  private readonly service = inject(ProductosService);
  private readonly router = inject(Router);
  private readonly formulariosService = inject(FormulariosService);
  private readonly dialog = inject(DialogService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly errores = inject(ApiErrorService);

  readonly cargando = signal(true);
  readonly productos = signal<ProductoDto[]>([]);
  readonly formularios = signal<FormularioDto[]>([]);

  readonly editando = signal<ProductoDto | null>(null);
  readonly nuevo = signal(false);
  readonly nombre = signal('');
  readonly descripcion = signal('');
  readonly activo = signal(true);
  readonly formularioId = signal('');
  readonly guardando = signal(false);
  readonly subiendoImagen = signal<string | null>(null);

  readonly columnas = ['nombre', 'formulario', 'estado', 'acciones'];

  ngOnInit(): void {
    this.cargar();
    this.formulariosService.list().subscribe({
      next: (formularios) => this.formularios.set(formularios),
    });
  }

  cargar(): void {
    this.cargando.set(true);
    this.service.list().pipe(finalize(() => this.cargando.set(false))).subscribe({
      next: (productos) => this.productos.set(productos),
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
    });
  }

  abrirNuevo(): void {
    this.router.navigate(['/app/configuracion/productos/nuevo']);
  }

  editar(producto: ProductoDto): void {
    this.router.navigate(['/app/configuracion/productos', producto.id, 'editar']);
  }

  guardar(): void {
    const nombre = this.nombre().trim();
    if (!nombre) {
      this.snackBar.open('El nombre del producto es obligatorio.', 'Cerrar', { duration: 4000 });
      return;
    }
    this.guardando.set(true);
    const request = {
      nombre,
      descripcion: this.descripcion().trim() || null,
      activo: this.activo(),
      formularioId: this.formularioId() || null,
    };
    const operacion = this.editando()
      ? this.service.update(this.editando()!.id, request)
      : this.service.create(request);
    operacion.pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: () => {
        this.snackBar.open('Producto guardado.', 'Cerrar', { duration: 4000 });
        this.nuevo.set(false);
        this.editando.set(null);
        this.cargar();
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
    });
  }

  eliminar(producto: ProductoDto): void {
    this.dialog.confirmar({
      titulo: 'Eliminar producto',
      mensaje: `¿Eliminar el producto ${producto.nombre}?`,
      textoConfirmar: 'Eliminar',
    }).subscribe((confirmado) => {
      if (!confirmado) return;
      this.service.delete(producto.id).subscribe({
        next: () => {
          this.snackBar.open('Producto eliminado.', 'Cerrar', { duration: 4000 });
          this.cargar();
        },
        error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
      });
    });
  }

  subirImagen(producto: ProductoDto, evento: Event): void {
    const entrada = evento.target as HTMLInputElement;
    const archivo = entrada.files?.item(0);
    if (!archivo) return;
    this.subiendoImagen.set(producto.id);
    this.service.subirImagen(producto.id, archivo).pipe(finalize(() => this.subiendoImagen.set(null))).subscribe({
      next: () => {
        this.snackBar.open('Imagen actualizada.', 'Cerrar', { duration: 4000 });
        this.cargar();
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
    });
  }

  imagen(producto: ProductoDto): string | null {
    if (!producto.imagenUrl) return null;
    return environment.apiUrl.replace('/api', '') + producto.imagenUrl;
  }
}