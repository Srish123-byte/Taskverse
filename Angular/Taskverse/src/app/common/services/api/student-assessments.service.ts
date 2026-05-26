import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subject, of } from 'rxjs';
import { finalize, shareReplay, tap } from 'rxjs/operators';
import { HttpClientService } from '../http/http-client.service';

export interface StudentAssessmentItem {
  assessmentId: string;
  assessmentName: string;
  assessmentStatus: string;
  durationMinutes: number;
  totalMarks: number;
  difficultyLevel: number;
  startDateTime?: string | null;
  endDateTime?: string | null;
}

@Injectable({ providedIn: 'root' })
export class StudentAssessmentsService {
  private readonly url = 'student/assessments';
  private readonly assessmentsCache = new Map<string, StudentAssessmentItem[]>();
  private readonly inFlightRequests = new Map<string, Observable<StudentAssessmentItem[]>>();
  private readonly assessmentsCacheResetSubject = new Subject<void>();

  readonly assessmentsCacheReset$ = this.assessmentsCacheResetSubject.asObservable();

  constructor(private readonly http: HttpClientService) {}

  getAssessments(assessmentStatuses: string[]): Observable<StudentAssessmentItem[]> {
    const cacheKey = this.getCacheKey(assessmentStatuses);
    const cachedAssessments = this.assessmentsCache.get(cacheKey);

    if (cachedAssessments) {
      return of(cachedAssessments);
    }

    const inFlightRequest = this.inFlightRequests.get(cacheKey);
    if (inFlightRequest) {
      return inFlightRequest;
    }

    let params = new HttpParams();

    for (const assessmentStatus of assessmentStatuses) {
      params = params.append('assessmentStatuses', assessmentStatus);
    }

    const request = this.http.post<StudentAssessmentItem[]>(this.url, {}, params).pipe(
      tap(assessments => {
        this.assessmentsCache.set(cacheKey, assessments ?? []);
      }),
      finalize(() => {
        this.inFlightRequests.delete(cacheKey);
      }),
      shareReplay(1)
    );

    this.inFlightRequests.set(cacheKey, request);

    return request;
  }

  clearAssessmentsCache(): void {
    this.assessmentsCache.clear();
    this.inFlightRequests.clear();
    this.assessmentsCacheResetSubject.next();
  }

  private getCacheKey(assessmentStatuses: string[]): string {
    return assessmentStatuses.map(status => status.trim().toUpperCase()).sort().join('|');
  }
}
