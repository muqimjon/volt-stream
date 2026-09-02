import { HttpClient, HttpContext, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../auth/auth';
import { FilterRequest, Paged, Paging } from '../models';
import { tzOffsetHours } from '../utils/date';
import { SILENT } from './api-error';

type Params = Record<string, string | number | boolean | null | undefined>;

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  get<T>(path: string, params?: Params, silent = false): Promise<T> {
    return firstValueFrom(
      this.http.get<T>(url(path), { params: toParams(params), context: ctx(silent) }),
    );
  }

  post<T>(path: string, body: unknown, silent = false): Promise<T> {
    return firstValueFrom(this.http.post<T>(url(path), body, { context: ctx(silent) }));
  }

  async filter<T>(
    path: string,
    request: Partial<FilterRequest>,
    options: { silent?: boolean; branchScoped?: boolean } = {},
  ): Promise<Paged<T>> {
    const branchId = this.auth.branchId();
    const filters = { ...(request.filters ?? {}) };
    if (options.branchScoped && branchId !== null) filters['BranchId'] = [`=${branchId}`];

    const body: FilterRequest = {
      page: request.page ?? 1,
      pageSize: request.pageSize ?? 20,
      search: request.search ?? null,
      sortBy: request.sortBy ?? null,
      descending: request.descending ?? true,
      timeZone: request.timeZone ?? tzOffsetHours(),
      filters,
    };

    const response = await firstValueFrom(
      this.http.post<T[]>(url(path), body, {
        observe: 'response',
        context: ctx(options.silent ?? false),
      }),
    );

    return { items: response.body ?? [], paging: readPaging(response.headers) };
  }
}

function url(path: string): string {
  return `/api/${path.replace(/^\/+/, '')}`;
}

function ctx(silent: boolean): HttpContext {
  return new HttpContext().set(SILENT, silent);
}

function toParams(params?: Params): HttpParams {
  let p = new HttpParams();
  for (const [k, v] of Object.entries(params ?? {}))
    if (v !== null && v !== undefined) p = p.set(k, String(v));
  return p;
}

function readPaging(headers: HttpHeaders): Paging | null {
  const raw = headers.get('X-Paging');
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Record<string, unknown>;
    const lower: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(parsed)) lower[k.toLowerCase()] = v;
    return {
      totalCount: Number(lower['totalcount'] ?? 0),
      page: Number(lower['page'] ?? 1),
      pageSize: Number(lower['pagesize'] ?? 0),
      totalPages: Number(lower['totalpages'] ?? 0),
    };
  } catch {
    return null;
  }
}
