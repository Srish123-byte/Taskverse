import { HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Session } from '../session/session.service';
import { AppConfig } from '../../../app.config';

export interface IRequestOptions {
  headers?: HttpHeaders | { [header: string]: string | string[] };
  observe: 'response';
  params?: HttpParams | { [param: string]: string | string[] };
  reportProgress?: boolean;
  responseType?: 'json';
  withCredentials?: boolean;
}

@Injectable({ providedIn: 'root' })
export class HttpHelperService {
  public api: string;

  constructor(
    private readonly session: Session,
    private readonly appConfig: AppConfig
  ) {
    this.api = `${this.appConfig.api_url}/`;
  }

  public getOptions(params: HttpParams = new HttpParams(), options?: IRequestOptions): IRequestOptions {
    const headers = this.getHeaders();

    if (!options) {
      options = {
        headers,
        observe: 'response',
        params,
        reportProgress: false,
        responseType: 'json',
        withCredentials: true
      };
    }

    if (!options.headers) {
      options.headers = headers;
    }

    return options;
  }

  private getHeaders(): HttpHeaders {
    let headers = new HttpHeaders({ 'Content-Type': 'application/json' });

    const token = this.session.jwtToken;
    if (token && token !== 'null' && token.trim().length > 0) {
      headers = headers.append('Authorization', 'Bearer ' + token);
    }

    return headers;
  }
}
