import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-cambiar-password',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule],
  templateUrl: './cambiar-password.html',
  styleUrl: './cambiar-password.scss',
})
export class CambiarPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly errores = inject(ApiErrorService);

  readonly form = this.fb.group({
    passwordActual: ['', Validators.required],
    passwordNueva: ['', [Validators.required, Validators.minLength(8)]],
  });

  enviar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { passwordActual, passwordNueva } = this.form.getRawValue();
    this.auth.cambiarPassword(passwordActual!, passwordNueva!).subscribe({
      next: () => {
        this.snackBar.open('Contraseña actualizada.', 'Cerrar', { duration: 4000 });
        this.router.navigate(['/app/productos']);
      },
      error: (error) => this.snackBar.open(this.errores.mensaje(error), 'Cerrar', { duration: 6000 }),
    });
  }

  omitir(): void {
    this.router.navigate(['/app/productos']);
  }
}
