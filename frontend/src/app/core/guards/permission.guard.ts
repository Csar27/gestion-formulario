import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../services/auth.service';

export const permissionGuard = (codigo: string): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);

  if (auth.tienePermiso(codigo)) {
    return true;
  }

  snackBar.open('No tienes permiso para acceder a este recurso.', 'Cerrar', { duration: 5000 });
  return router.parseUrl('/app/acceso-denegado');
};
