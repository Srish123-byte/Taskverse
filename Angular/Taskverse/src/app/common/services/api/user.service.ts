import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppConfig } from '../../../app.config';

export interface RegisterRequest {
  fullName: string;
  email: string;
  phone?: string;
  collegeId?: string;
  classId?: string;
  batchId?: string;
  role: string;
  password: string;
}

export interface RegisterResponse {
  userId: string;
  fullName: string;
  email: string;
  phone?: string;
  collegeId?: string;
  classId?: string;
  batchId?: string;
  role: string;
  status: string;
  createdAt: string;
}

export interface RegistrationCollegeOption {
  collegeId: string;
  name: string;
}

export interface RegistrationClassOption {
  classId: string;
  collegeId: string;
  name: string;
  academicYear?: string;
}

export interface RegistrationBatchOption {
  batchId: string;
  classId: string;
  collegeId: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class UserService {

  constructor(
    private readonly http: HttpClient,
    private readonly appConfig: AppConfig
  ) {}

  register(request: RegisterRequest): Observable<RegisterResponse> {
    const url = `${this.appConfig.api_url}/users/register`;
    return this.http.post<RegisterResponse>(url, request, {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  getApprovedRegistrationColleges(): Observable<RegistrationCollegeOption[]> {
    const url = `${this.appConfig.api_url}/users/registration/colleges`;
    return this.http.get<RegistrationCollegeOption[]>(url);
  }

  getRegistrationClasses(collegeId: string): Observable<RegistrationClassOption[]> {
    const url = `${this.appConfig.api_url}/users/registration/colleges/${collegeId}/classes`;
    return this.http.get<RegistrationClassOption[]>(url);
  }

  getRegistrationBatches(classId: string): Observable<RegistrationBatchOption[]> {
    const url = `${this.appConfig.api_url}/users/registration/classes/${classId}/batches`;
    return this.http.get<RegistrationBatchOption[]>(url);
  }
}
