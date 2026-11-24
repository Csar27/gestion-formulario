import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult, UsuarioCreateRequest, UsuarioDto, UsuarioUpdateRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class UsuariosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/usuarios`;

  list(page = 1, pageSize = 20): Observable<PagedResult<UsuarioDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<UsuarioDto>>(this.url, { params });
  }

  get(id: string): Observable<UsuarioDto> {
    return this.http.get<UsuarioDto>(`${this.url}/${id}`);
  }

  create(request: UsuarioCreateRequest): Observable<UsuarioDto> {
    return this.http.post<UsuarioDto>(this.url, request);
  }

  update(id: string, request: UsuarioUpdateRequest): Observable<UsuarioDto> {
    return this.http.put<UsuarioDto>(`${this.url}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}