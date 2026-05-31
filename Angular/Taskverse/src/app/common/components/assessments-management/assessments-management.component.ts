import { formatDate } from '@angular/common';
import { ChangeDetectorRef, Component, HostBinding, Input, OnDestroy, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, takeUntil } from 'rxjs';
import { RoleType } from '../../enums/role-type.enum';
import {
  AssessmentAdminService,
  AssessmentManagementItem,
  AssessmentManagementSearchResult
} from '../../services/api/assessment-admin.service';
import { AccountService } from '../../services/api/account.service';
import { Session } from '../../services/session/session.service';

type AssessmentStatusFilter = 'all' | 'LIVE' | 'UPCOMING' | 'COMPLETED' | 'DRAFT';
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
export class AssessmentsManagementComponent implements OnInit, OnDestroy {
  @Input() heroKicker = 'Admin Console';
  @Input() pageTitle = 'Assessments Management';
  @Input() pageWelcome = 'Configure, monitor, and deploy high-stakes technical assessments.';
  @Input() theme: 'college-admin' | 'trainer' = 'college-admin';
  @Input() createAssessmentRoute = '';

  readonly pageSize = 3;
  readonly statusOptions: FilterOption[] = [
    { value: 'all', label: 'Status: All' },
    { value: 'LIVE', label: 'Status: Live' },
    { value: 'UPCOMING', label: 'Status: Upcoming' },
    { value: 'COMPLETED', label: 'Status: Completed' },
    { value: 'DRAFT', label: 'Status: Draft' }
  ];
  readonly difficultyOptions: FilterOption[] = [
    { value: 'all', label: 'Difficulty: All' },
    { value: 'Easy', label: 'Difficulty: Easy' },
    { value: 'Medium', label: 'Difficulty: Medium' },
    { value: 'Hard', label: 'Difficulty: Hard' }
  ];

  searchTerm = '';
  selectedStatus: AssessmentStatusFilter = 'all';
  selectedDifficulty: AssessmentDifficultyFilter = 'all';
  currentPage = 1;
  isLoading = false;
  errorMessage = '';

  assessments: AssessmentManagementItem[] = [];
  totalCount = 0;
  activeCount = 0;
  completedCount = 0;

  private readonly destroy$ = new Subject<void>();
  private hasLoadedInitialAssessments = false;
  private isBootstrappingContext = false;

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
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly accountService: AccountService,
    private readonly snackBar: MatSnackBar,
    private readonly session: Session,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (this.hasAssessmentAccessContext()) {
      this.loadAssessments();
      return;
    }

    this.bootstrapAssessmentAccessContext();

    this.session.user$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        if (!this.hasLoadedInitialAssessments && this.hasAssessmentAccessContext()) {
          this.loadAssessments();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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
    this.openSnackBar(`Edit flow for "${assessment.assessmentName}" will be connected next.`);
  }

  deleteAssessment(assessment: AssessmentManagementItem): void {
    this.openSnackBar(`Delete flow for "${assessment.assessmentName}" will be connected next.`);
  }

  getStatusClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }

  formatAssessmentDate(value: string): string {
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

  private loadAssessments(): void {
    this.hasLoadedInitialAssessments = true;
    this.isLoading = true;
    this.errorMessage = '';

    this.assessmentAdminService.searchAssessments({
      searchTerm: this.searchTerm.trim() || undefined,
      assessmentStatus: this.selectedStatus === 'all' ? undefined : this.selectedStatus,
      difficultyLevel: this.mapDifficultyFilter(this.selectedDifficulty),
      pageNumber: this.currentPage,
      pageSize: this.pageSize
    }).subscribe({
      next: result => this.applyAssessmentResult(result),
      error: error => {
        this.assessments = [];
        this.totalCount = 0;
        this.activeCount = 0;
        this.completedCount = 0;
        this.errorMessage = error?.error?.message ?? 'Unable to load assessments right now.';
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private applyAssessmentResult(result: AssessmentManagementSearchResult): void {
    this.assessments = result.items ?? [];
    this.totalCount = result.totalCount ?? 0;
    this.activeCount = result.activeCount ?? 0;
    this.completedCount = result.completedCount ?? 0;
    this.currentPage = result.pageNumber > 0 ? result.pageNumber : 1;
    this.isLoading = false;
    this.changeDetectorRef.detectChanges();
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

  private openSnackBar(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3500,
      horizontalPosition: 'center',
      verticalPosition: 'top',
      panelClass: ['question-editor-success-snackbar']
    });
  }

  private hasAssessmentAccessContext(): boolean {
    return !!this.session.role && !!this.session.collegeId;
  }

  private bootstrapAssessmentAccessContext(): void {
    if (this.isBootstrappingContext || !this.shouldFetchUserProfileForAccessContext()) {
      return;
    }

    this.isBootstrappingContext = true;

    this.accountService.getUserProfile().subscribe({
      next: user => {
        this.session.user = user;
        this.session.userEmail = user.email;
        this.session.userId = user.userId;
        this.session.role = user.role;

        if (!this.hasLoadedInitialAssessments && this.hasAssessmentAccessContext()) {
          this.loadAssessments();
        }
      },
      error: () => {
        this.isBootstrappingContext = false;
        this.errorMessage = 'Unable to prepare assessment access right now.';
        this.changeDetectorRef.detectChanges();
      },
      complete: () => {
        this.isBootstrappingContext = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private shouldFetchUserProfileForAccessContext(): boolean {
    return !!this.session.jwtToken &&
      (this.session.role === RoleType.CollegeAdmin || this.session.role === RoleType.Trainer) &&
      !this.session.collegeId;
  }
}
