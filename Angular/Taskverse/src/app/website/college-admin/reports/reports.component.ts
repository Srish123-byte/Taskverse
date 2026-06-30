import { Component, OnInit } from '@angular/core';
import { Session } from '../../../common/services/session/session.service';
import { AppConfig } from '../../../app.config';
import {
  ReportsService,
  ReportFilters,
  BranchWiseReport,
  FilterOption
} from '../../../common/services/api/reports.service';

@Component({
  selector: 'app-college-admin-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  collegeId = '';
  classId = '';
  batchId = '';
  studentId = '';

  report: BranchWiseReport | null = null;
  branches: FilterOption[] = [];
  batches: FilterOption[] = [];
  loading = false;
  error = '';

  selectedBranchId = '';
  selectedBatchId = '';
  dateFrom = '';
  dateTo = '';

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
    const filters: ReportFilters = { collegeId: this.collegeId };
    if (this.selectedBranchId) filters.classId = this.selectedBranchId;
    if (this.selectedBatchId) filters.batchId = this.selectedBatchId;
    if (this.dateFrom) filters.dateFrom = this.dateFrom;
    if (this.dateTo) filters.dateTo = this.dateTo;

    this.reportsService.getBranchWiseReport(filters).subscribe({
      next: (data) => { this.report = data; this.loading = false; },
      error: (err) => { this.error = 'Failed to load report.'; this.loading = false; }
    });
  }

  applyFilters(): void { this.loadReport(); }
  clearFilters(): void {
    this.selectedBranchId = '';
    this.selectedBatchId = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.loadReport();
  }

  exportPdf(): void {
    this.reportsService.exportBranchWisePdf(this.buildFilters());
  }
  exportExcel(): void {
    this.reportsService.exportBranchWiseExcel(this.buildFilters());
  }

  private buildFilters(): ReportFilters {
    const filters: ReportFilters = { collegeId: this.collegeId };
    if (this.selectedBranchId) filters.classId = this.selectedBranchId;
    if (this.selectedBatchId) filters.batchId = this.selectedBatchId;
    if (this.dateFrom) filters.dateFrom = this.dateFrom;
    if (this.dateTo) filters.dateTo = this.dateTo;
    return filters;
  }
}
