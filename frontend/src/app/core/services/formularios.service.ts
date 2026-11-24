import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FormularioDto, FormularioRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class FormulariosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/formularios`;

  list(): Observable<FormularioDto[]> {
    return this.http.get<FormularioDto[]>(this.url);
  }

  get(id: string): Observable<FormularioDto> {
    return this.http.get<FormularioDto>(`${this.url}/${id}`);
  }

  create(request: FormularioRequest): Observable<FormularioDto> {
    return this.http.post<FormularioDto>(this.url, request);
  }

  update(id: string, request: FormularioRequest): Observable<FormularioDto> {
    return this.http.put<FormularioDto>(`${this.url}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
