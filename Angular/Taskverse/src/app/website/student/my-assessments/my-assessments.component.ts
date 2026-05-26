import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { StudentAssessmentItem, StudentAssessmentsService } from '../../../common/services/api/student-assessments.service';

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
  private readonly subscriptions = new Subscription();

  constructor(private readonly studentAssessmentsService: StudentAssessmentsService) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.studentAssessmentsService.assessmentsCacheReset$.subscribe(() => {
        this.loadAssessments();
      })
    );

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

  private loadAssessments(): void {
    const selectedTab = this.tabs.find(tab => tab.key === this.activeTab);
    if (!selectedTab) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.studentAssessmentsService
      .getAssessments(selectedTab.statuses)
      .pipe(finalize(() => { this.isLoading = false; }))
      .subscribe({
        next: assessments => {
          this.assessments = assessments ?? [];
        },
        error: error => {
          console.error('Failed to load student assessments.', error);
          this.assessments = [];
          this.errorMessage = error?.error?.message || 'Unable to load assessments right now.';
        }
      });
  }
}
