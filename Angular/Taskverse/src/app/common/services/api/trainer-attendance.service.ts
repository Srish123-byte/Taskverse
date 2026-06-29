import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpClientService } from '../http/http-client.service';
import { HttpHelperService } from '../http/http-helper.service';

export enum AttendanceSessionType {
  PreBreak = 1,
  PostBreak = 2
}

export enum AttendanceEntryType {
  Present = 1,
  Absent = 2
}

export interface AttendanceBatchOption {
  batchId: string;
  batchName: string;
  batchOwnerTrainerName?: string | null;
}

export interface AttendanceBatchGroup {
  classId: string;
  className: string;
  academicYear?: string | null;
  batches: AttendanceBatchOption[];
}

export interface AttendanceStudent {
  studentId: string;
  userId: string;
  fullName: string;
  email: string;
  enrollmentNumber?: string | null;
  attendanceEntry?: AttendanceEntryType | null;
}

export interface AttendanceRoster {
  classId: string;
  className: string;
  academicYear?: string | null;
  batchId: string;
  batchName: string;
  attendanceDate: string;
  attendanceSession: AttendanceSessionType;
  isSubmitted: boolean;
  isLocked: boolean;
  canEdit: boolean;
  submittedByTrainerName?: string | null;
  batchOwnerTrainerName?: string | null;
  submittedAt?: string | null;
  lastModifiedAt?: string | null;
  totalStudents: number;
  presentCount: number;
  absentCount: number;
  attendancePercentage: number;
  students: AttendanceStudent[];
}

export interface SubmitAttendanceRequest {
  batchId: string;
  attendanceDate: string;
  attendanceSession: AttendanceSessionType;
  entries: SubmitAttendanceEntryRequest[];
}

export interface SubmitAttendanceEntryRequest {
  studentId: string;
  attendanceEntry: AttendanceEntryType;
}

export interface AttendanceHistoryItem {
  attendanceSessionId: string;
  attendanceDate: string;
  attendanceSession: AttendanceSessionType;
  submittedByTrainerName: string;
  batchOwnerTrainerName?: string | null;
  submittedAt: string;
  lastModifiedAt: string;
  isLocked: boolean;
  totalStudents: number;
  presentCount: number;
  absentCount: number;
  attendancePercentage: number;
}

export interface AttendanceHistory {
  batchId: string;
  batchName: string;
  fromDate: string;
  toDate: string;
  items: AttendanceHistoryItem[];
}

export interface EmailAttendanceReportRequest {
  batchId: string;
  fromDate: string;
  toDate: string;
  recipientEmails: string[];
}

export interface DownloadedAttendanceExport {
  blob: Blob;
  fileName: string;
}

@Injectable({ providedIn: 'root' })
export class TrainerAttendanceService {
  private readonly url = 'attendance';

  constructor(
    private readonly http: HttpClientService,
    private readonly rawHttp: HttpClient,
    private readonly httpHelper: HttpHelperService
  ) {}

  getAttendanceBatches(): Observable<AttendanceBatchGroup[]> {
    return this.http
      .get<any[]>(`${this.url}/batches`)
      .pipe(map(items => (items ?? []).map(item => this.mapBatchGroup(item))));
  }

  getAttendanceRoster(batchId: string, attendanceDate: string, attendanceSession: AttendanceSessionType): Observable<AttendanceRoster> {
    let params = new HttpParams()
      .set('batchId', batchId)
      .set('attendanceDate', attendanceDate)
      .set('attendanceSession', attendanceSession);

    return this.http
      .get<any>(`${this.url}/roster`, params)
      .pipe(map(item => this.mapRoster(item)));
  }

  submitAttendance(request: SubmitAttendanceRequest): Observable<AttendanceRoster> {
    return this.http
      .post<any>(`${this.url}/sessions`, request)
      .pipe(map(item => this.mapRoster(item)));
  }

  getAttendanceHistory(batchId: string, fromDate: string, toDate: string): Observable<AttendanceHistory> {
    let params = new HttpParams()
      .set('batchId', batchId)
      .set('fromDate', fromDate)
      .set('toDate', toDate);

    return this.http
      .get<any>(`${this.url}/history`, params)
      .pipe(map(item => this.mapHistory(item)));
  }

