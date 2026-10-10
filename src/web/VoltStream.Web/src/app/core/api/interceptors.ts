import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, throwError } from 'rxjs';
import { AuthService } from '../auth/auth';
import { TranslateService } from '../i18n/translate';
import { ToastService } from '../ui/toast';
import { ApiError, SILENT } from './api-error';

const isApi = (url: string) => url.startsWith('/api');

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApi(req.url)) return next(req);

  const auth = inject(AuthService);
  const headers: Record<string, string> = { 'X-Client': 'Web' };

  const token = auth.token();
  if (token && !req.url.endsWith('/user/login')) headers['Authorization'] = `Bearer ${token}`;

  const branchId = auth.branchId();
  if (branchId !== null) headers['X-Branch-Id'] = String(branchId);

  return next(req.clone({ setHeaders: headers }));
};

export const unwrapInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApi(req.url)) return next(req);

  return next(req).pipe(
    map((event) => {
      if (!(event instanceof HttpResponse)) return event;
      const body = event.body as { statusCode?: number; data?: unknown } | null;
      if (body && typeof body === 'object' && 'statusCode' in body && 'data' in body)
        return event.clone({ body: body.data });
      return event;
    }),
  );
};

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApi(req.url)) return next(req);

  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);
  const translate = inject(TranslateService);
  const isLogin = req.url.endsWith('/user/login');
  const silent = req.context.get(SILENT) || isLogin;

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = err.error as { message?: string } | null;
      const serverMessage = typeof body?.message === 'string' ? body.message : '';
      let message: string;

      if (err.status === 0) {
        message = translate.t('Web.NetworkError');
        if (!silent) toast.error(message);
      } else if (err.status === 401 && !isLogin) {
        message = translate.t('Web.SessionExpired');
        if (auth.token()) {
          auth.logout();
          toast.error(message);
          void router.navigate(['/login'], { replaceUrl: true });
        }
      } else {
        if (err.status === 403) message = serverMessage || translate.t('Web.NoAccess');
        else if (err.status >= 500) message = translate.t('Web.ServerError');
        else message = serverMessage || translate.t('Web.RequestFailed');
        if (!silent) toast.error(message);
      }

      return throwError(() => new ApiError(err.status, message, err.error));
    }),
  );
};
