import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppConfig } from '../../../app.config';

export interface RegisterRequest {
  fullName: string;
  email: string;
  phone?: string;
  collegeId?: string;
  role: string;
  password: string;
}

export interface RegisterResponse {
  userId: string;
  fullName: string;
  email: string;
  phone?: string;
  collegeId?: string;
  role: string;
  status: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class UserService {

  constructor(
    private readonly http: HttpClient,
    private readonly appConfig: AppConfig
  ) {}

  /**
   * Public self-registration — uses HttpClient directly (observe:'body')
   * instead of the authenticated HttpClientService, so the response body
   * is returned without any JWT or response-wrapping concerns.
   */
  register(request: RegisterRequest): Observable<RegisterResponse> {
    const url = `${this.appConfig.api_url}/users/register`;
    return this.http.post<RegisterResponse>(url, request, {
      headers: { 'Content-Type': 'application/json' }
    });
  }
}
