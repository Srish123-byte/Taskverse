import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import {
  StudentAssessmentDetail,
  StudentAssessmentItem,
  StudentAssessmentsService
} from '../../../common/services/api/student-assessments.service';

type AssessmentTab = 'active' | 'past';

@Component({
  selector: 'app-student-my-assessments',
  standalone: false,
  templateUrl: './my-assessments.component.html',
  styleUrl: './my-assessments.component.scss'
})
export class MyAssessmentsComponent implements OnInit, OnDestroy {
  readonly tabs: { key: AssessmentTab; label: string; statuses: string[] }[] = [
    { key: 'active', label: 'Active/Upcoming', statuses: ['LIVE', 'SCHEDULED'] },
    { key: 'past', label: 'Past Assessments', statuses: ['COMPLETED'] }
  ];

  activeTab: AssessmentTab = 'active';
  assessments: StudentAssessmentItem[] = [];
  isLoading = false;
  errorMessage = '';
  selectedAssessmentDetail: StudentAssessmentDetail | null = null;
  selectedAssessmentActionLabel = '';
  selectedAssessmentName = '';
  isDetailModalOpen = false;
  loadingAssessmentId: string | null = null;
  private readonly subscriptions = new Subscription();
  private assessmentsLoadSubscription?: Subscription;
  private assessmentDetailSubscription?: Subscription;

  constructor(
    private readonly studentAssessmentsService: StudentAssessmentsService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAssessments();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  selectTab(tab: AssessmentTab): void {
    if (this.activeTab === tab || this.isLoading) {
      return;
    }

    this.activeTab = tab;
    this.loadAssessments();
  }

  trackByAssessmentId(_: number, assessment: StudentAssessmentItem): string {
    return assessment.assessmentId;
  }

  getDifficultyLabel(level: number): string {
    if (level >= 4) {
      return 'Hard';
    }

    if (level === 3) {
      return 'Medium';
    }

    return 'Easy';
  }

  getStatusLabel(status: string): string {
    switch (status?.toUpperCase()) {
      case 'LIVE':
        return 'Live Now';
      case 'SCHEDULED':
        return 'Upcoming';
      case 'COMPLETED':
        return 'Completed';
      default:
        return status;
    }
  }

  getEmptyStateMessage(): string {
    return this.activeTab === 'past'
      ? 'You have not completed any assessments yet. Your finished assessments will appear here.'
      : 'No active or upcoming assessments right now. New assessments assigned to you will appear here.';
  }

  getAssessmentContext(assessment: StudentAssessmentItem): string {
    const parts = [assessment.subjectName, assessment.topicName]
      .map(value => value?.trim())
      .filter((value): value is string => !!value);

    return parts.join(' • ');
  }

  getActionLabel(status: string): string {
    return status?.toUpperCase() === 'LIVE' ? 'Start Assessment' : 'View Details';
  }

  isPrimaryAction(status: string): boolean {
    return status?.toUpperCase() === 'LIVE';
  }

  openAssessmentAction(assessment: StudentAssessmentItem): void {
    this.assessmentDetailSubscription?.unsubscribe();
    this.loadingAssessmentId = assessment.assessmentId;
    this.errorMessage = '';

    this.assessmentDetailSubscription = this.studentAssessmentsService
      .getAssessmentDetail(assessment.assessmentId)
      .pipe(finalize(() => {
        this.loadingAssessmentId = null;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: detail => {
          this.selectedAssessmentDetail = detail;
          this.selectedAssessmentActionLabel = this.getActionLabel(assessment.assessmentStatus);
          this.selectedAssessmentName = assessment.assessmentName;
          this.isDetailModalOpen = true;
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          console.error('Failed to load student assessment detail.', error);
          this.errorMessage = error?.error?.message || 'Unable to load assessment details right now.';
          this.changeDetectorRef.detectChanges();
        }
      });

    this.subscriptions.add(this.assessmentDetailSubscription);
  }

  closeAssessmentDetailModal(): void {
    this.isDetailModalOpen = false;
    this.selectedAssessmentDetail = null;
    this.selectedAssessmentActionLabel = '';
    this.selectedAssessmentName = '';
  }

  private loadAssessments(): void {
    const selectedTab = this.tabs.find(tab => tab.key === this.activeTab);
    if (!selectedTab) {
      return;
    }

    this.assessmentsLoadSubscription?.unsubscribe();

    this.isLoading = true;
    this.errorMessage = '';

    this.assessmentsLoadSubscription = this.studentAssessmentsService
      .getAssessments(selectedTab.statuses)
      .pipe(finalize(() => {
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: assessments => {
          this.assessments = assessments ?? [];
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          console.error('Failed to load student assessments.', error);
          this.assessments = [];
          this.errorMessage = error?.error?.message || 'Unable to load assessments right now.';
          this.changeDetectorRef.detectChanges();
        }
      });

    this.subscriptions.add(this.assessmentsLoadSubscription);
  }
}
