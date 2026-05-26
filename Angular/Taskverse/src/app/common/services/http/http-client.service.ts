import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { HttpHelperService } from './http-helper.service';

@Injectable({
  providedIn: 'root'
})
export class HttpClientService {
  constructor(
    private readonly http: HttpClient,
    private readonly httpHelperService: HttpHelperService
  ) {}

  private formatErrors(error: any): Observable<never> {
    console.error('An error occurred', error);
    return throwError(() => error);
  }

  get<T>(path: string, params: HttpParams = new HttpParams()): Observable<T> {
    return this.http
      .get<T>(this.httpHelperService.api + path, this.httpHelperService.getOptions(params))
      .pipe(map((response: HttpResponse<T>) => response.body as T))
      .pipe(catchError(this.formatErrors));
  }

  post<T>(path: string, body: object = {}, params: HttpParams = new HttpParams()): Observable<T> {
    return this.http
      .post<T>(this.httpHelperService.api + path, JSON.stringify(body), this.httpHelperService.getOptions(params))
      .pipe(map((response: HttpResponse<T>) => response.body as T))
      .pipe(catchError(this.formatErrors));
  }

  put<T>(path: string, body: object = {}): Observable<T> {
    return this.http
      .put<T>(this.httpHelperService.api + path, JSON.stringify(body), this.httpHelperService.getOptions())
      .pipe(map((response: HttpResponse<T>) => response.body as T))
      .pipe(catchError(this.formatErrors));
  }

  delete<T>(path: string, params: HttpParams = new HttpParams()): Observable<T> {
    return this.http
      .delete<T>(this.httpHelperService.api + path, this.httpHelperService.getOptions(params))
      .pipe(map((response: HttpResponse<T>) => response.body as T))
      .pipe(catchError(this.formatErrors));
  }
}
