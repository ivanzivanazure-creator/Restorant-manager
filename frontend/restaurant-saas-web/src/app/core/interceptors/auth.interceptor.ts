import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { TokenStorageService } from '../auth/token-storage.service';
import { refreshState } from './refresh-state';

/** Attaches the current access token to every same-origin API request. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(TokenStorageService);
  const accessToken = tokens.accessToken;

  if (!accessToken || !req.url.startsWith(environment.apiBaseUrl)) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }));
};

/** On a 401 from the API (access token expired), attempts exactly one silent refresh-and-retry per
 * request, sharing a single in-flight refresh call across any requests that 401 concurrently. */
export const refreshTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const tokens = inject(TokenStorageService);

  return next(req).pipe(
    catchError((error: unknown) => {
      const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/refresh') || req.url.includes('/auth/register');
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || isAuthEndpoint || !tokens.refreshToken) {
        return throwError(() => error);
      }

      return triggerOrAwaitRefresh(auth).pipe(
        switchMap((accessToken) => next(req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }))),
        catchError((refreshError: unknown) => {
          auth.logout();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};

function triggerOrAwaitRefresh(auth: AuthService): Observable<string> {
  if (!refreshState.inProgress) {
    refreshState.inProgress = true;
    refreshState.accessToken$.next(null);
    auth.refresh().subscribe({
      next: (tokens) => {
        refreshState.inProgress = false;
        refreshState.accessToken$.next(tokens.accessToken);
      },
      error: () => {
        refreshState.inProgress = false;
        refreshState.accessToken$.next(null);
      },
    });
  }

  return refreshState.accessToken$.pipe(
    filter((token): token is string => token !== null),
    take(1),
  );
}
