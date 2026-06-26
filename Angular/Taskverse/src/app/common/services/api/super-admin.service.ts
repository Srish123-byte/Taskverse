import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { BulkStudentUploadRequest, BulkStudentUploadResult, College, CollegeActionRequest, CollegeSearchRequest, CollegeSearchResult, PagedUsersResult, PendingUser, SuperAdminDashboard, UserActionRequest, UserSearchRequest } from '../../models/super-admin.model';
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

  searchColleges(request: CollegeSearchRequest): Observable<CollegeSearchResult[]> {
    return this.http
      .post<any[]>(`${this.url}/colleges/search`, request)
      .pipe(map(colleges => (colleges ?? []).map(college => this.mapCollegeSearchResult(college))));
  }

  getPendingUsers(): Observable<PendingUser[]> {
    return this.http.get<PendingUser[]>(`${this.url}/users/pending`);
  }

  searchUsers(request: UserSearchRequest): Observable<PagedUsersResult> {
    return this.http.post<PagedUsersResult>(`${this.url}/users/search`, request);
  }

  approveUser(userId: string, request: UserActionRequest = {}): Observable<void> {
    return this.http.post<void>(`${this.url}/users/${userId}/approve`, request);
  }

  rejectUser(userId: string, request: UserActionRequest = {}): Observable<void> {
    return this.http.post<void>(`${this.url}/users/${userId}/reject`, request);
  }

  bulkUploadStudents(request: BulkStudentUploadRequest): Observable<BulkStudentUploadResult> {
    return this.http.post<BulkStudentUploadResult>(`${this.url}/users/bulk-upload/students`, request);
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

  private mapCollegeSearchResult(college: any): CollegeSearchResult {
    return {
      collegeId: college?.collegeId ?? college?.CollegeId ?? '',
      name: college?.name ?? college?.Name ?? '',
      city: college?.city ?? college?.City,
      state: college?.state ?? college?.State,
      adminName: college?.adminName ?? college?.AdminName,
      adminEmail: college?.adminEmail ?? college?.AdminEmail,
      totalUsers: college?.totalUsers ?? college?.TotalUsers ?? 0,
      status: college?.status ?? college?.Status ?? ''
    };
  }
}
