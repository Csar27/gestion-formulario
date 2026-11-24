import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FormularioDto, ProductoDto } from '../../core/models/api.models';
import { ApiErrorService } from '../../core/services/api-error.service';
import { FormulariosService } from '../../core/services/formularios.service';
import { ProductosService } from '../../core/services/productos.service';

@Component({
  selector: 'app-producto-editor',
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
  ],
  templateUrl: './producto-editor.html',
  styleUrl: './producto-editor.scss',
})
export class ProductoEditorComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(ProductosService);
  private readonly formulariosService = inject(FormulariosService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly errores = inject(ApiErrorService);

  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly subiendo = signal(false);
  readonly editando = signal<ProductoDto | null>(null);
  readonly formularios = signal<FormularioDto[]>([]);

  readonly nombre = signal('');
  readonly descripcion = signal('');
  readonly activo = signal(true);
  readonly formularioId = signal('');

  ngOnInit(): void {
    this.formulariosService.list().subscribe({
      next: (formularios) => this.formularios.set(formularios),
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.cargando.set(false);
      return;
    }

    this.service.get(id).pipe(finalize(() => this.cargando.set(false))).subscribe({
      next: (producto) => {
        this.editando.set(producto);
        this.nombre.set(producto.nombre);
        this.descripcion.set(producto.descripcion ?? '');
        this.activo.set(producto.activo);
        this.formularioId.set(producto.formularioId ?? '');
      },
      error: (error) => {
        this.cargando.set(false);
        this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 });
        this.volver();
      },
    });
  }

  imagen(): string | null {
    const producto = this.editando();
    if (!producto?.imagenUrl) return null;
    return environment.apiUrl.replace('/api', '') + producto.imagenUrl;
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
    const id = this.editando()?.id;
    const operacion = id ? this.service.update(id, request) : this.service.create(request);
    operacion.pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: (producto) => {
        this.snackBar.open('Producto guardado.', 'Cerrar', { duration: 4000 });
        if (!id) {
          this.router.navigate(['/app/configuracion/productos', producto.id, 'editar']);
        } else {
          this.editando.set(producto);
        }
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
    });
  }

  subirImagen(evento: Event): void {
    const id = this.editando()?.id;
    if (!id) return;
    const entrada = evento.target as HTMLInputElement;
    const archivo = entrada.files?.item(0);
    if (!archivo) return;
    this.subiendo.set(true);
    this.service.subirImagen(id, archivo).pipe(finalize(() => this.subiendo.set(false))).subscribe({
      next: (producto) => {
        this.editando.set(producto);
        this.snackBar.open('Imagen actualizada.', 'Cerrar', { duration: 4000 });
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 7000 }),
    });
  }

  volver(): void {
    this.router.navigate(['/app/configuracion/productos']);
  }
}