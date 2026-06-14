import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { finalize, map, switchMap } from 'rxjs/operators';
import { RouteAddress } from '../../../common/constants/routes.constants';
import {
  StudentAssessmentDetail,
  StudentAssessmentItem,
  StudentAssessmentsService
} from '../../../common/services/api/student-assessments.service';
import { DeviceInformationService } from '../../../common/services/utilities/device-information.service';

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
  selectedAssessmentId: string | null = null;
  selectedAssessmentStatus = '';
  isDetailModalOpen = false;
  isStartingAssessment = false;
  attemptErrorMessage = '';
  loadingAssessmentId: string | null = null;
  private readonly subscriptions = new Subscription();
  private assessmentsLoadSubscription?: Subscription;
  private assessmentDetailSubscription?: Subscription;
  private attemptStartSubscription?: Subscription;

  constructor(
    private readonly studentAssessmentsService: StudentAssessmentsService,
    private readonly deviceInformationService: DeviceInformationService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly router: Router
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

    return parts.join(' - ');
  }

  getActionLabel(status: string): string {
    if (this.isLiveStatus(status)) {
      return 'Start Assessment';
    }

    return 'View Details';
  }

  isPrimaryAction(status: string): boolean {
    return this.isLiveStatus(status);
  }

  canStartSelectedAssessment(): boolean {
    return !!this.selectedAssessmentId;
  }

  shouldShowDetailAction(): boolean {
    return this.isLiveStatus(this.selectedAssessmentStatus) || this.isCompletedStatus(this.selectedAssessmentStatus);
  }

  openAssessmentAction(assessment: StudentAssessmentItem): void {
    this.assessmentDetailSubscription?.unsubscribe();
    this.loadingAssessmentId = assessment.assessmentId;
    this.errorMessage = '';
    this.attemptErrorMessage = '';

    this.assessmentDetailSubscription = this.studentAssessmentsService
      .getAssessmentDetail(assessment.assessmentId)
      .pipe(finalize(() => {
        this.loadingAssessmentId = null;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: detail => {
          this.selectedAssessmentDetail = detail;
          this.selectedAssessmentActionLabel = this.isCompletedStatus(assessment.assessmentStatus)
            ? 'View Report'
            : (this.isLiveStatus(assessment.assessmentStatus) ? this.getActionLabel(assessment.assessmentStatus) : '');
          this.selectedAssessmentName = assessment.assessmentName;
          this.selectedAssessmentId = assessment.assessmentId;
          this.selectedAssessmentStatus = assessment.assessmentStatus;
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
    this.selectedAssessmentId = null;
    this.selectedAssessmentStatus = '';
    this.isStartingAssessment = false;
    this.attemptErrorMessage = '';
  }

  async startSelectedAssessment(): Promise<void> {
    if (!this.canStartSelectedAssessment() || this.isStartingAssessment) {
      return;
    }

    if (this.isCompletedStatus(this.selectedAssessmentStatus)) {
      this.closeAssessmentDetailModal();
      void this.router.navigateByUrl(`/${RouteAddress.Student.Results}`);
      return;
    }

    const assessmentId = this.selectedAssessmentId;
    if (!assessmentId) {
      return;
    }

    this.attemptStartSubscription?.unsubscribe();
    this.isStartingAssessment = true;
    this.attemptErrorMessage = '';
    this.attemptStartSubscription = this.deviceInformationService
      .getProctoringDeviceDetails()
      .pipe(
        switchMap(deviceDetails =>
          this.studentAssessmentsService.startAssessment(assessmentId, deviceDetails).pipe(
            switchMap(attempt =>
              this.studentAssessmentsService
                .startProctorSession(attempt.attemptId, {
                  attemptId: attempt.attemptId,
                  assessmentId: attempt.assessmentId,
                  startedAt: attempt.startedAt ?? new Date().toISOString(),
                  browserName: deviceDetails.browserName,
                  browserVersion: deviceDetails.browserVersion,
                  operatingSystem: deviceDetails.operatingSystem,
                  deviceType: deviceDetails.deviceType,
                  userAgent: deviceDetails.userAgent,
                  ipAddress: deviceDetails.ipAddress
                })
                .pipe(map(session => ({ attempt, session })))
            )
          )
        )
      )
      .pipe(finalize(() => {
        this.isStartingAssessment = false;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: ({ attempt, session }) => {
          this.closeAssessmentDetailModal();
          void this.router.navigateByUrl(
            `/${RouteAddress.Student.AssessmentRunner}/${attempt.attemptId}/run`,
            { state: { attempt, session, startInFullscreen: true } }
          );
        },
        error: error => {
          console.error('Failed to start student assessment.', error);
          const errorMessage = error?.error?.message || 'Unable to start this assessment and proctoring session right now.';
          this.attemptErrorMessage = errorMessage;

          if (this.shouldTreatAssessmentAsCompleted(errorMessage)) {
            this.markSelectedAssessmentAsCompleted();
            this.loadAssessments();
          }

          this.changeDetectorRef.detectChanges();
        }
      });

    this.subscriptions.add(this.attemptStartSubscription);
  }

  private isLiveStatus(status: string | null | undefined): boolean {
    return status?.trim().toUpperCase() === 'LIVE';
  }

  private isCompletedStatus(status: string | null | undefined): boolean {
    return status?.trim().toUpperCase() === 'COMPLETED';
  }

  private shouldTreatAssessmentAsCompleted(message: string | null | undefined): boolean {
    const normalizedMessage = message?.trim().toLowerCase() ?? '';
    return normalizedMessage.includes('already expired and was auto-submitted')
      || normalizedMessage.includes('already been submitted by the current student');
  }

  private markSelectedAssessmentAsCompleted(): void {
    this.selectedAssessmentStatus = 'COMPLETED';
    this.selectedAssessmentActionLabel = 'View Report';
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
