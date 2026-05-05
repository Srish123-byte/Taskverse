import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClientService } from '../http/http-client.service';

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
  private readonly url = 'users';

  constructor(private readonly http: HttpClientService) {}

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.url}/register`, request);
  }
}