  exportAttendance(batchId: string, fromDate: string, toDate: string): Observable<DownloadedAttendanceExport> {
    let params = new HttpParams()
      .set('batchId', batchId)
      .set('fromDate', fromDate)
      .set('toDate', toDate);

    const options: any = this.httpHelper.getOptions(params);
    options.responseType = 'blob';

    return this.rawHttp
      .get(`${this.httpHelper.api}${this.url}/export`, options)
      .pipe(
        map((response: any) => {
          const contentDisposition = response.headers?.get?.('content-disposition') ?? '';
          const fileNameMatch = /filename="?([^"]+)"?/i.exec(contentDisposition);

          return {
            blob: response.body as Blob,
            fileName: fileNameMatch?.[1] ?? 'attendance-report.xls'
          };
        })
      );
  }

  emailAttendanceReport(request: EmailAttendanceReportRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.url}/email-report`, request);
  }

  private mapBatchGroup(item: any): AttendanceBatchGroup {
    return {
      classId: item?.classId ?? item?.ClassId ?? '',
      className: item?.className ?? item?.ClassName ?? '',
      academicYear: item?.academicYear ?? item?.AcademicYear ?? null,
      batches: (item?.batches ?? item?.Batches ?? []).map((batch: any) => this.mapBatchOption(batch))
    };
  }

  private mapBatchOption(item: any): AttendanceBatchOption {
    return {
      batchId: item?.batchId ?? item?.BatchId ?? '',
      batchName: item?.batchName ?? item?.BatchName ?? '',
      batchOwnerTrainerName: item?.batchOwnerTrainerName ?? item?.BatchOwnerTrainerName ?? null
    };
  }

  private mapRoster(item: any): AttendanceRoster {
    return {
      classId: item?.classId ?? item?.ClassId ?? '',
      className: item?.className ?? item?.ClassName ?? '',
      academicYear: item?.academicYear ?? item?.AcademicYear ?? null,
      batchId: item?.batchId ?? item?.BatchId ?? '',
      batchName: item?.batchName ?? item?.BatchName ?? '',
      attendanceDate: item?.attendanceDate ?? item?.AttendanceDate ?? '',
      attendanceSession: item?.attendanceSession ?? item?.AttendanceSession ?? AttendanceSessionType.PreBreak,
      isSubmitted: item?.isSubmitted ?? item?.IsSubmitted ?? false,
      isLocked: item?.isLocked ?? item?.IsLocked ?? false,
      canEdit: item?.canEdit ?? item?.CanEdit ?? false,
      submittedByTrainerName: item?.submittedByTrainerName ?? item?.SubmittedByTrainerName ?? null,
      batchOwnerTrainerName: item?.batchOwnerTrainerName ?? item?.BatchOwnerTrainerName ?? null,
      submittedAt: item?.submittedAt ?? item?.SubmittedAt ?? null,
      lastModifiedAt: item?.lastModifiedAt ?? item?.LastModifiedAt ?? null,
      totalStudents: item?.totalStudents ?? item?.TotalStudents ?? 0,
      presentCount: item?.presentCount ?? item?.PresentCount ?? 0,
      absentCount: item?.absentCount ?? item?.AbsentCount ?? 0,
      attendancePercentage: item?.attendancePercentage ?? item?.AttendancePercentage ?? 0,
      students: (item?.students ?? item?.Students ?? []).map((student: any) => this.mapStudent(student))
    };
  }

  private mapStudent(item: any): AttendanceStudent {
    return {
      studentId: item?.studentId ?? item?.StudentId ?? '',
      userId: item?.userId ?? item?.UserId ?? '',
      fullName: item?.fullName ?? item?.FullName ?? '',
      email: item?.email ?? item?.Email ?? '',
      enrollmentNumber: item?.enrollmentNumber ?? item?.EnrollmentNumber ?? null,
      attendanceEntry: item?.attendanceEntry ?? item?.AttendanceEntry ?? null
    };
  }

  private mapHistory(item: any): AttendanceHistory {
    return {
      batchId: item?.batchId ?? item?.BatchId ?? '',
      batchName: item?.batchName ?? item?.BatchName ?? '',
      fromDate: item?.fromDate ?? item?.FromDate ?? '',
      toDate: item?.toDate ?? item?.ToDate ?? '',
      items: (item?.items ?? item?.Items ?? []).map((historyItem: any) => this.mapHistoryItem(historyItem))
    };
  }

  private mapHistoryItem(item: any): AttendanceHistoryItem {
    return {
      attendanceSessionId: item?.attendanceSessionId ?? item?.AttendanceSessionId ?? '',
      attendanceDate: item?.attendanceDate ?? item?.AttendanceDate ?? '',
      attendanceSession: item?.attendanceSession ?? item?.AttendanceSession ?? AttendanceSessionType.PreBreak,
      submittedByTrainerName: item?.submittedByTrainerName ?? item?.SubmittedByTrainerName ?? '',
      batchOwnerTrainerName: item?.batchOwnerTrainerName ?? item?.BatchOwnerTrainerName ?? null,
      submittedAt: item?.submittedAt ?? item?.SubmittedAt ?? '',
      lastModifiedAt: item?.lastModifiedAt ?? item?.LastModifiedAt ?? '',
      isLocked: item?.isLocked ?? item?.IsLocked ?? false,
      totalStudents: item?.totalStudents ?? item?.TotalStudents ?? 0,
      presentCount: item?.presentCount ?? item?.PresentCount ?? 0,
      absentCount: item?.absentCount ?? item?.AbsentCount ?? 0,
      attendancePercentage: item?.attendancePercentage ?? item?.AttendancePercentage ?? 0
    };
  }
}
