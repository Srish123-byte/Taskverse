import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { College, CollegeActionRequest, PendingUser, SuperAdminDashboard, UserActionRequest } from '../../models/super-admin.model';
import { HttpClientService } from '../http/http-client.service';

@Injectable({ providedIn: 'root' })
export class SuperAdminService {
  private readonly url = 'super-admin';

  constructor(private readonly http: HttpClientService) {}

  getDashboard(): Observable<SuperAdminDashboard> {
    return this.http.get<SuperAdminDashboard>(`${this.url}/dashboard`);
  }

  getColleges(): Observable<College[]> {
    return this.http.get<College[]>(`${this.url}/colleges`);
  }

  getPendingUsers(): Observable<PendingUser[]> {
    return this.http.get<PendingUser[]>(`${this.url}/users/pending`);
  }

  approveUser(userId: string, request: UserActionRequest = {}): Observable<void> {
    return this.http.post<void>(`${this.url}/users/${userId}/approve`, request);
  }

  rejectUser(userId: string, request: UserActionRequest = {}): Observable<void> {
    return this.http.post<void>(`${this.url}/users/${userId}/reject`, request);
  }

  approveCollege(collegeId: string, request: CollegeActionRequest = {}): Observable<College> {
    return this.http.post<College>(`${this.url}/colleges/${collegeId}/approve`, request);
  }

  rejectCollege(collegeId: string, request: CollegeActionRequest = {}): Observable<College> {
    return this.http.post<College>(`${this.url}/colleges/${collegeId}/reject`, request);
  }

  deactivateCollege(collegeId: string, request: CollegeActionRequest = {}): Observable<College> {
    return this.http.post<College>(`${this.url}/colleges/${collegeId}/deactivate`, request);
  }

  reactivateCollege(collegeId: string, request: CollegeActionRequest = {}): Observable<College> {
    return this.http.post<College>(`${this.url}/colleges/${collegeId}/reactivate`, request);
  }
}
