import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClientService } from '../http/http-client.service';

@Injectable({ providedIn: 'root' })
export class AssessmentAdminService {
  private readonly url = 'assessments';

  constructor(private readonly http: HttpClientService) {}

  deleteAssessment(assessmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${assessmentId}`);
  }
}
