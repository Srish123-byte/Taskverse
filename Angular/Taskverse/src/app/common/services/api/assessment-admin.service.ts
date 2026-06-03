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

export interface AssessmentManagementSearchRequest {
  searchTerm?: string;
  assessmentStatus?: string;
  difficultyLevel?: number;
  pageNumber: number;
  pageSize: number;
}

export interface AssessmentManagementItem {
  assessmentId: string;
  assessmentName: string;
  category: string;
  topicName?: string | null;
  assessmentStatus: string;
  assessmentDate: string;
  totalMarks: number;
  difficultyLevel: number;
}

export interface AssessmentManagementSearchResult {
  items: AssessmentManagementItem[];
  totalCount: number;
  activeCount: number;
  completedCount: number;
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
  passingPercentage: number;
  assignedBatchIds: string[];
  questionIds: string[];
  durationMinutes: number;
  totalMarks: number;
  startDateTime?: string | null;
  endDateTime?: string | null;
  isDraftSave?: boolean;
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
  passingPercentage: number;
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

  searchAssessments(request: AssessmentManagementSearchRequest): Observable<AssessmentManagementSearchResult> {
    return this.http
      .post<any>(`${this.url}/search`, request)
      .pipe(map((result: any) => this.mapAssessmentManagementSearchResult(result)));
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

  createAssessment(request: CreateAssessmentRequest, skipGlobalErrorRedirect = false): Observable<AssessmentRecord> {
    return this.http.post<AssessmentRecord>(this.url, request, undefined, skipGlobalErrorRedirect);
  }

  getAssessment(assessmentId: string): Observable<AssessmentRecord> {
    return this.http.get<AssessmentRecord>(`${this.url}/${assessmentId}`);
  }

  updateAssessment(assessmentId: string, request: CreateAssessmentRequest, skipGlobalErrorRedirect = false): Observable<AssessmentRecord> {
    return this.http.put<AssessmentRecord>(`${this.url}/${assessmentId}`, request, skipGlobalErrorRedirect);
  }

  publishAssessment(request: PublishAssessmentRequest, skipGlobalErrorRedirect = false): Observable<AssessmentRecord> {
    return this.http.post<AssessmentRecord>(`${this.url}/publish`, request, undefined, skipGlobalErrorRedirect);
  }

  deleteQuestions(request: DeleteQuestionsRequest, skipGlobalErrorRedirect = false): Observable<DeleteQuestionsResponse> {
    return this.http.delete<DeleteQuestionsResponse>(
      `${this.url}/questions`,
      undefined,
      request,
      skipGlobalErrorRedirect
    );
  }

  deleteAssessment(assessmentId: string, skipGlobalErrorRedirect = false): Observable<void> {
    return this.http.delete<void>(`${this.url}/${assessmentId}`, undefined, undefined, skipGlobalErrorRedirect);
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

  private mapAssessmentManagementSearchResult(result: any): AssessmentManagementSearchResult {
    return {
      items: (result?.items ?? result?.Items ?? []).map((item: any) => this.mapAssessmentManagementItem(item)),
      totalCount: result?.totalCount ?? result?.TotalCount ?? 0,
      activeCount: result?.activeCount ?? result?.ActiveCount ?? 0,
      completedCount: result?.completedCount ?? result?.CompletedCount ?? 0,
      pageNumber: result?.pageNumber ?? result?.PageNumber ?? 1,
      pageSize: result?.pageSize ?? result?.PageSize ?? 10
    };
  }

  private mapAssessmentManagementItem(item: any): AssessmentManagementItem {
    return {
      assessmentId: item?.assessmentId ?? item?.AssessmentId ?? '',
      assessmentName: item?.assessmentName ?? item?.AssessmentName ?? '',
      category: item?.category ?? item?.Category ?? '',
      topicName: item?.topicName ?? item?.TopicName ?? null,
      assessmentStatus: item?.assessmentStatus ?? item?.AssessmentStatus ?? '',
      assessmentDate: item?.assessmentDate ?? item?.AssessmentDate ?? '',
      totalMarks: item?.totalMarks ?? item?.TotalMarks ?? 0,
      difficultyLevel: item?.difficultyLevel ?? item?.DifficultyLevel ?? 0
    };
  }
}
