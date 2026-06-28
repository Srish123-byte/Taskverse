import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConfig } from '../../../app.config';

export interface ReportFilters {
  collegeId?: string;
  classId?: string;
  batchId?: string;
  studentId?: string;
  trainerId?: string;
  assessmentId?: string;
  dateFrom?: string;
  dateTo?: string;
  academicYear?: string;
  semester?: string;
  performanceLevel?: string;
}

export interface ReportMetadata {
  reportTitle: string;
  generatedDate: string;
  generatedTime: string;
  generatedBy: string;
  appliedFilters: { [key: string]: string };
  collegeName?: string;
  academicYear?: string;
}

export interface CollegeWiseSummary {
  totalColleges: number;
  totalStudents: number;
  totalTrainers: number;
  totalAssessments: number;
  averageScore: number;
  overallPassPercentage: number;
}

export interface CollegeWiseRow {
  collegeName: string;
  totalStudents: number;
  totalTrainers: number;
  totalAssessments: number;
  assessmentsCompleted: number;
  averageScore: number;
  highestScore: number;
  lowestScore: number;
  passPercentage: number;
  activeStudents: number;
  performanceGrade: string;
}

export interface CollegeWiseReport {
  metadata: ReportMetadata;
  summary: CollegeWiseSummary;
  rows: CollegeWiseRow[];
}

export interface BranchWiseSummary {
  totalBranches: number;
  totalStudents: number;
  totalTrainers: number;
  totalAssessments: number;
  averageMarks: number;
  overallPassPercentage: number;
}

export interface BranchWiseRow {
  branchName: string;
  totalStudents: number;
  totalTrainers: number;
  totalAssessments: number;
  averageMarks: number;
  highestMarks: number;
  lowestMarks: number;
  passPercentage: number;
  strongestTopics: string[];
  weakestTopics: string[];
}

export interface BranchWiseReport {
  metadata: ReportMetadata;
  summary: BranchWiseSummary;
  rows: BranchWiseRow[];
}

export interface StudentAssessmentBreakdown {
  assessmentName: string;
  assessmentType: string;
  obtainedMarks: number;
  totalMarks: number;
  percentage: number;
  rank: number;
  status: string;
  date: string;
}

export interface StudentAiInsights {
  learningGaps: string[];
  rootCauseAnalysis: string[];
  weakTopics: string[];
  strongTopics: string[];
  communicationGaps: string[];
  interviewReadiness: string;
  recommendedPracticeAreas: string[];
  suggestedResources: string[];
  priorityLevel: string;
  improvementPlan: string[];
}

export interface StudentPerformanceRow {
  studentId: string;
  name: string;
  enrollmentNumber: string;
  collegeName: string;
  branchName: string;
  semester: string;
  batchName: string;
  trainerName: string;
  assessments: StudentAssessmentBreakdown[];
  totalMarks: number;
  totalObtained: number;
  overallPercentage: number;
  overallRank: number;
  collegeRank: number;
  batchRank: number;
  assessmentCompletionRate: number;
  placementReadiness: string;
  performanceTrend: string;
  aiInsights: StudentAiInsights;
}

export interface StudentPerformanceSummary {
  totalStudents: number;
  averagePercentage: number;
  passPercentage: number;
  highestPercentage: number;
  lowestPercentage: number;
  placementReadyCount: number;
}

export interface StudentPerformanceReport {
  metadata: ReportMetadata;
  summary: StudentPerformanceSummary;
  rows: StudentPerformanceRow[];
}

