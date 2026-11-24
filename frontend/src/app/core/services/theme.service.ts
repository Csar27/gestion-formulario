import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly clave = 'gestion-formulario-tema';
  readonly oscuro = signal(false);

  constructor() {
    const guardado = localStorage.getItem(this.clave);
    const preferenciaOscura = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const esOscuro = guardado ? guardado === 'dark' : preferenciaOscura;
    this.oscuro.set(esOscuro);
    this.aplicar(esOscuro);
  }

  alternar(): void {
    const nuevo = !this.oscuro();
    this.oscuro.set(nuevo);
    localStorage.setItem(this.clave, nuevo ? 'dark' : 'light');
    this.aplicar(nuevo);
  }

  private aplicar(oscuro: boolean): void {
    document.documentElement.classList.toggle('dark', oscuro);
  }
}