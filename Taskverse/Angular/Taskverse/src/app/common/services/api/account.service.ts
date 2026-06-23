import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpClientService } from '../http/http-client.service';
import { User } from '../../models/user.model';
import { AppConfig } from '../../../app.config';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LogoutRequest {
  userId: string;
  refreshToken: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
  accessToken?: string;
  forceRotate?: boolean;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface RefreshTokenResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly url = 'auth';

  constructor(
    private readonly http: HttpClientService,
    private readonly rawHttp: HttpClient,
    private readonly appConfig: AppConfig
  ) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.rawHttp.post<LoginResponse>(
      `${this.appConfig.api_url}/${this.url}/login`,
      request,
      {
        headers: {
          'Content-Type': 'application/json',
          'X-Skip-Global-Error-Redirect': 'true'
        }
      }
    );
  }

  getUserProfile(): Observable<User> {
    return this.http.get<User>(`${this.url}/profile`);
  }

  refreshToken(request: RefreshTokenRequest): Observable<RefreshTokenResponse> {
    return this.rawHttp.post<RefreshTokenResponse>(
      `${this.appConfig.api_url}/${this.url}/refresh`,
      request,
      {
        headers: {
          'Content-Type': 'application/json',
          'X-Skip-Global-Error-Redirect': 'true',
          'X-Skip-Auth-Refresh': 'true'
        }
      }
    );
  }

  logout(request: LogoutRequest): Observable<void> {
    return this.http.post<void>(`${this.url}/logout`, request);
  }
}
