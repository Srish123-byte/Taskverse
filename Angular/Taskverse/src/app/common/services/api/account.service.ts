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

export interface LoginResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
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
        headers: { 'Content-Type': 'application/json' }
      }
    );
  }

  getUserProfile(): Observable<User> {
    return this.http.get<User>(`${this.url}/profile`);
  }

  logout(request: LogoutRequest): Observable<void> {
    return this.http.post<void>(`${this.url}/logout`, request);
  }
}
