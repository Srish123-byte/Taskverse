import { ChangeDetectorRef, Component, HostBinding, Input, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

type AssessmentStatus = 'LIVE' | 'UPCOMING' | 'COMPLETED' | 'DRAFT';
type AssessmentDifficulty = 'Easy' | 'Medium' | 'Hard';

interface AssessmentManagementItem {
  assessmentId: string;
  assessmentCode: string;
  assessmentName: string;
  category: string;
  status: AssessmentStatus;
  scheduledDate: string;
  totalMarks: number;
  difficulty: AssessmentDifficulty;
}

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
  selectedStatus = 'all';
  selectedDifficulty = 'all';
  currentPage = 1;

  assessments: AssessmentManagementItem[] = [];
  filteredAssessments: AssessmentManagementItem[] = [];
  visibleAssessments: AssessmentManagementItem[] = [];

  @HostBinding('class.theme-trainer')
  get isTrainerTheme(): boolean {
    return this.theme === 'trainer';
  }

  @HostBinding('class.theme-college-admin')
  get isCollegeAdminTheme(): boolean {
    return this.theme === 'college-admin';
  }

  constructor(
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.assessments = this.buildSeedAssessments();
    this.applyFilters();
  }

  get totalCount(): number {
    return this.assessments.length;
  }

  get activeCount(): number {
    return this.assessments.filter(item => item.status === 'LIVE' || item.status === 'UPCOMING').length;
  }

  get completedCount(): number {
    return this.assessments.filter(item => item.status === 'COMPLETED').length;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredAssessments.length / this.pageSize));
  }

  get pageStart(): number {
    return this.filteredAssessments.length === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.filteredAssessments.length);
  }

  get createAssessmentRouteSegments(): string[] {
    return this.createAssessmentRoute.split('/').filter(segment => segment.length > 0);
  }

  trackByAssessmentId(_: number, assessment: AssessmentManagementItem): string {
    return assessment.assessmentId;
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStatus = 'all';
    this.selectedDifficulty = 'all';
    this.currentPage = 1;
    this.applyFilters();
  }

  prevPage(): void {
    if (this.currentPage <= 1) {
      return;
    }

    this.currentPage -= 1;
    this.updateVisibleAssessments();
  }

  nextPage(): void {
    if (this.currentPage >= this.totalPages) {
      return;
    }

    this.currentPage += 1;
    this.updateVisibleAssessments();
  }

  editAssessment(assessment: AssessmentManagementItem): void {
    this.openSnackBar(`Edit flow for "${assessment.assessmentName}" will be connected next.`);
  }

  deleteAssessment(assessment: AssessmentManagementItem): void {
    this.assessments = this.assessments.filter(item => item.assessmentId !== assessment.assessmentId);

    if (this.currentPage > 1 && this.pageStart > this.assessments.length) {
      this.currentPage -= 1;
    }

    this.applyFilters();
    this.openSnackBar(`"${assessment.assessmentName}" was removed from the mock management list.`);
  }

  getStatusClass(status: AssessmentStatus): string {
    return `status-${status.toLowerCase()}`;
  }

  private applyFilters(): void {
    const normalizedSearch = this.searchTerm.trim().toLowerCase();

    this.filteredAssessments = this.assessments.filter(assessment => {
      const matchesSearch = normalizedSearch.length === 0 ||
        assessment.assessmentName.toLowerCase().includes(normalizedSearch) ||
        assessment.category.toLowerCase().includes(normalizedSearch) ||
        assessment.assessmentCode.toLowerCase().includes(normalizedSearch);

      const matchesStatus = this.selectedStatus === 'all' || assessment.status === this.selectedStatus;
      const matchesDifficulty = this.selectedDifficulty === 'all' || assessment.difficulty === this.selectedDifficulty;

      return matchesSearch && matchesStatus && matchesDifficulty;
    });

    this.currentPage = Math.min(this.currentPage, this.totalPages);
    this.updateVisibleAssessments();
  }

  private updateVisibleAssessments(): void {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    this.visibleAssessments = this.filteredAssessments.slice(startIndex, startIndex + this.pageSize);
    this.changeDetectorRef.detectChanges();
  }

  private buildSeedAssessments(): AssessmentManagementItem[] {
    if (this.theme === 'trainer') {
      return [
        {
          assessmentId: 'trainer-1',
          assessmentCode: 'TR-AS-2026-01',
          assessmentName: 'Frontend Systems Drill',
          category: 'Engineering',
          status: 'LIVE',
          scheduledDate: 'Jun 04, 2026',
          totalMarks: 90,
          difficulty: 'Medium'
        },
        {
          assessmentId: 'trainer-2',
          assessmentCode: 'TR-AS-2026-02',
          assessmentName: 'Angular Patterns Sprint',
          category: 'Engineering',
          status: 'UPCOMING',
          scheduledDate: 'Jun 12, 2026',
          totalMarks: 75,
          difficulty: 'Medium'
        },
        {
          assessmentId: 'trainer-3',
          assessmentCode: 'TR-AS-2026-03',
          assessmentName: 'Node Services Capstone',
          category: 'Platform',
          status: 'COMPLETED',
          scheduledDate: 'May 16, 2026',
          totalMarks: 120,
          difficulty: 'Hard'
        },
        {
          assessmentId: 'trainer-4',
          assessmentCode: 'TR-AS-2026-04',
          assessmentName: 'Data Modelling Checkpoint',
          category: 'Databases',
          status: 'LIVE',
          scheduledDate: 'Jun 08, 2026',
          totalMarks: 80,
          difficulty: 'Easy'
        },
        {
          assessmentId: 'trainer-5',
          assessmentCode: 'TR-AS-2026-05',
          assessmentName: 'TypeScript Fluency Lab',
          category: 'Programming',
          status: 'DRAFT',
          scheduledDate: 'Jun 18, 2026',
          totalMarks: 60,
          difficulty: 'Easy'
        }
      ];
    }

    return [
      {
        assessmentId: 'admin-1',
        assessmentCode: 'CA-AS-2026-01',
        assessmentName: 'Python Basics Midterm',
        category: 'Engineering',
        status: 'LIVE',
        scheduledDate: 'Jun 02, 2026',
        totalMarks: 100,
        difficulty: 'Easy'
      },
      {
        assessmentId: 'admin-2',
        assessmentCode: 'CA-AS-2026-02',
        assessmentName: 'Data Structures Final',
        category: 'Computer Science',
        status: 'UPCOMING',
        scheduledDate: 'Jun 10, 2026',
        totalMarks: 150,
        difficulty: 'Hard'
      },
      {
        assessmentId: 'admin-3',
        assessmentCode: 'CA-AS-2026-03',
        assessmentName: 'Algorithms Quiz 1',
        category: 'Engineering',
        status: 'COMPLETED',
        scheduledDate: 'May 19, 2026',
        totalMarks: 50,
        difficulty: 'Medium'
      },
      {
        assessmentId: 'admin-4',
        assessmentCode: 'CA-AS-2026-04',
        assessmentName: 'Cloud Readiness Check',
        category: 'Infrastructure',
        status: 'LIVE',
        scheduledDate: 'Jun 06, 2026',
        totalMarks: 80,
        difficulty: 'Medium'
      },
      {
        assessmentId: 'admin-5',
        assessmentCode: 'CA-AS-2026-05',
        assessmentName: 'Database Reliability Mock',
        category: 'Databases',
        status: 'UPCOMING',
        scheduledDate: 'Jun 14, 2026',
        totalMarks: 120,
        difficulty: 'Hard'
      },
      {
        assessmentId: 'admin-6',
        assessmentCode: 'CA-AS-2026-06',
        assessmentName: 'Java Foundations Screening',
        category: 'Programming',
        status: 'DRAFT',
        scheduledDate: 'Jun 20, 2026',
        totalMarks: 70,
        difficulty: 'Easy'
      }
    ];
  }

  private openSnackBar(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3500,
      horizontalPosition: 'center',
      verticalPosition: 'top',
      panelClass: ['question-editor-success-snackbar']
    });
  }
}
