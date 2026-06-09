import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { finalize, map, switchMap } from 'rxjs/operators';
import {
  ProctorSessionResponse,
  StudentAssessmentDetail,
  StudentAssessmentItem,
  StudentAttemptAnswer,
  StudentAttemptRecovery,
  StudentAttemptRecoveryQuestion,
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
  isSavingAnswer = false;
  isSubmittingAttempt = false;
  activeAttempt: StudentAttemptRecovery | null = null;
  currentQuestionIndex = 0;
  countdownSeconds = 0;
  attemptErrorMessage = '';
  loadingAssessmentId: string | null = null;
  private readonly subscriptions = new Subscription();
  private assessmentsLoadSubscription?: Subscription;
  private assessmentDetailSubscription?: Subscription;
  private attemptStartSubscription?: Subscription;
  private answerSaveSubscription?: Subscription;
  private attemptSubmitSubscription?: Subscription;
  private countdownTimerId: number | null = null;
  private heartbeatTimerId: number | null = null;
  private proctorSessionId: string | null = null;
  private readonly heartbeatIntervalMs = 25000;

  constructor(
    private readonly studentAssessmentsService: StudentAssessmentsService,
    private readonly deviceInformationService: DeviceInformationService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAssessments();
  }

  ngOnDestroy(): void {
    this.stopCountdown();
    this.stopHeartbeat();
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

  trackByQuestionOption(_: number, option: string): string {
    return option;
  }

  isMultipleAnswerQuestion(question: StudentAttemptRecoveryQuestion): boolean {
    return question.allowsMultipleAnswers;
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
    return this.isLiveStatus(status) ? 'Start Assessment' : 'View Details';
  }

  get currentQuestion(): StudentAttemptRecoveryQuestion | null {
    if (!this.activeAttempt?.questions?.length) {
      return null;
    }

    return this.activeAttempt.questions[this.currentQuestionIndex] ?? null;
  }

  get isAttemptMode(): boolean {
    return !!this.activeAttempt;
  }

  get formattedCountdown(): string {
    const hours = Math.floor(this.countdownSeconds / 3600);
    const minutes = Math.floor((this.countdownSeconds % 3600) / 60);
    const seconds = this.countdownSeconds % 60;

    return [hours, minutes, seconds]
      .map(value => value.toString().padStart(2, '0'))
      .join(':');
  }

  get isLastQuestion(): boolean {
    return !!this.activeAttempt?.questions?.length && this.currentQuestionIndex === this.activeAttempt.questions.length - 1;
  }

  getQuestionPositionLabel(): string {
    if (!this.activeAttempt?.questions?.length) {
      return 'Question';
    }

    return `Question ${this.currentQuestionIndex + 1} of ${this.activeAttempt.questions.length}`;
  }

  isPrimaryAction(status: string): boolean {
    return this.isLiveStatus(status);
  }

  canStartSelectedAssessment(): boolean {
    return !!this.selectedAssessmentId;
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
          this.selectedAssessmentActionLabel = this.getActionLabel(assessment.assessmentStatus);
          this.selectedAssessmentName = assessment.assessmentName;
          this.selectedAssessmentId = assessment.assessmentId;
          this.selectedAssessmentStatus = assessment.assessmentStatus;
          this.activeAttempt = null;
          this.currentQuestionIndex = 0;
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
    this.activeAttempt = null;
    this.currentQuestionIndex = 0;
    this.countdownSeconds = 0;
    this.isStartingAssessment = false;
    this.isSavingAnswer = false;
    this.isSubmittingAttempt = false;
    this.attemptErrorMessage = '';
    this.stopCountdown();
    this.stopHeartbeat();
    this.proctorSessionId = null;
  }

  startSelectedAssessment(): void {
    if (!this.canStartSelectedAssessment() || this.isStartingAssessment) {
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
        ),
      )
      .pipe(finalize(() => {
        this.isStartingAssessment = false;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: ({ attempt, session }) => {
          this.activateAttempt(attempt, session);
        },
        error: error => {
          console.error('Failed to start student assessment.', error);
          this.attemptErrorMessage = error?.error?.message || 'Unable to start this assessment and proctoring session right now.';
          this.changeDetectorRef.detectChanges();
        }
      });

    this.subscriptions.add(this.attemptStartSubscription);
  }

  showPreviousQuestion(): void {
    if (this.currentQuestionIndex <= 0 || this.isSavingAnswer || this.isSubmittingAttempt) {
      return;
    }

    this.currentQuestionIndex -= 1;
  }

  showNextQuestion(): void {
    if (
      !this.activeAttempt ||
      this.isSavingAnswer ||
      this.isSubmittingAttempt ||
      this.currentQuestionIndex >= this.activeAttempt.questions.length - 1
    ) {
      return;
    }

    this.persistCurrentAnswer(savedAnswer => {
      const currentQuestion = this.currentQuestion;
      if (currentQuestion) {
        currentQuestion.selectedAnswer = savedAnswer.selectedAnswer ?? null;
        currentQuestion.selectedAnswers = savedAnswer.selectedAnswers ?? null;
        currentQuestion.answeredAt = savedAnswer.answeredAt ?? null;
      }

      this.currentQuestionIndex += 1;
      this.changeDetectorRef.detectChanges();
    });
  }

  submitCurrentAttempt(): void {
    if (!this.activeAttempt || this.isSavingAnswer || this.isSubmittingAttempt) {
      return;
    }

    this.persistCurrentAnswer(savedAnswer => {
      const attempt = this.activeAttempt;
      const currentQuestion = this.currentQuestion;
      if (!attempt) {
        return;
      }

      if (currentQuestion) {
        currentQuestion.selectedAnswer = savedAnswer.selectedAnswer ?? null;
        currentQuestion.selectedAnswers = savedAnswer.selectedAnswers ?? null;
        currentQuestion.answeredAt = savedAnswer.answeredAt ?? null;
      }

      this.attemptSubmitSubscription?.unsubscribe();
      this.isSubmittingAttempt = true;
      this.attemptErrorMessage = '';

      this.attemptSubmitSubscription = this.studentAssessmentsService
        .submitAttempt(attempt.attemptId)
        .pipe(finalize(() => {
          this.isSubmittingAttempt = false;
          this.changeDetectorRef.detectChanges();
        }))
        .subscribe({
          next: () => {
            this.exitAttemptMode();
            this.loadAssessments();
          },
          error: error => {
            console.error('Failed to submit student assessment attempt.', error);
            this.attemptErrorMessage = error?.error?.message || 'Unable to submit this assessment right now.';
            this.changeDetectorRef.detectChanges();
          }
        });

      this.subscriptions.add(this.attemptSubmitSubscription);
    });
  }

  selectQuestionOption(question: StudentAttemptRecoveryQuestion, option: string): void {
    if (question.allowsMultipleAnswers) {
      const selectedAnswers = question.selectedAnswers?.length
        ? question.selectedAnswers
        : (question.selectedAnswer ? [question.selectedAnswer] : []);
      const isSelected = selectedAnswers.includes(option);
      const nextSelections = isSelected
        ? selectedAnswers.filter(value => value !== option)
        : [...selectedAnswers, option];

      question.selectedAnswers = nextSelections;
      question.selectedAnswer = nextSelections[0] ?? null;
      return;
    }

    question.selectedAnswer = option;
    question.selectedAnswers = [option];
  }

  isQuestionOptionSelected(question: StudentAttemptRecoveryQuestion, option: string): boolean {
    return question.allowsMultipleAnswers
      ? ((question.selectedAnswers?.length
          ? question.selectedAnswers
          : (question.selectedAnswer ? [question.selectedAnswer] : [])).includes(option))
      : question.selectedAnswer === option;
  }

  exitAttemptMode(): void {
    this.activeAttempt = null;
    this.currentQuestionIndex = 0;
    this.countdownSeconds = 0;
    this.isSavingAnswer = false;
    this.isSubmittingAttempt = false;
    this.attemptErrorMessage = '';
    this.stopCountdown();
    this.stopHeartbeat();
    this.proctorSessionId = null;
    this.changeDetectorRef.detectChanges();
  }

  private persistCurrentAnswer(onSuccess: (savedAnswer: StudentAttemptAnswer) => void): void {
    if (!this.activeAttempt) {
      return;
    }

    const currentQuestion = this.currentQuestion;
    if (!currentQuestion) {
      return;
    }

    this.answerSaveSubscription?.unsubscribe();
    this.isSavingAnswer = true;
    this.attemptErrorMessage = '';

    this.answerSaveSubscription = this.studentAssessmentsService
      .saveAttemptAnswer(this.activeAttempt.attemptId, currentQuestion.questionId, {
        selectedAnswer: currentQuestion.allowsMultipleAnswers
          ? (currentQuestion.selectedAnswers?.[0] ?? null)
          : (currentQuestion.selectedAnswer ?? null),
        selectedAnswers: currentQuestion.allowsMultipleAnswers
          ? (currentQuestion.selectedAnswers ?? [])
          : (currentQuestion.selectedAnswer ? [currentQuestion.selectedAnswer] : [])
      })
      .pipe(finalize(() => {
        this.isSavingAnswer = false;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: savedAnswer => {
          onSuccess(savedAnswer);
        },
        error: error => {
          console.error('Failed to save student assessment answer.', error);
          this.attemptErrorMessage = error?.error?.message || 'Unable to save this answer right now.';
          this.changeDetectorRef.detectChanges();
        }
      });

    this.subscriptions.add(this.answerSaveSubscription);
  }

  private isLiveStatus(status: string | null | undefined): boolean {
    return status?.trim().toUpperCase() === 'LIVE';
  }

  private activateAttempt(attempt: StudentAttemptRecovery, session: ProctorSessionResponse): void {
    this.activeAttempt = attempt;
    this.proctorSessionId = session.sessionId;
    this.currentQuestionIndex = 0;
    this.attemptErrorMessage = '';
    this.selectedAssessmentName = attempt.assessmentName;
    this.selectedAssessmentDetail = null;
    this.isDetailModalOpen = false;
    this.countdownSeconds = this.resolveInitialCountdown(attempt);
    this.sendHeartbeat();
    this.startHeartbeat();
    this.startCountdown();
    this.changeDetectorRef.detectChanges();
  }

  private resolveInitialCountdown(attempt: StudentAttemptRecovery): number {
    const maxDurationSeconds = Math.max(0, attempt.durationMinutes * 60);
    const normalizedStatus = attempt.attemptStatus?.trim().toUpperCase();

    if (normalizedStatus === 'IN_PROGRESS' && attempt.remainingSeconds >= 0) {
      return maxDurationSeconds > 0
        ? Math.min(attempt.remainingSeconds, maxDurationSeconds)
        : attempt.remainingSeconds;
    }

    if (attempt.remainingSeconds > 0) {
      return attempt.remainingSeconds;
    }

    return maxDurationSeconds;
  }

  private startCountdown(): void {
    this.stopCountdown();

    if (this.countdownSeconds <= 0) {
      return;
    }

    this.countdownTimerId = window.setInterval(() => {
      if (this.countdownSeconds <= 1) {
        this.countdownSeconds = 0;
        this.stopCountdown();
      } else {
        this.countdownSeconds -= 1;
      }

      this.changeDetectorRef.detectChanges();
    }, 1000);
  }

  private stopCountdown(): void {
    if (this.countdownTimerId !== null) {
      window.clearInterval(this.countdownTimerId);
      this.countdownTimerId = null;
    }
  }

  private startHeartbeat(): void {
    this.stopHeartbeat();

    if (!this.activeAttempt || !this.proctorSessionId) {
      return;
    }

    this.heartbeatTimerId = window.setInterval(() => {
      this.sendHeartbeat();
    }, this.heartbeatIntervalMs);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatTimerId !== null) {
      window.clearInterval(this.heartbeatTimerId);
      this.heartbeatTimerId = null;
    }
  }

  private sendHeartbeat(): void {
    if (!this.activeAttempt || !this.proctorSessionId) {
      return;
    }

    const currentQuestionId = this.currentQuestion?.questionId ?? null;

    const heartbeatSubscription = this.studentAssessmentsService
      .sendSessionHeartbeat(this.proctorSessionId, {
        attemptId: this.activeAttempt.attemptId,
        clientTimestamp: new Date().toISOString(),
        visibilityState: this.deviceInformationService.getVisibilityState(),
        isFullscreen: this.deviceInformationService.isFullscreenActive(),
        networkStatus: this.deviceInformationService.getNetworkStatus(),
        questionId: currentQuestionId
      })
      .subscribe({
        error: error => {
          console.warn('Failed to send proctoring heartbeat.', error);
        }
      });

    this.subscriptions.add(heartbeatSubscription);
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