export interface FilterOption {
  id: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class ReportsService {
  constructor(
    private readonly http: HttpClient,
    private readonly appConfig: AppConfig
  ) {}

  private get apiBase(): string {
    return (this.appConfig.api_url || '').trim().replace(/\/+$/, '');
  }

  private buildParams(filters: ReportFilters): HttpParams {
    let params = new HttpParams();
    if (filters.collegeId) params = params.set('collegeId', filters.collegeId);
    if (filters.classId) params = params.set('classId', filters.classId);
    if (filters.batchId) params = params.set('batchId', filters.batchId);
    if (filters.studentId) params = params.set('studentId', filters.studentId);
    if (filters.trainerId) params = params.set('trainerId', filters.trainerId);
    if (filters.assessmentId) params = params.set('assessmentId', filters.assessmentId);
    if (filters.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters.dateTo) params = params.set('dateTo', filters.dateTo);
    if (filters.academicYear) params = params.set('academicYear', filters.academicYear);
    if (filters.semester) params = params.set('semester', filters.semester);
    if (filters.performanceLevel) params = params.set('performanceLevel', filters.performanceLevel);
    return params;
  }

  private buildQueryString(filters: ReportFilters): string {
    const params = this.buildParams(filters);
    const str = params.toString();
    return str ? `?${str}` : '';
  }

  // ── Super Admin: College Wise ──
  getCollegeWiseReport(filters: ReportFilters = {}): Observable<CollegeWiseReport> {
    return this.http.get<CollegeWiseReport>(
      `${this.apiBase}/reports/super-admin/college-wise`,
      { params: this.buildParams(filters) }
    );
  }

  exportCollegeWisePdf(filters: ReportFilters = {}): void {
    window.open(`${this.apiBase}/reports/super-admin/college-wise/export/pdf${this.buildQueryString(filters)}`, '_blank');
  }

  exportCollegeWiseExcel(filters: ReportFilters = {}): void {
    window.open(`${this.apiBase}/reports/super-admin/college-wise/export/excel${this.buildQueryString(filters)}`, '_blank');
  }

  // ── College Admin: Branch Wise ──
  getBranchWiseReport(filters: ReportFilters = {}): Observable<BranchWiseReport> {
    return this.http.get<BranchWiseReport>(
      `${this.apiBase}/reports/college-admin/branch-wise`,
      { params: this.buildParams(filters) }
    );
  }

  exportBranchWisePdf(filters: ReportFilters = {}): void {
    window.open(`${this.apiBase}/reports/college-admin/branch-wise/export/pdf${this.buildQueryString(filters)}`, '_blank');
  }

  exportBranchWiseExcel(filters: ReportFilters = {}): void {
    window.open(`${this.apiBase}/reports/college-admin/branch-wise/export/excel${this.buildQueryString(filters)}`, '_blank');
  }

  // ── Trainer: Student Performance ──
  getStudentPerformanceReport(filters: ReportFilters = {}): Observable<StudentPerformanceReport> {
    return this.http.get<StudentPerformanceReport>(
      `${this.apiBase}/reports/trainer/student-performance`,
      { params: this.buildParams(filters) }
    );
  }

  exportStudentPerformancePdf(filters: ReportFilters = {}): void {
    window.open(`${this.apiBase}/reports/trainer/student-performance/export/pdf${this.buildQueryString(filters)}`, '_blank');
  }

  exportStudentPerformanceExcel(filters: ReportFilters = {}): void {
    window.open(`${this.apiBase}/reports/trainer/student-performance/export/excel${this.buildQueryString(filters)}`, '_blank');
  }

  // ── Filter Options ──
  getColleges(): Observable<FilterOption[]> {
    return this.http.get<FilterOption[]>(`${this.apiBase}/reports/filters/colleges`);
  }

  getBranches(collegeId?: string): Observable<FilterOption[]> {
    let params = new HttpParams();
    if (collegeId) params = params.set('collegeId', collegeId);
    return this.http.get<FilterOption[]>(`${this.apiBase}/reports/filters/branches`, { params });
  }

  getBatches(classId?: string): Observable<FilterOption[]> {
    let params = new HttpParams();
    if (classId) params = params.set('classId', classId);
    return this.http.get<FilterOption[]>(`${this.apiBase}/reports/filters/batches`, { params });
  }

  getTrainers(collegeId?: string): Observable<FilterOption[]> {
    let params = new HttpParams();
    if (collegeId) params = params.set('collegeId', collegeId);
    return this.http.get<FilterOption[]>(`${this.apiBase}/reports/filters/trainers`, { params });
  }
}
