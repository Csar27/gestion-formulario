import { Directive, inject, Input, OnDestroy, OnInit, TemplateRef, ViewContainerRef } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

@Directive({
  selector: '[hasPermission]',
  standalone: true,
})
export class HasPermissionDirective implements OnInit, OnDestroy {
  @Input('hasPermission') codigo = '';

  private readonly plantilla = inject(TemplateRef<unknown>);
  private readonly contenedor = inject(ViewContainerRef);
  private readonly auth = inject(AuthService);
  private readonly destruir$ = new Subject<void>();

  ngOnInit(): void {
    this.auth.cambios$.pipe(takeUntil(this.destruir$)).subscribe(() => this.aplicar());
    this.aplicar();
  }

  ngOnDestroy(): void {
    this.destruir$.next();
    this.destruir$.complete();
  }

  private aplicar(): void {
    this.contenedor.clear();
    if (this.auth.tienePermiso(this.codigo)) {
      this.contenedor.createEmbeddedView(this.plantilla);
    }
  }
}
