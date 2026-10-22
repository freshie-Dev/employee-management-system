
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { API_CONFIG } from '../config/api.config';
import { ApiQuery } from '../models/api.models';


@Injectable({
  providedIn: 'root'
})
export class ApiClient {

  private readonly http = inject(HttpClient);
  private readonly config = inject(API_CONFIG);

  private url(path: string): string {
    return `${this.config.baseUrl.replace(/\/+$/, '')}/${path.replace(/^\/+/, '')}`;
  }

  private params(query?: ApiQuery): HttpParams {
    let params = new HttpParams();

    if (!query) return params;

    for (const [key, value] of Object.entries(query)) {
      if (value !== null && value !== undefined) {
        params = params.set(key, String(value));
      }
    }

    return params;
  }

  get<T>(path: string, query?: ApiQuery) {
    return this.http.get<T>(this.url(path), {
      params: this.params(query)
    });
  }

  post<TResponse, TBody>(
    path: string,
    body: TBody
  ) {
    return this.http.post<TResponse>(
      this.url(path),
      body
    );
  }

  put<TResponse, TBody>(
    path: string,
    body: TBody
  ) {
    return this.http.put<TResponse>(
      this.url(path),
      body
    );
  }

  patch<TResponse, TBody>(
    path: string,
    body: TBody
  ) {
    return this.http.patch<TResponse>(
      this.url(path),
      body
    );
  }

  delete<T>(path: string) {
    return this.http.delete<T>(this.url(path));
  }

  resourceUrl(path: string): string {
    return this.url(path);
  }
}
