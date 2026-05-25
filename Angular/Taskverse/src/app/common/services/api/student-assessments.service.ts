import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
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

  constructor(private readonly http: HttpClientService) {}

  getAssessments(assessmentStatuses: string[]): Observable<StudentAssessmentItem[]> {
    let params = new HttpParams();

    for (const assessmentStatus of assessmentStatuses) {
      params = params.append('assessmentStatuses', assessmentStatus);
    }

    return this.http.post<StudentAssessmentItem[]>(this.url, {}, params);
  }
}
