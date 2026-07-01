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
  stream?: string | null;
  subjectId?: string;
  subject?: string | null;
  topicId?: string;
  topic?: string | null;
  topicTag?: string[] | null;
  questionType?: string | null;
  questionText?: string | null;
  options?: string[];
  answer?: string | null;
  correctAnswers?: string[];
  explanation?: string | null;
  marks: number;
  negativeMarks: number;
  difficultyLevel: number;
  questionTitle?: string | null;
  problemStatement?: string | null;
  detailedDescription?: string | null;
  inputFormat?: string | null;
  outputFormat?: string | null;
  constraintsText?: string | null;
  defaultLanguageCode?: string | null;
  defaultTimeLimitMs?: number | null;
  defaultMemoryLimitKb?: number | null;
  defaultMaxCodeSizeKb?: number | null;
  examples?: CodingQuestionExample[] | null;
  testCases?: CodingTestCase[] | null;
  sourceRowNumber?: number;
}

export interface CodingQuestionExample {
  input?: string | null;
  output?: string | null;
  explanation?: string | null;
}

export interface CodingTestCase {
  testCaseId?: string;
  inputFormat?: string | null;
  inputData?: string | null;
  expectedOutput?: string | null;
  comparisonMode: number;
  numericTolerance?: number | null;
  isHidden: boolean;
  isSample: boolean;
  timeLimitMs?: number | null;
  memoryLimitKb?: number | null;
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
  questionText?: string | null;
  options?: string[] | null;
  answer?: string | null;
  correctAnswers?: string[] | null;
  allowsMultipleAnswers?: boolean;
  explanation?: string | null;
  marks: number;
  negativeMarks: number;
  difficultyLevel: number;
  questionTitle?: string | null;
  problemStatement?: string | null;
  detailedDescription?: string | null;
  inputFormat?: string | null;
  outputFormat?: string | null;
  constraintsText?: string | null;
  examples?: CodingQuestionExample[] | null;
  defaultLanguageCode?: string | null;
  defaultTimeLimitMs?: number | null;
  defaultMemoryLimitKb?: number | null;
  defaultMaxCodeSizeKb?: number | null;
  testCases?: CodingTestCase[] | null;
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

export interface QuestionTopicCatalogItem {
  topicId: string;
  topicName: string;
}

export interface QuestionSubjectCatalogItem {
  subjectId: string;
  subjectName: string;
  topics: QuestionTopicCatalogItem[];
}

export interface QuestionClassificationCatalog {
  subjects: QuestionSubjectCatalogItem[];
}

export interface CreateQuestionClassificationEntryRequest {
  subjectId?: string;
  subjectName?: string;
  topicName?: string;
}

export interface QuestionClassificationEntry {
  subjectId: string;
  subjectName: string;
  topicId?: string | null;
  topicName?: string | null;
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
  subjectIds: string[];
  topicIds: string[];
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
  subjectIds: string[];
  subjectNames: string[];
  subjectName?: string | null;
  subjectDisplayLabel?: string | null;
  topicIds: string[];
  topicNames: string[];
  topicName?: string | null;
  topicDisplayLabel?: string | null;
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
  subjectIds: string[];
  subjectNames: string[];
  subjectName?: string | null;
  subjectDisplayLabel?: string | null;
  topicIds: string[];
  topicNames: string[];
  topicName?: string | null;
  topicDisplayLabel?: string | null;
  assessmentStatus: string;
  assessmentDate?: string | null;
  startDateTime?: string | null;
  totalMarks: number;
  difficultyLevel: number;
}

export interface PagedAssessmentManagementResult {
  items: AssessmentManagementItem[];
  totalCount: number;
  activeCount: number;
  completedCount: number;
  pageNumber: number;
  pageSize: number;
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
    return this.http
      .get<any>(`${this.url}/questions/${questionId}`, undefined, skipGlobalErrorRedirect)
      .pipe(map(question => this.mapQuestionBankItem(question)));
  }

  searchQuestionBank(request: QuestionBankSearchRequest, skipGlobalErrorRedirect = false): Observable<PagedQuestionBankResult> {
    return this.http
      .post<any>(`${this.url}/questions/search`, request, undefined, skipGlobalErrorRedirect)
      .pipe(map(result => this.mapPagedQuestionBankResult(result)));
  }

