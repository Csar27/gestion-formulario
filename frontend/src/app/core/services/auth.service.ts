import { HttpClient } from '@angular/common/http';
import { computed, Injectable, inject, signal } from '@angular/core';
import { Subject, Observable, finalize, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResponse } from '../models/api.models';

interface OperacionResponse {
  exitoso: boolean;
  mensaje: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly clave = 'gestion-formulario-usuario';
  private readonly http = inject(HttpClient);
  private readonly estado = signal<LoginResponse | null>(this.leerStorage());

  readonly usuario = this.estado.asReadonly();
  readonly estaAutenticado = computed(() => Boolean(this.estado()?.accessToken));
  readonly debeCambiarPassword = computed(() => this.estado()?.debeCambiarPassword ?? false);
  readonly permisos = computed(() => this.estado()?.permisos ?? []);
  readonly cambios$ = new Subject<void>();

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(tap((respuesta) => this.guardar(respuesta)));
  }

  refresh(): Observable<LoginResponse> {
    const refreshToken = this.estado()?.refreshToken;
    if (!refreshToken) {
      return throwError(() => new Error('No hay token de refresco.'));
    }
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/refresh`, { refreshToken })
      .pipe(tap((respuesta) => this.guardar(respuesta)));
  }

  logout(): Observable<OperacionResponse> {
    return this.http
      .post<OperacionResponse>(`${environment.apiUrl}/auth/logout`, {})
      .pipe(finalize(() => this.limpiar()));
  }

  cambiarPassword(passwordActual: string, passwordNueva: string): Observable<OperacionResponse> {
    return this.http
      .post<OperacionResponse>(`${environment.apiUrl}/auth/cambiar-password`, {
        passwordActual,
        passwordNueva,
      })
      .pipe(
        tap(() => {
          const actual = this.estado();
          if (actual) {
            this.guardar({ ...actual, debeCambiarPassword: false });
          }
        }),
      );
  }

  accessToken(): string | null {
    return this.estado()?.accessToken ?? null;
  }

  tienePermiso(codigo: string): boolean {
    const usuario = this.estado();
    if (!usuario) {
      return false;
    }
    if (usuario.roles.some((rol) => rol.toLowerCase() === 'admin')) {
      return true;
    }
    return usuario.permisos.includes(codigo);
  }

  limpiar(): void {
    localStorage.removeItem(this.clave);
    this.estado.set(null);
    this.cambios$.next();
  }

  private guardar(respuesta: LoginResponse): void {
    localStorage.setItem(this.clave, JSON.stringify(respuesta));
    this.estado.set(respuesta);
    this.cambios$.next();
  }

  private leerStorage(): LoginResponse | null {
    const valor = localStorage.getItem(this.clave);
    if (!valor) {
      return null;
    }
    try {
      return JSON.parse(valor) as LoginResponse;
    } catch {
      localStorage.removeItem(this.clave);
      return null;
    }
  }
}
