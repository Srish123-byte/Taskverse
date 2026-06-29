import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpHelperService } from '../http/http-helper.service';

@Injectable({
  providedIn: 'root'
})
export class ReportsService {
  constructor(
    private http: HttpClient,
    private httpHelper: HttpHelperService
  ) {}

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
}