  getQuestionClassificationCatalog(): Observable<QuestionClassificationCatalog> {
    return this.http.get<QuestionClassificationCatalog>(`${this.url}/questions/catalog`);
  }

  createQuestionClassificationEntry(request: CreateQuestionClassificationEntryRequest): Observable<QuestionClassificationEntry> {
    return this.http
      .post<any>(`${this.url}/questions/catalog/items`, request)
      .pipe(map((item: any) => this.mapQuestionClassificationEntry(item)));
  }

  searchAssessments(request: AssessmentManagementSearchRequest): Observable<PagedAssessmentManagementResult> {
    return this.http
      .post<any>(`${this.url}/search`, request)
      .pipe(map((result: any) => this.mapAssessmentManagementResult(result)));
  }

  createQuestions(request: CreateQuestionRequest[]): Observable<QuestionBankItem[]> {
    return this.http
      .post<any[]>(`${this.url}/questions`, request)
      .pipe(map(items => (items ?? []).map(item => this.mapQuestionBankItem(item))));
  }

  getTrainerAssignedClassesAndBatches(): Observable<AssessmentAssignmentCatalog> {
    return this.http
      .get<any>(`${this.url}/trainer/assigned-classes-batches`)
      .pipe(map((catalog: any) => this.mapAssignmentCatalog(catalog)));
  }

  updateQuestion(questionId: string, request: CreateQuestionRequest): Observable<QuestionBankItem> {
    return this.http
      .put<any>(`${this.url}/questions/${questionId}`, request)
      .pipe(map(question => this.mapQuestionBankItem(question)));
  }

  createAssessment(request: CreateAssessmentRequest, skipGlobalErrorRedirect = false): Observable<AssessmentRecord> {
    return this.http
      .post<any>(this.url, request, undefined, skipGlobalErrorRedirect)
      .pipe(map((assessment: any) => this.mapAssessmentRecord(assessment)));
  }

  getAssessment(assessmentId: string): Observable<AssessmentRecord> {
    return this.http
      .get<any>(`${this.url}/${assessmentId}`)
      .pipe(map((assessment: any) => this.mapAssessmentRecord(assessment)));
  }

  updateAssessment(assessmentId: string, request: CreateAssessmentRequest, skipGlobalErrorRedirect = false): Observable<AssessmentRecord> {
    return this.http
      .put<any>(`${this.url}/${assessmentId}`, request, skipGlobalErrorRedirect)
      .pipe(map((assessment: any) => this.mapAssessmentRecord(assessment)));
  }

