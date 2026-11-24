import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProductoCreateRequest, ProductoDto, ProductoUpdateRequest } from '../models/api.models';

export const API_BASE_URL = environment.apiUrl.replace('/api', '');

@Injectable({ providedIn: 'root' })
export class ProductosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/productos`;

  list(): Observable<ProductoDto[]> {
    return this.http.get<ProductoDto[]>(this.url);
  }

  get(id: string): Observable<ProductoDto> {
    return this.http.get<ProductoDto>(`${this.url}/${id}`);
  }

  create(request: ProductoCreateRequest): Observable<ProductoDto> {
    return this.http.post<ProductoDto>(this.url, request);
  }

  update(id: string, request: ProductoUpdateRequest): Observable<ProductoDto> {
    return this.http.put<ProductoDto>(`${this.url}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  subirImagen(id: string, archivo: File): Observable<ProductoDto> {
    const datos = new FormData();
    datos.append('archivo', archivo, archivo.name);
    return this.http.post<ProductoDto>(`${this.url}/${id}/imagen`, datos);
  }
}