import { Component, OnInit } from '@angular/core';
import { Session } from '../../../common/services/session/session.service';
import { AppConfig } from '../../../app.config';
import {
  ReportsService,
  ReportFilters,
  StudentPerformanceReport,
  StudentPerformanceRow,
  FilterOption
} from '../../../common/services/api/reports.service';

@Component({
  selector: 'app-trainer-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  // Legacy fields
  collegeId = '';
  classId = '';
  batchId = '';
  studentId = '';

  // New Fields
  report: StudentPerformanceReport | null = null;
  branches: FilterOption[] = [];
  batches: FilterOption[] = [];
  loading = false;
  error = '';

  // Filters
  selectedBranchId = '';
  selectedBatchId = '';
  selectedStudentId = '';
  selectedPerformanceLevel = '';
  dateFrom = '';
  dateTo = '';

  // Selected student for detailed AI Insights modal/view
  activeStudent: StudentPerformanceRow | null = null;

  constructor(
    private readonly session: Session,
    private readonly appConfig: AppConfig,
    private readonly reportsService: ReportsService
  ) {}

  ngOnInit(): void {
    this.collegeId = this.session.collegeId || '';
    this.loadFilters();
    this.loadReport();
  }

  loadFilters(): void {
    this.reportsService.getBranches(this.collegeId).subscribe({
      next: (data) => this.branches = data,
      error: () => this.branches = []
    });
    this.reportsService.getBatches().subscribe({
      next: (data) => this.batches = data,
      error: () => this.batches = []
    });
  }

  loadReport(): void {
    this.loading = true;
    this.error = '';
    const filters = this.buildFilters();

    this.reportsService.getStudentPerformanceReport(filters).subscribe({
      next: (data) => {
        this.report = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load student performance report.';
        this.loading = false;
        console.error(err);
      }
    });
  }

  applyFilters(): void {
    this.loadReport();
  }

  clearFilters(): void {
    this.selectedBranchId = '';
    this.selectedBatchId = '';
    this.selectedStudentId = '';
    this.selectedPerformanceLevel = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.loadReport();
  }

  viewInsights(student: StudentPerformanceRow): void {
    this.activeStudent = student;
  }

  closeInsights(): void {
    this.activeStudent = null;
  }

  exportPdf(): void {
    this.reportsService.exportStudentPerformancePdf(this.buildFilters());
  }

  exportExcel(): void {
    this.reportsService.exportStudentPerformanceExcel(this.buildFilters());
  }

  getGradeClass(pct: number): string {
    if (pct >= 85) return 'grade-excellent';
    if (pct >= 70) return 'grade-good';
    if (pct >= 50) return 'grade-average';
    return 'grade-poor';
  }

  getReadinessClass(readiness: string): string {
    switch (readiness) {
      case 'Excellent': case 'Ready': return 'readiness-ready';
      case 'Good': case 'Partially Ready': return 'readiness-partial';
      default: return 'readiness-low';
    }
  }

  private buildFilters(): ReportFilters {
    const filters: ReportFilters = { collegeId: this.collegeId };
    if (this.selectedBranchId) filters.classId = this.selectedBranchId;
    if (this.selectedBatchId) filters.batchId = this.selectedBatchId;
    if (this.selectedStudentId) filters.studentId = this.selectedStudentId;
    if (this.selectedPerformanceLevel) filters.performanceLevel = this.selectedPerformanceLevel;
    if (this.dateFrom) filters.dateFrom = this.dateFrom;
    if (this.dateTo) filters.dateTo = this.dateTo;
    return filters;
  }
}
