import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClientService } from '../http/http-client.service';

export interface StudentAssessmentItem {
  assessmentId: string;
  assessmentName: string;
  subjectName?: string | null;
  topicName?: string | null;
  assessmentStatus: string;
  durationMinutes: number;
  totalMarks: number;
  difficultyLevel: number;
  startDateTime?: string | null;
  endDateTime?: string | null;
}

export interface StudentAssessmentDetail {
  assessmentName: string;
  durationMinutes: number;
  totalMarks: number;
  totalQuestions: number;
  startTime?: string | null;
  endTime?: string | null;
  instructions?: string | null;
}

export interface StudentAttemptRecoveryQuestion {
  questionId: string;
  displayOrder: number;
  questionType: string;
  questionText: string;
  options?: string[] | null;
  marks: number;
  negativeMarks: number;
  difficultyLevel: number;
  allowsMultipleAnswers: boolean;
  selectedAnswer?: string | null;
  selectedAnswers?: string[] | null;
  answeredAt?: string | null;
}

export interface SaveStudentAttemptAnswerRequest {
  selectedAnswer?: string | null;
  selectedAnswers?: string[] | null;
}

export interface StartStudentAssessmentRequest {
  browserName?: string | null;
  browserVersion?: string | null;
  operatingSystem?: string | null;
  deviceType?: string | null;
  userAgent?: string | null;
  ipAddress?: string | null;
}

export interface StartProctorSessionRequest extends StartStudentAssessmentRequest {
  attemptId: string;
  assessmentId: string;
  studentId?: string | null;
  startedAt?: string | null;
}

export interface ProctorSessionResponse {
  sessionId: string;
  attemptId: string;
  assessmentId: string;
  studentId: string;
  status: string;
  startedAt?: string | null;
  endedAt?: string | null;
}

export interface SessionHeartbeatRequest {
  attemptId: string;
  clientTimestamp?: string | null;
  visibilityState: 'Visible' | 'Hidden' | 'Unknown';
  isFullscreen: boolean;
  networkStatus: 'Online' | 'Offline' | 'Unstable' | 'Unknown';
  questionId?: string | null;
}

export interface SessionHeartbeatResponse {
  sessionId: string;
  lastHeartbeatAt: string;
}

export interface StudentAttemptAnswer {
  questionId: string;
  selectedAnswer?: string | null;
  selectedAnswers?: string[] | null;
  answeredAt?: string | null;
}

export interface StudentAttemptSubmitResult {
  attemptId: string;
  attemptStatus: string;
  submittedAt?: string | null;
}

export interface StudentAttemptRecovery {
  attemptId: string;
  assessmentId: string;
  assessmentName: string;
  attemptStatus: string;
  startedAt?: string | null;
  submittedAt?: string | null;
  expiresAt?: string | null;
  remainingSeconds: number;
  durationMinutes: number;
  totalMarks: number;
  totalQuestions: number;
  attemptedQuestions: number;
  unansweredQuestions: number;
  instructions?: string | null;
  questions: StudentAttemptRecoveryQuestion[];
}

@Injectable({ providedIn: 'root' })
export class StudentAssessmentsService {
  private readonly url = 'students/assessments';

  constructor(private readonly http: HttpClientService) {}

  getAssessments(assessmentStatuses: string[]): Observable<StudentAssessmentItem[]> {
    let params = new HttpParams();

    for (const assessmentStatus of assessmentStatuses) {
      params = params.append('assessmentStatuses', assessmentStatus);
    }

    return this.http.post<StudentAssessmentItem[]>(this.url, {}, params);
  }

  getAssessmentDetail(assessmentId: string): Observable<StudentAssessmentDetail> {
    return this.http.get<StudentAssessmentDetail>(`${this.url}/${assessmentId}`);
  }

  startAssessment(
    assessmentId: string,
    request: StartStudentAssessmentRequest = {}
  ): Observable<StudentAttemptRecovery> {
    return this.http.post<StudentAttemptRecovery>(`${this.url}/${assessmentId}/start`, request);
  }

  startProctorSession(attemptId: string, request: StartProctorSessionRequest): Observable<ProctorSessionResponse> {
    return this.http.post<ProctorSessionResponse>(`proctor/attempts/${attemptId}/session`, request);
  }

  sendSessionHeartbeat(sessionId: string, request: SessionHeartbeatRequest): Observable<SessionHeartbeatResponse> {
    return this.http.post<SessionHeartbeatResponse>(`sessionhealth/sessions/${sessionId}/heartbeat`, request);
  }

  getAttemptRecovery(attemptId: string): Observable<StudentAttemptRecovery> {
    return this.http.get<StudentAttemptRecovery>(`students/attempts/${attemptId}`);
  }

  saveAttemptAnswer(
    attemptId: string,
    questionId: string,
    request: SaveStudentAttemptAnswerRequest
  ): Observable<StudentAttemptAnswer> {
    return this.http.put<StudentAttemptAnswer>(`students/attempts/${attemptId}/${questionId}/answers`, request);
  }

  submitAttempt(attemptId: string): Observable<StudentAttemptSubmitResult> {
    return this.http.post<StudentAttemptSubmitResult>(`students/attempts/${attemptId}/submit`);
  }
}
