import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AsignarPermisosRequest,
  PermisoDto,
  RolCreateRequest,
  RolDto,
  RolUpdateRequest,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/roles`;

  list(): Observable<RolDto[]> {
    return this.http.get<RolDto[]>(this.url);
  }

  get(id: string): Observable<RolDto> {
    return this.http.get<RolDto>(`${this.url}/${id}`);
  }

  create(request: RolCreateRequest): Observable<RolDto> {
    return this.http.post<RolDto>(this.url, request);
  }

  update(id: string, request: RolUpdateRequest): Observable<RolDto> {
    return this.http.put<RolDto>(`${this.url}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  permisosDisponibles(): Observable<PermisoDto[]> {
    return this.http.get<PermisoDto[]>(`${this.url}/permisos-disponibles`);
  }

  getPermisos(id: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.url}/${id}/permisos`);
  }

  setPermisos(id: string, request: AsignarPermisosRequest): Observable<{ rolId: string; permisos: string[] }> {
    return this.http.put<{ rolId: string; permisos: string[] }>(`${this.url}/${id}/permisos`, request);
  }
}