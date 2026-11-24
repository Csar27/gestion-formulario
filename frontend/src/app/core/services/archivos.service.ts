import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ArchivosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/archivos`;

  subir(archivo: File): Observable<{ url: string }> {
    const datos = new FormData();
    datos.append('archivo', archivo, archivo.name);
    return this.http.post<{ url: string }>(this.url, datos);
  }
}