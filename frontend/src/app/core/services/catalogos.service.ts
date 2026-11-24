import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CatalogoDto,
  CatalogoItemDto,
  CatalogoResumenDto,
  ImportacionResultadoDto,
  OpcionCatalogoDto,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class CatalogosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/catalogos`;

  list(soloActivos = true): Observable<CatalogoResumenDto[]> {
    return this.http.get<CatalogoResumenDto[]>(this.url, {
      params: new HttpParams().set('soloActivos', soloActivos),
    });
  }

  get(id: string): Observable<CatalogoDto> {
    return this.http.get<CatalogoDto>(`${this.url}/${id}`);
  }

  create(request: { nombre: string; descripcion: string | null; activo: boolean; catalogoPadreId: string | null }): Observable<CatalogoDto> {
    return this.http.post<CatalogoDto>(this.url, request);
  }

  update(id: string, request: { nombre: string; descripcion: string | null; activo: boolean; catalogoPadreId: string | null }): Observable<CatalogoDto> {
    return this.http.put<CatalogoDto>(`${this.url}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  createItem(catalogoId: string, request: { codigo: string; texto: string; orden: number; activo: boolean; padreItemId: string | null }): Observable<CatalogoItemDto> {
    return this.http.post<CatalogoItemDto>(`${this.url}/${catalogoId}/items`, request);
  }

  updateItem(itemId: string, request: { codigo: string; texto: string; orden: number; activo: boolean; padreItemId: string | null }): Observable<CatalogoItemDto> {
    return this.http.put<CatalogoItemDto>(`${this.url}/items/${itemId}`, request);
  }

  deleteItem(itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/items/${itemId}`);
  }

  opciones(id: string, padreItemId?: string | null, buscar?: string | null): Observable<OpcionCatalogoDto[]> {
    let params = new HttpParams();
    if (padreItemId) {
      params = params.set('padreItemId', padreItemId);
    }
    if (buscar) {
      params = params.set('buscar', buscar);
    }
    return this.http.get<OpcionCatalogoDto[]>(`${this.url}/${id}/items`, { params });
  }

  importar(id: string, archivo: File, reemplazar: boolean): Observable<ImportacionResultadoDto> {
    const datos = new FormData();
    datos.append('archivo', archivo, archivo.name);
    datos.append('reemplazar', String(reemplazar));
    return this.http.post<ImportacionResultadoDto>(`${this.url}/${id}/importar`, datos);
  }
}
