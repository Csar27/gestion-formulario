import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { ProductoDto } from '../../core/models/api.models';
import { ProductosService } from '../../core/services/productos.service';

@Component({
  selector: 'app-mis-productos',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './mis-productos.html',
  styleUrl: './mis-productos.scss',
})
export class MisProductosComponent implements OnInit {
  private readonly service = inject(ProductosService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  readonly cargando = signal(true);
  readonly productos = signal<ProductoDto[]>([]);

  ngOnInit(): void {
    this.service.list().pipe(finalize(() => this.cargando.set(false))).subscribe({
      next: (productos) => this.productos.set(productos.filter((p) => p.activo && !!p.formularioId)),
      error: () => this.productos.set([]),
    });
  }

  imagen(producto: ProductoDto): string | null {
    if (!producto.imagenUrl) return null;
    return environment.apiUrl.replace('/api', '') + producto.imagenUrl;
  }

  diligenciar(producto: ProductoDto): void {
    if (!producto.formularioId) {
      this.snackBar.open('Este producto no tiene un formulario asociado.', 'Cerrar', { duration: 5000 });
      return;
    }
    this.router.navigate(['/app/mis-formularios', producto.formularioId, 'llenar']);
  }
}