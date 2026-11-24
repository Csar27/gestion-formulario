import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ProblemDetails } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ApiErrorService {
  mensaje(error: unknown, respuestaPorDefecto = 'No se pudo completar la operación.'): string {
    const http = error as HttpErrorResponse;
    const cuerpo = http?.error as ProblemDetails | null;

    if (cuerpo?.detail) {
      return cuerpo.detail;
    }
    if (cuerpo?.title) {
      return cuerpo.title;
    }
    if (cuerpo?.errors) {
      const primerCampo = Object.keys(cuerpo.errors)[0];
      if (primerCampo) {
        return `${primerCampo}: ${cuerpo.errors[primerCampo].join(', ')}`;
      }
    }
    if (typeof http?.message === 'string' && http.message) {
      return http.message;
    }
    return respuestaPorDefecto;
  }

  codigo(error: unknown): string | null {
    const http = error as HttpErrorResponse;
    const cuerpo = http?.error as ProblemDetails | null;
    return cuerpo?.code ?? null;
  }
}
