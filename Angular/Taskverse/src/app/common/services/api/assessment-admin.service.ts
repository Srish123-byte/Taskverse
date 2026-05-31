import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpClientService } from '../http/http-client.service';

export interface QuestionBankSearchRequest {
  difficultyLevel?: number;
  subjectId?: string;
  topicId?: string;
  subject?: string;
  topic?: string;
  pageNumber: number;
  pageSize: number;
}

export interface CreateQuestionRequest {
  stream: string;
  subjectId?: string;
  subject?: string;
  topicId?: string;
  topic?: string;
  topicTag: string[];
  questionType: string;
  questionText: string;
  options?: string[];
  answer: string;
  explanation?: string;
  marks: number;
  negativeMarks: number;
  difficultyLevel: number;
  sourceRowNumber?: number;
}

export interface QuestionBankItem {
  questionId: string;
  collegeId: string;
  subjectId?: string | null;
  topicId?: string | null;
  stream?: string | null;
  subject?: string | null;
  topic?: string | null;
  topicTag?: string[] | null;
  questionType: string;
  questionText: string;
  options?: string[] | null;
  answer?: string | null;
  explanation?: string | null;
  marks: number;
  negativeMarks: number;
  difficultyLevel: number;
  version: number;
  createdBy: string;
  createdAt: string;
  modifiedAt?: string | null;
}

export interface PagedQuestionBankResult {
  items: QuestionBankItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface AssessmentTopicCatalogItem {
  topicId: string;
  topicName: string;
  batchIds: string[];
}

export interface AssessmentSubjectCatalogItem {
  subjectId: string;
  subjectName: string;
  batchIds: string[];
  topics: AssessmentTopicCatalogItem[];
}

export interface AssessmentSubjectTopicCatalog {
  subjects: AssessmentSubjectCatalogItem[];
}

export interface AssessmentAssignmentBatch {
  batchId: string;
  classId: string;
  collegeId: string;
  name: string;
}

export interface AssessmentAssignmentClass {
  classId: string;
  collegeId: string;
  name: string;
  academicYear?: string;
  batches: AssessmentAssignmentBatch[];
}

export interface AssessmentAssignmentCatalog {
  classes: AssessmentAssignmentClass[];
}

export interface CreateAssessmentRequest {
  assessmentName: string;
  subjectId?: string | null;
  subjectName?: string | null;
  topicId?: string | null;
  topicName?: string | null;
  instructions?: string | null;
  allowLateEntry: boolean;
  allowQuestionReview: boolean;
  negativeMarking: boolean;
  assignedBatchIds: string[];
  questionIds: string[];
  durationMinutes: number;
  totalMarks: number;
  startDateTime?: string | null;
  endDateTime?: string | null;
}

export interface PublishAssessmentRequest extends CreateAssessmentRequest {
  assessmentId?: string | null;
}

export interface AssessmentRecord {
  assessmentId: string;
  collegeId: string;
  subjectId?: string | null;
  subjectName?: string | null;
  topicId?: string | null;
  topicName?: string | null;
  assessmentName: string;
  assessmentType: string;
  assessmentStatus: string;
  durationMinutes: number;
  totalMarks: number;
  difficultyLevel: number;
  startDateTime?: string | null;
  endDateTime?: string | null;
  instructions?: string | null;
  assignedBatchIds: string[];
  allowLateEntry: boolean;
  showResultsImmediately: boolean;
  allowQuestionReview: boolean;
  negativeMarking: boolean;
  isTotalMarksAutoCalculated?: boolean | null;
  createdBy: string;
  createdAt: string;
  modifiedAt?: string | null;
  questionIds: string[];
}

export interface DeleteQuestionsRequest {
  questionIds: string[];
}

export interface DeleteQuestionsResponse {
  deletedQuestionIds: string[];
}

@Injectable({ providedIn: 'root' })
export class AssessmentAdminService {
  private readonly url = 'assessments';

  constructor(private readonly http: HttpClientService) {}

  getQuestion(questionId: string, skipGlobalErrorRedirect = false): Observable<QuestionBankItem> {
    return this.http.get<QuestionBankItem>(`${this.url}/questions/${questionId}`, undefined, skipGlobalErrorRedirect);
  }

  searchQuestionBank(request: QuestionBankSearchRequest): Observable<PagedQuestionBankResult> {
    return this.http.post<PagedQuestionBankResult>(`${this.url}/questions/search`, request);
  }

  createQuestions(request: CreateQuestionRequest[]): Observable<QuestionBankItem[]> {
    return this.http.post<QuestionBankItem[]>(`${this.url}/questions`, request);
  }

  getSubjectTopicCatalog(): Observable<AssessmentSubjectTopicCatalog> {
    return this.http.get<AssessmentSubjectTopicCatalog>(`${this.url}/subjects-topics/catalog`);
  }

  getTrainerAssignedClassesAndBatches(): Observable<AssessmentAssignmentCatalog> {
    return this.http
      .get<any>(`${this.url}/trainer/assigned-classes-batches`)
      .pipe(map((catalog: any) => this.mapAssignmentCatalog(catalog)));
  }

  updateQuestion(questionId: string, request: CreateQuestionRequest): Observable<QuestionBankItem> {
    return this.http.put<QuestionBankItem>(`${this.url}/questions/${questionId}`, request);
  }

  createAssessment(request: CreateAssessmentRequest): Observable<AssessmentRecord> {
    return this.http.post<AssessmentRecord>(this.url, request);
  }

  publishAssessment(request: PublishAssessmentRequest): Observable<AssessmentRecord> {
    return this.http.post<AssessmentRecord>(`${this.url}/publish`, request);
  }

  deleteQuestions(request: DeleteQuestionsRequest, skipGlobalErrorRedirect = false): Observable<DeleteQuestionsResponse> {
    return this.http.delete<DeleteQuestionsResponse>(
      `${this.url}/questions`,
      undefined,
      request,
      skipGlobalErrorRedirect
    );
  }

  deleteAssessment(assessmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${assessmentId}`);
  }

  private mapAssignmentCatalog(catalog: any): AssessmentAssignmentCatalog {
    return {
      classes: (catalog?.classes ?? catalog?.Classes ?? []).map((item: any) => this.mapAssignmentClass(item))
    };
  }

  private mapAssignmentClass(item: any): AssessmentAssignmentClass {
    return {
      classId: item?.classId ?? item?.ClassId ?? '',
      collegeId: item?.collegeId ?? item?.CollegeId ?? '',
      name: item?.name ?? item?.Name ?? '',
      academicYear: item?.academicYear ?? item?.AcademicYear ?? undefined,
      batches: (item?.batches ?? item?.Batches ?? []).map((batch: any) => this.mapAssignmentBatch(batch))
    };
  }

  private mapAssignmentBatch(item: any): AssessmentAssignmentBatch {
    return {
      batchId: item?.batchId ?? item?.BatchId ?? '',
      classId: item?.classId ?? item?.ClassId ?? '',
      collegeId: item?.collegeId ?? item?.CollegeId ?? '',
      name: item?.name ?? item?.Name ?? ''
    };
  }
}
