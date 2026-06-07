import { formatDate } from '@angular/common';
import { ChangeDetectorRef, Component, HostBinding, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AssessmentAdminService,
  AssessmentManagementItem,
  AssessmentManagementSearchRequest
} from '../../services/api/assessment-admin.service';

type AssessmentStatusFilter = 'all' | 'DRAFT' | 'SCHEDULED' | 'LIVE' | 'COMPLETED' | 'CANCELLED';
type AssessmentDifficultyFilter = 'all' | 'Easy' | 'Medium' | 'Hard';

interface FilterOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-assessments-management',
  standalone: false,
  templateUrl: './assessments-management.component.html',
  styleUrl: './assessments-management.component.scss'
})
export class AssessmentsManagementComponent implements OnInit {
  private static readonly snackBarConfig = {
    duration: 3500,
    horizontalPosition: 'center' as const,
    verticalPosition: 'top' as const,
    panelClass: ['question-editor-success-snackbar']
  };

  @Input() heroKicker = 'Admin Console';
  @Input() pageTitle = 'Assessments Management';
  @Input() pageWelcome = 'Configure, monitor, and deploy high-stakes technical assessments.';
  @Input() theme: 'college-admin' | 'trainer' = 'college-admin';
  @Input() createAssessmentRoute = '';
  @Input() editAssessmentRouteBase = '';

  readonly pageSize = 3;
  readonly statusOptions: FilterOption[] = [
    { value: 'all', label: 'All' },
    { value: 'DRAFT', label: 'Draft' },
    { value: 'SCHEDULED', label: 'Scheduled' },
    { value: 'LIVE', label: 'Live' },
    { value: 'COMPLETED', label: 'Completed' },
    { value: 'CANCELLED', label: 'Cancelled' }
  ];
  readonly difficultyOptions: FilterOption[] = [
    { value: 'all', label: 'All' },
    { value: 'Easy', label: 'Easy' },
    { value: 'Medium', label: 'Medium' },
    { value: 'Hard', label: 'Hard' }
  ];

  searchTerm = '';
  selectedStatus: AssessmentStatusFilter = 'all';
  selectedDifficulty: AssessmentDifficultyFilter = 'all';
  currentPage = 1;

  assessments: AssessmentManagementItem[] = [];
  totalCount = 0;
  activeCount = 0;
  completedCount = 0;
  isLoading = false;
  errorMessage = '';

  @HostBinding('class.theme-trainer')
  get isTrainerTheme(): boolean {
    return this.theme === 'trainer';
  }

  @HostBinding('class.theme-college-admin')
  get isCollegeAdminTheme(): boolean {
    return this.theme === 'college-admin';
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get pageStart(): number {
    return this.totalCount === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return this.totalCount === 0 ? 0 : Math.min(this.pageStart + this.assessments.length - 1, this.totalCount);
  }

  get createAssessmentRouteSegments(): string[] {
    return this.createAssessmentRoute.split('/').filter(segment => segment.length > 0);
  }

  constructor(
    private readonly snackBar: MatSnackBar,
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAssessments();
  }

  trackByAssessmentId(_: number, assessment: AssessmentManagementItem): string {
    return assessment.assessmentId;
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadAssessments();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStatus = 'all';
    this.selectedDifficulty = 'all';
    this.currentPage = 1;
    this.loadAssessments();
  }

  prevPage(): void {
    if (this.currentPage <= 1 || this.isLoading) {
      return;
    }

    this.currentPage -= 1;
    this.loadAssessments();
  }

  nextPage(): void {
    if (this.currentPage >= this.totalPages || this.isLoading) {
      return;
    }

    this.currentPage += 1;
    this.loadAssessments();
  }

  editAssessment(assessment: AssessmentManagementItem): void {
    if (!this.isEditAllowed(assessment)) {
      this.openSnackBar(this.getEditRestrictionMessage(assessment));
      return;
    }

    if (!this.editAssessmentRouteBase) {
      return;
    }

    void this.router.navigateByUrl(`/${this.editAssessmentRouteBase}/${assessment.assessmentId}`);
  }

  deleteAssessment(): void {
    this.openSnackBar('Delete assessment is not wired yet.');
  }

  getStatusClass(status: string): string {
    return `status-${this.normalizeStatus(status).toLowerCase().replace(/\s+/g, '-')}`;
  }

  getStatusLabel(status: string): string {
    return this.normalizeStatus(status);
  }

  formatAssessmentDate(value?: string | null): string {
    if (!value) {
      return '--';
    }

    return formatDate(value, 'MMM dd, yyyy', 'en-US');
  }

  getDifficultyLabel(difficultyLevel: number): string {
    switch (difficultyLevel) {
      case 1:
        return 'Easy';
      case 2:
        return 'Medium';
      case 3:
        return 'Hard';
      default:
        return 'Unspecified';
    }
  }

  getCategoryLabel(assessment: AssessmentManagementItem): string {
    return assessment.subjectName?.trim() || assessment.topicName?.trim() || 'Uncategorized';
  }

  isEditAllowed(assessment: AssessmentManagementItem): boolean {
    const normalizedStatus = this.normalizeStatus(assessment.assessmentStatus);
    return normalizedStatus === 'DRAFT' || normalizedStatus === 'SCHEDULED' || normalizedStatus === 'CANCELLED';
  }

  getEditRestrictionMessage(assessment: AssessmentManagementItem): string {
    const normalizedStatus = this.normalizeStatus(assessment.assessmentStatus);

    if (normalizedStatus === 'LIVE') {
      return "Edit isn't allowed once the assessment is Live";
    }

    if (normalizedStatus === 'COMPLETED') {
      return "Edit isn't allowed once the assessment is Completed";
    }

    return `Edit isn't allowed once the assessment is ${normalizedStatus}`;
  }

  private loadAssessments(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.assessmentAdminService.searchAssessments(this.buildSearchRequest()).subscribe({
      next: result => {
        this.assessments = result.items ?? [];
        this.totalCount = result.totalCount ?? 0;
        this.activeCount = result.activeCount ?? 0;
        this.completedCount = result.completedCount ?? 0;
        this.currentPage = result.pageNumber > 0 ? result.pageNumber : this.currentPage;
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();

        const adjustedTotalPages = this.totalPages;
        if (this.totalCount > 0 && this.currentPage > adjustedTotalPages) {
          this.currentPage = adjustedTotalPages;
          this.loadAssessments();
        }
      },
      error: error => {
        this.assessments = [];
        this.totalCount = 0;
        this.activeCount = 0;
        this.completedCount = 0;
        this.isLoading = false;
        this.errorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          'Unable to load assessments right now.';
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private buildSearchRequest(): AssessmentManagementSearchRequest {
    return {
      searchTerm: this.searchTerm.trim() || undefined,
      assessmentStatus: this.selectedStatus === 'all' ? undefined : this.selectedStatus,
      difficultyLevel: this.mapDifficultyFilter(this.selectedDifficulty),
      pageNumber: this.currentPage,
      pageSize: this.pageSize
    };
  }

  private mapDifficultyFilter(value: AssessmentDifficultyFilter): number | undefined {
    switch (value) {
      case 'Easy':
        return 1;
      case 'Medium':
        return 2;
      case 'Hard':
        return 3;
      default:
        return undefined;
    }
  }

  private normalizeStatus(status: string): string {
    return status.trim().replace(/_/g, ' ').toUpperCase();
  }

  private openSnackBar(message: string): void {
    this.snackBar.open(message, 'Close', AssessmentsManagementComponent.snackBarConfig);
  }
}
