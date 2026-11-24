import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { PERMISOS } from '../../core/models/api.models';

@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatCardModule, MatIconModule, HasPermissionDirective],
  templateUrl: './configuracion.html',
  styleUrl: './configuracion.scss',
})
export class ConfiguracionComponent {
  readonly PERMISOS = PERMISOS;
}