  publishAssessment(request: PublishAssessmentRequest, skipGlobalErrorRedirect = false): Observable<AssessmentRecord> {
    return this.http
      .post<any>(`${this.url}/publish`, request, undefined, skipGlobalErrorRedirect)
      .pipe(map((assessment: any) => this.mapAssessmentRecord(assessment)));
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

  private mapPagedQuestionBankResult(result: any): PagedQuestionBankResult {
    return {
      items: (result?.items ?? result?.Items ?? []).map((item: any) => this.mapQuestionBankItem(item)),
      totalCount: result?.totalCount ?? result?.TotalCount ?? 0,
      pageNumber: result?.pageNumber ?? result?.PageNumber ?? 1,
      pageSize: result?.pageSize ?? result?.PageSize ?? 10
    };
  }

  private mapQuestionBankItem(item: any): QuestionBankItem {
    const answer = item?.answer ?? item?.Answer ?? null;
    const correctAnswers = this.parseStoredAnswers(answer);

    return {
      questionId: item?.questionId ?? item?.QuestionId ?? '',
      collegeId: item?.collegeId ?? item?.CollegeId ?? '',
      subjectId: item?.subjectId ?? item?.SubjectId ?? null,
      topicId: item?.topicId ?? item?.TopicId ?? null,
      stream: item?.stream ?? item?.Stream ?? null,
      subject: item?.subject ?? item?.Subject ?? null,
      topic: item?.topic ?? item?.Topic ?? null,
      topicTag: item?.topicTag ?? item?.TopicTag ?? null,
      questionType: item?.questionType ?? item?.QuestionType ?? '',
      questionText: item?.questionText ?? item?.QuestionText ?? null,
      options: item?.options ?? item?.Options ?? null,
      answer,
      correctAnswers,
      allowsMultipleAnswers: correctAnswers.length > 1,
      explanation: item?.explanation ?? item?.Explanation ?? null,
      marks: item?.marks ?? item?.Marks ?? 0,
      negativeMarks: item?.negativeMarks ?? item?.NegativeMarks ?? 0,
      difficultyLevel: item?.difficultyLevel ?? item?.DifficultyLevel ?? 1,
      questionTitle: item?.questionTitle ?? item?.QuestionTitle ?? null,
      problemStatement: item?.problemStatement ?? item?.ProblemStatement ?? null,
      detailedDescription: item?.detailedDescription ?? item?.DetailedDescription ?? null,
      inputFormat: item?.inputFormat ?? item?.InputFormat ?? null,
      outputFormat: item?.outputFormat ?? item?.OutputFormat ?? null,
      constraintsText: item?.constraintsText ?? item?.ConstraintsText ?? null,
      examples: item?.examples ?? item?.Examples ?? null,
      defaultLanguageCode: item?.defaultLanguageCode ?? item?.DefaultLanguageCode ?? null,
      defaultTimeLimitMs: item?.defaultTimeLimitMs ?? item?.DefaultTimeLimitMs ?? null,
      defaultMemoryLimitKb: item?.defaultMemoryLimitKb ?? item?.DefaultMemoryLimitKb ?? null,
      defaultMaxCodeSizeKb: item?.defaultMaxCodeSizeKb ?? item?.DefaultMaxCodeSizeKb ?? null,
      testCases: item?.testCases ?? item?.TestCases ?? null,
      version: item?.version ?? item?.Version ?? 1,
      createdBy: item?.createdBy ?? item?.CreatedBy ?? '',
      createdAt: item?.createdAt ?? item?.CreatedAt ?? '',
      modifiedAt: item?.modifiedAt ?? item?.ModifiedAt ?? null
    };
  }

  private parseStoredAnswers(answer: string | null | undefined): string[] {
    if (!answer?.trim()) {
      return [];
    }

    try {
      const parsed = JSON.parse(answer);
      if (Array.isArray(parsed)) {
        return parsed.filter((value): value is string => typeof value === 'string' && value.trim().length > 0);
      }
    } catch {
      // Fall back to the single-answer legacy format.
    }

    return [answer.trim()];
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

  private mapAssessmentRecord(record: any): AssessmentRecord {
    return {
      assessmentId: record?.assessmentId ?? record?.assessment_id ?? record?.AssessmentId ?? '',
      collegeId: record?.collegeId ?? record?.college_id ?? record?.CollegeId ?? '',
      subjectIds: record?.subjectIds ?? record?.subject_ids ?? record?.SubjectIds ?? [],
      subjectNames: record?.subjectNames ?? record?.subject_names ?? record?.SubjectNames ?? [],
      subjectName: record?.subjectName ?? record?.subject_name ?? record?.SubjectName ?? null,
      subjectDisplayLabel:
        record?.subjectDisplayLabel ?? record?.subject_display_label ?? record?.SubjectDisplayLabel ??
        record?.subjectName ?? record?.subject_name ?? record?.SubjectName ?? null,
      topicIds: record?.topicIds ?? record?.topic_ids ?? record?.TopicIds ?? [],
      topicNames: record?.topicNames ?? record?.topic_names ?? record?.TopicNames ?? [],
      topicName: record?.topicName ?? record?.topic_name ?? record?.TopicName ?? null,
      topicDisplayLabel:
        record?.topicDisplayLabel ?? record?.topic_display_label ?? record?.TopicDisplayLabel ??
        record?.topicName ?? record?.topic_name ?? record?.TopicName ?? null,
      assessmentName: record?.assessmentName ?? record?.assessment_name ?? record?.AssessmentName ?? '',
      assessmentType: record?.assessmentType ?? record?.assessment_type ?? record?.AssessmentType ?? '',
      assessmentStatus: record?.assessmentStatus ?? record?.assessment_status ?? record?.AssessmentStatus ?? '',
      durationMinutes: record?.durationMinutes ?? record?.duration_minutes ?? record?.DurationMinutes ?? 0,
      totalMarks: record?.totalMarks ?? record?.total_marks ?? record?.TotalMarks ?? 0,
      difficultyLevel: record?.difficultyLevel ?? record?.difficulty_level ?? record?.DifficultyLevel ?? 0,
      startDateTime: record?.startDateTime ?? record?.start_datetime ?? record?.StartDateTime ?? null,
      endDateTime: record?.endDateTime ?? record?.end_datetime ?? record?.EndDateTime ?? null,
      instructions: record?.instructions ?? record?.Instructions ?? null,
      assignedBatchIds: record?.assignedBatchIds ?? record?.assigned_batch_ids ?? record?.AssignedBatchIds ?? [],
      allowLateEntry: record?.allowLateEntry ?? record?.allow_late_entry ?? record?.AllowLateEntry ?? false,
      showResultsImmediately: record?.showResultsImmediately ?? record?.show_results_immediately ?? record?.ShowResultsImmediately ?? false,
      passingPercentage: record?.passingPercentage ?? record?.passing_percentage ?? record?.PassingPercentage ?? 0,
      allowQuestionReview: record?.allowQuestionReview ?? record?.allow_question_review ?? record?.AllowQuestionReview ?? false,
      negativeMarking: record?.negativeMarking ?? record?.negative_marking ?? record?.NegativeMarking ?? false,
      isTotalMarksAutoCalculated:
        record?.isTotalMarksAutoCalculated ?? record?.is_total_marks_auto_calculated ?? record?.IsTotalMarksAutoCalculated ?? null,
      createdBy: record?.createdBy ?? record?.created_by ?? record?.CreatedBy ?? '',
      createdAt: record?.createdAt ?? record?.created_at ?? record?.CreatedAt ?? '',
      modifiedAt: record?.modifiedAt ?? record?.modified_at ?? record?.ModifiedAt ?? null,
      questionIds: record?.questionIds ?? record?.question_ids ?? record?.QuestionIds ?? []
    };
  }

  private mapAssessmentManagementResult(result: any): PagedAssessmentManagementResult {
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
      assessmentId: item?.assessmentId ?? item?.assessment_id ?? item?.AssessmentId ?? '',
      assessmentName: item?.assessmentName ?? item?.assessment_name ?? item?.AssessmentName ?? '',
      subjectIds: item?.subjectIds ?? item?.subject_ids ?? item?.SubjectIds ?? [],
      subjectNames: item?.subjectNames ?? item?.subject_names ?? item?.SubjectNames ?? [],
      subjectName: item?.subjectName ?? item?.subject_name ?? item?.SubjectName ?? null,
      subjectDisplayLabel:
        item?.subjectDisplayLabel ?? item?.subject_display_label ?? item?.SubjectDisplayLabel ??
        item?.subjectName ?? item?.subject_name ?? item?.SubjectName ?? null,
      topicIds: item?.topicIds ?? item?.topic_ids ?? item?.TopicIds ?? [],
      topicNames: item?.topicNames ?? item?.topic_names ?? item?.TopicNames ?? [],
      topicName: item?.topicName ?? item?.topic_name ?? item?.TopicName ?? null,
      topicDisplayLabel:
        item?.topicDisplayLabel ?? item?.topic_display_label ?? item?.TopicDisplayLabel ??
        item?.topicName ?? item?.topic_name ?? item?.TopicName ?? null,
      assessmentStatus: item?.assessmentStatus ?? item?.assessment_status ?? item?.AssessmentStatus ?? '',
      assessmentDate: item?.assessmentDate ?? item?.assessment_date ?? item?.AssessmentDate ?? null,
      startDateTime: item?.startDateTime ?? item?.start_datetime ?? item?.StartDateTime ?? null,
      totalMarks: item?.totalMarks ?? item?.total_marks ?? item?.TotalMarks ?? 0,
      difficultyLevel: item?.difficultyLevel ?? item?.difficulty_level ?? item?.DifficultyLevel ?? 0
    };
  }

  private mapQuestionClassificationEntry(item: any): QuestionClassificationEntry {
    return {
      subjectId: item?.subjectId ?? item?.subject_id ?? item?.SubjectId ?? '',
      subjectName: item?.subjectName ?? item?.subject_name ?? item?.SubjectName ?? '',
      topicId: item?.topicId ?? item?.topic_id ?? item?.TopicId ?? null,
      topicName: item?.topicName ?? item?.topic_name ?? item?.TopicName ?? null
    };
  }
}
