import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  GuardarRespuestaRequest,
  PagedResult,
  RespuestaDto,
  RespuestaListItemDto,
  RespuestaValorHistorialDto,
} from '../models/api.models';

export interface ConsultaRespuestas {
  formularioId?: string;
  estado?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class RespuestasService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/respuestas`;

  guardar(request: GuardarRespuestaRequest): Observable<RespuestaDto> {
    return this.http.post<RespuestaDto>(this.url, request);
  }

  list(consulta: ConsultaRespuestas = {}): Observable<PagedResult<RespuestaListItemDto>> {
    let params = new HttpParams();
    if (consulta.formularioId) {
      params = params.set('formularioId', consulta.formularioId);
    }
    if (consulta.estado) {
      params = params.set('estado', consulta.estado);
    }
    params = params.set('page', consulta.page ?? 1);
    params = params.set('pageSize', consulta.pageSize ?? 20);
    return this.http.get<PagedResult<RespuestaListItemDto>>(this.url, { params });
  }

  get(id: string): Observable<RespuestaDto> {
    return this.http.get<RespuestaDto>(`${this.url}/${id}`);
  }

  historial(id: string): Observable<RespuestaValorHistorialDto[]> {
    return this.http.get<RespuestaValorHistorialDto[]>(`${this.url}/${id}/historial`);
  }
}
