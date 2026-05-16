import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { PendingUser, UserActionRequest } from '../../models/super-admin.model';
import { HttpClientService } from '../http/http-client.service';

export interface ClassConfigurationTotals {
  totalClasses: number;
  totalBatches: number;
  totalStudents: number;
  capacityUtilization: number;
}

export interface CollegeBatchSummary {
  batchId: string;
  classId: string;
  collegeId: string;
  name: string;
  description?: string;
  capacity: number;
  studentCount: number;
  createdAt: string;
}

export interface CollegeClassSummary {
  classId: string;
  collegeId: string;
  name: string;
  academicYear?: string;
  department?: string;
  totalStudents: number;
  totalCapacity: number;
  createdAt: string;
  batches: CollegeBatchSummary[];
}

export interface ClassConfiguration {
  totals: ClassConfigurationTotals;
  classes: CollegeClassSummary[];
}

export interface CreateCollegeClassRequest {
  name: string;
  academicYear?: string;
  department?: string;
}

export interface CreateCollegeBatchRequest {
  name: string;
  description?: string;
  capacity?: number;
}

@Injectable({ providedIn: 'root' })
export class CollegeAdminService {
  private readonly url = 'college-admin';
  private readonly pendingUsersSubject = new BehaviorSubject<PendingUser[]>([]);

  readonly pendingUsers$ = this.pendingUsersSubject.asObservable();

  constructor(private readonly http: HttpClientService) {}

  getClassConfiguration(): Observable<ClassConfiguration> {
    return this.http
      .get<any>(`${this.url}/classes`)
      .pipe(map(configuration => this.mapConfiguration(configuration)));
  }

  createClass(request: CreateCollegeClassRequest): Observable<CollegeClassSummary> {
    return this.http
      .post<any>(`${this.url}/classes`, request)
      .pipe(map(item => this.mapClass(item)));
  }

  createBatch(classId: string, request: CreateCollegeBatchRequest): Observable<CollegeBatchSummary> {
    return this.http
      .post<any>(`${this.url}/classes/${classId}/batches`, request)
      .pipe(map(item => this.mapBatch(item)));
  }

  getPendingUsers(): Observable<PendingUser[]> {
    return this.http.get<PendingUser[]>(`${this.url}/users/pending`).pipe(
      map(users => users ?? []),
      tap(users => this.pendingUsersSubject.next(users))
    );
  }

  approveUser(userId: string, request: UserActionRequest = {}): Observable<void> {
    return this.http.post<void>(`${this.url}/users/${userId}/approve`, request).pipe(
      tap(() => this.removePendingUser(userId))
    );
  }

  rejectUser(userId: string, request: UserActionRequest = {}): Observable<void> {
    return this.http.post<void>(`${this.url}/users/${userId}/reject`, request).pipe(
      tap(() => this.removePendingUser(userId))
    );
  }

  setPendingUsers(users: PendingUser[]): void {
    this.pendingUsersSubject.next(users);
  }

  private mapConfiguration(configuration: any): ClassConfiguration {
    return {
      totals: {
        totalClasses: configuration?.totals?.totalClasses ?? configuration?.Totals?.TotalClasses ?? 0,
        totalBatches: configuration?.totals?.totalBatches ?? configuration?.Totals?.TotalBatches ?? 0,
        totalStudents: configuration?.totals?.totalStudents ?? configuration?.Totals?.TotalStudents ?? 0,
        capacityUtilization: configuration?.totals?.capacityUtilization ?? configuration?.Totals?.CapacityUtilization ?? 0
      },
      classes: (configuration?.classes ?? configuration?.Classes ?? []).map((item: any) => this.mapClass(item))
    };
  }

  private mapClass(item: any): CollegeClassSummary {
    return {
      classId: item?.classId ?? item?.ClassId ?? '',
      collegeId: item?.collegeId ?? item?.CollegeId ?? '',
      name: item?.name ?? item?.Name ?? '',
      academicYear: item?.academicYear ?? item?.AcademicYear ?? undefined,
      department: item?.department ?? item?.Department ?? undefined,
      totalStudents: item?.totalStudents ?? item?.TotalStudents ?? 0,
      totalCapacity: item?.totalCapacity ?? item?.TotalCapacity ?? 0,
      createdAt: item?.createdAt ?? item?.CreatedAt ?? '',
      batches: (item?.batches ?? item?.Batches ?? []).map((batch: any) => this.mapBatch(batch))
    };
  }

  private mapBatch(item: any): CollegeBatchSummary {
    return {
      batchId: item?.batchId ?? item?.BatchId ?? '',
      classId: item?.classId ?? item?.ClassId ?? '',
      collegeId: item?.collegeId ?? item?.CollegeId ?? '',
      name: item?.name ?? item?.Name ?? '',
      description: item?.description ?? item?.Description ?? undefined,
      capacity: item?.capacity ?? item?.Capacity ?? 0,
      studentCount: item?.studentCount ?? item?.StudentCount ?? 0,
      createdAt: item?.createdAt ?? item?.CreatedAt ?? ''
    };
  }

  private removePendingUser(userId: string): void {
    this.pendingUsersSubject.next(this.pendingUsersSubject.value.filter(user => user.userId !== userId));
  }
}
