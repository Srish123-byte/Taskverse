import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { HttpHelperService } from './http-helper.service';

@Injectable({
  providedIn: 'root'
})
export class HttpClientService {
  private static readonly skipGlobalErrorRedirectHeader = 'X-Skip-Global-Error-Redirect';

  constructor(
    private readonly http: HttpClient,
    private readonly httpHelperService: HttpHelperService
  ) {}

  private formatErrors(error: any): Observable<never> {
    console.error('An error occurred', error);
    return throwError(() => error);
  }

  get<T>(path: string, params: HttpParams = new HttpParams(), skipGlobalErrorRedirect = false): Observable<T> {
    const options = this.buildOptions(params, skipGlobalErrorRedirect);

    return this.http
      .get<T>(this.httpHelperService.api + path, options)
      .pipe(map((response: HttpResponse<T>) => response.body as T))
      .pipe(catchError(this.formatErrors));
  }

  post<T>(
    path: string,
    body: object = {},
    params: HttpParams = new HttpParams(),
    skipGlobalErrorRedirect = false
  ): Observable<T> {
    const options = this.buildOptions(params, skipGlobalErrorRedirect);

    return this.http
      .post<T>(this.httpHelperService.api + path, JSON.stringify(body), options)
      .pipe(map((response: HttpResponse<T>) => response.body as T))
      .pipe(catchError(this.formatErrors));
  }

  put<T>(path: string, body: object = {}, skipGlobalErrorRedirect = false): Observable<T> {
    const options = this.buildOptions(new HttpParams(), skipGlobalErrorRedirect);

    return this.http
      .put<T>(this.httpHelperService.api + path, JSON.stringify(body), options)
      .pipe(map((response: HttpResponse<T>) => response.body as T))
      .pipe(catchError(this.formatErrors));
  }

  delete<T>(
    path: string,
    params: HttpParams = new HttpParams(),
    body?: object,
    skipGlobalErrorRedirect = false
  ): Observable<T> {
    const options = this.buildOptions(params, skipGlobalErrorRedirect);

    return this.http
      .delete<T>(this.httpHelperService.api + path, {
        ...options,
        body: body ? JSON.stringify(body) : undefined
      })
      .pipe(map((response: HttpResponse<T>) => response.body as T))
      .pipe(catchError(this.formatErrors));
  }

  private buildOptions(params: HttpParams, skipGlobalErrorRedirect: boolean) {
    const options = this.httpHelperService.getOptions(params);

    if (!skipGlobalErrorRedirect) {
      return options;
    }

    const headers = options.headers instanceof HttpHeaders
      ? options.headers
      : new HttpHeaders(options.headers);

    return {
      ...options,
      headers: headers.set(HttpClientService.skipGlobalErrorRedirectHeader, 'true')
    };
  }
}
