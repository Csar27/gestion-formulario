import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardResumenDto, PagedResult, ReporteRespuestaDto } from '../models/api.models';

export interface ConsultaReporte {
  formularioId?: string;
  estado?: string;
  desde?: string;
  hasta?: string;
  usuarioId?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class ReportesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/reportes`;

  dashboard(consulta: ConsultaReporte = {}): Observable<DashboardResumenDto> {
    let params = new HttpParams();
    if (consulta.formularioId) params = params.set('formularioId', consulta.formularioId);
    if (consulta.estado) params = params.set('estado', consulta.estado);
    if (consulta.desde) params = params.set('desde', consulta.desde);
    if (consulta.hasta) params = params.set('hasta', consulta.hasta);
    if (consulta.usuarioId) params = params.set('usuarioId', consulta.usuarioId);
    return this.http.get<DashboardResumenDto>(`${this.url}/dashboard`, { params });
  }

  list(consulta: ConsultaReporte = {}): Observable<PagedResult<ReporteRespuestaDto>> {
    let params = new HttpParams();
    if (consulta.formularioId) params = params.set('formularioId', consulta.formularioId);
    if (consulta.estado) params = params.set('estado', consulta.estado);
    if (consulta.desde) params = params.set('desde', consulta.desde);
    if (consulta.hasta) params = params.set('hasta', consulta.hasta);
    if (consulta.usuarioId) params = params.set('usuarioId', consulta.usuarioId);
    params = params.set('page', consulta.page ?? 1);
    params = params.set('pageSize', consulta.pageSize ?? 20);
    return this.http.get<PagedResult<ReporteRespuestaDto>>(this.url, { params });
  }

  exportar(formato: 'excel' | 'pdf', consulta: ConsultaReporte = {}): Observable<HttpResponse<Blob>> {
    let params = new HttpParams();
    if (consulta.formularioId) params = params.set('formularioId', consulta.formularioId);
    if (consulta.estado) params = params.set('estado', consulta.estado);
    if (consulta.desde) params = params.set('desde', consulta.desde);
    if (consulta.hasta) params = params.set('hasta', consulta.hasta);
    if (consulta.usuarioId) params = params.set('usuarioId', consulta.usuarioId);
    return this.http.get(`${this.url}/${formato}`, {
      params,
      responseType: 'blob',
      observe: 'response',
    });
  }
}