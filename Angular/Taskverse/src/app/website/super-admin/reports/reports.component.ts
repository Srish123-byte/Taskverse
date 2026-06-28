import { Component, OnInit } from '@angular/core';
import { AppConfig } from '../../../app.config';
import {
  ReportsService,
  ReportFilters,
  CollegeWiseReport,
  CollegeWiseRow,
  FilterOption
} from '../../../common/services/api/reports.service';

@Component({
  selector: 'app-super-admin-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  // Legacy filter fields (preserved)
  collegeId = '';
  classId = '';
  batchId = '';
  studentId = '';

  // Enterprise report data
  report: CollegeWiseReport | null = null;
  colleges: FilterOption[] = [];
  loading = false;
  error = '';

  // Filters
  selectedCollegeId = '';
  dateFrom = '';
  dateTo = '';
  academicYear = '';

  constructor(
    private readonly appConfig: AppConfig,
    private readonly reportsService: ReportsService
  ) {}

  ngOnInit(): void {
    this.loadFilters();
    this.loadReport();
  }

  loadFilters(): void {
    this.reportsService.getColleges().subscribe({
      next: (data) => this.colleges = data,
      error: () => this.colleges = []
    });
  }

  loadReport(): void {
    this.loading = true;
    this.error = '';
    const filters: ReportFilters = {};
    if (this.selectedCollegeId) filters.collegeId = this.selectedCollegeId;
    if (this.dateFrom) filters.dateFrom = this.dateFrom;
    if (this.dateTo) filters.dateTo = this.dateTo;
    if (this.academicYear) filters.academicYear = this.academicYear;

    this.reportsService.getCollegeWiseReport(filters).subscribe({
      next: (data) => {
        this.report = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load report data. Please try again.';
        this.loading = false;
        console.error('Report load error:', err);
      }
    });
  }

  applyFilters(): void {
    this.loadReport();
  }

  clearFilters(): void {
    this.selectedCollegeId = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.academicYear = '';
    this.loadReport();
  }

  exportPdf(): void {
    this.reportsService.exportCollegeWisePdf(this.buildFilters());
  }

  exportExcel(): void {
    this.reportsService.exportCollegeWiseExcel(this.buildFilters());
  }

  // Legacy export (preserved)
  exportLegacyPdf(): void {
    this.legacyExport('pdf');
  }

  exportLegacyExcel(): void {
    this.legacyExport('excel');
  }

  getGradeClass(grade: string): string {
    switch (grade) {
      case 'A+': case 'A': return 'grade-excellent';
      case 'B+': case 'B': return 'grade-good';
      case 'C': return 'grade-average';
      default: return 'grade-poor';
    }
  }

  private buildFilters(): ReportFilters {
    const filters: ReportFilters = {};
    if (this.selectedCollegeId) filters.collegeId = this.selectedCollegeId;
    if (this.dateFrom) filters.dateFrom = this.dateFrom;
    if (this.dateTo) filters.dateTo = this.dateTo;
    if (this.academicYear) filters.academicYear = this.academicYear;
    return filters;
  }

  private legacyExport(format: 'pdf' | 'excel'): void {
    const params = new URLSearchParams();
    if (this.collegeId) params.set('collegeId', this.collegeId);
    if (this.classId) params.set('classId', this.classId);
    if (this.batchId) params.set('batchId', this.batchId);
    if (this.studentId) params.set('studentId', this.studentId);
    const apiBaseUrl = (this.appConfig.api_url || '').trim().replace(/\/+$/, '');
    const reportUrl = `${apiBaseUrl}/reports/export/${format}?${params.toString()}`;
    window.open(reportUrl, '_blank');
  }
}
