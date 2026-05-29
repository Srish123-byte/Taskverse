import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
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
  topicTag: string;
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
  topicTag?: string | null;
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

  updateQuestion(questionId: string, request: CreateQuestionRequest): Observable<QuestionBankItem> {
    return this.http.put<QuestionBankItem>(`${this.url}/questions/${questionId}`, request);
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
}
