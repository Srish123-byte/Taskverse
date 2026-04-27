import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClientService } from '../http/http-client.service';
import { User } from '../../models/user.model';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: User;
}

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly url = 'accounts';

  constructor(private readonly http: HttpClientService) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.url}/login`, request);
  }

  getUserProfile(): Observable<User> {
    return this.http.get<User>(`${this.url}/profile`);
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.url}/logout`, {});
  }
}
