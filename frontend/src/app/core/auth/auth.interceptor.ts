import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/** Rotas que não levam token: abrem ou renovam a sessão. */
const ANONYMOUS = ['/api/v1/auth/login', '/api/v1/auth/refresh'];

/**
 * Põe o token de acesso nas chamadas à API. Num 401, renova a sessão uma vez e repete a chamada;
 * se o refresh também falhar, a sessão acabou e o usuário volta ao login.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.includes('/api/') || ANONYMOUS.some((path) => request.url.includes(path))) {
    return next(request);
  }

  const auth = inject(AuthService);
  const token = auth.accessToken();
  if (!token) {
    return next(request);
  }

  return next(withToken(request, token)).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }
      return from(auth.refresh()).pipe(
        switchMap((renewed) => {
          const fresh = auth.accessToken();
          if (!renewed || !fresh) {
            void auth.expire();
            return throwError(() => error);
          }
          return next(withToken(request, fresh));
        }),
      );
    }),
  );
};

function withToken(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
