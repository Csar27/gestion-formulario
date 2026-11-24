import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();
  const url = request.url;
  const esRefresh = url.includes('/auth/refresh');
  const conToken =
    token && !esRefresh
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;

  return next(conToken).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || esRefresh || url.includes('/auth/login')) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap((respuesta) =>
          next(
            conToken.clone({
              setHeaders: { Authorization: `Bearer ${respuesta.accessToken}` },
            }),
          ),
        ),
        catchError((refreshError) => {
          auth.limpiar();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
