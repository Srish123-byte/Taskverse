import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpHelperService } from '../http/http-helper.service';
import { HttpClientService } from '../http/http-client.service';

export interface ReportContextTotals {
  totalClasses: number;
  totalBatches: number;
  totalStudents: number;
  averagePercentage: number;
  passRate: number;
}

export interface ReportStudent {
  studentId: string;
  userId: string;
  collegeId: string;
  classId?: string;
  batchId?: string;
  fullName: string;
  email: string;
  enrollmentNumber?: string;
  averagePercentage: number;
  assessmentCount: number;
}

export interface ReportBatch {
  batchId: string;
  classId: string;
  collegeId: string;
  name: string;
  studentCount: number;
  students: ReportStudent[];
}

export interface ReportClass {
  classId: string;
  collegeId: string;
  name: string;
  batches: ReportBatch[];
}

export interface ReportContext {
  totals: ReportContextTotals;
  classes: ReportClass[];
}

export interface ReportEmailPayload {
  targetEmail?: string;
  targetEmails?: string[];
  reportType: 'college' | 'branch' | 'student';
  entityId: string;
  format: 'pdf' | 'excel';
}

@Injectable({
  providedIn: 'root'
})
export class ReportsService {
  constructor(
    private http: HttpClient,
    private httpHelper: HttpHelperService,
    private apiClient: HttpClientService
  ) {}

  getTrainerContext(): Observable<ReportContext> {
    return this.apiClient.get<ReportContext>('reports/context/trainer');
  }

  getCollegeAdminContext(): Observable<ReportContext> {
    return this.apiClient.get<ReportContext>('reports/context/college-admin');
  }

  getStudentContext(): Observable<ReportStudent> {
    return this.apiClient.get<ReportStudent>('reports/context/student');
  }

  getStudentsForBatch(classId: string, batchId: string): Observable<ReportStudent[]> {
    return this.apiClient.get<ReportStudent[]>(`reports/classes/${classId}/batches/${batchId}/students`);
  }

  exportCollegeReport(collegeId: string, format: string = 'pdf'): Observable<Blob> {
    return this.http.get(`${this.httpHelper.api}reports/export/college/${collegeId}?format=${format}`, {
      responseType: 'blob'
    });
  }

  exportBranchReport(branchId: string, format: string = 'pdf'): Observable<Blob> {
    return this.http.get(`${this.httpHelper.api}reports/export/branch/${branchId}?format=${format}`, {
      responseType: 'blob'
    });
  }

  exportStudentReport(studentId: string, format: string = 'pdf'): Observable<Blob> {
    return this.http.get(`${this.httpHelper.api}reports/export/student/${studentId}?format=${format}`, {
      responseType: 'blob'
    });
  }

  emailReport(request: ReportEmailPayload): Observable<any> {
    return this.http.post(`${this.httpHelper.api}reports/export/email`, request);
  }
}
