import { ChangeDetectorRef, Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { RouteAddress } from '../../../common/constants/routes.constants';
import {
  ProctorSessionEnforcementResponse,
  ProctorSessionResponse,
  ProctorSessionRuleResponse,
  ProctorSessionStateResponse,
  StudentAssessmentsService,
  StudentAttemptAnswer,
  StudentAttemptRecovery,
  StudentAttemptRecoveryQuestion,
  StudentResult,
  StudentResultQuestionResult
} from '../../../common/services/api/student-assessments.service';
import {
  AssessmentProctoringService,
  ProctoringViolationEvent
} from '../../../common/services/assessment/assessment-proctoring.service';
import { Session } from '../../../common/services/session/session.service';
import { DeviceInformationService } from '../../../common/services/utilities/device-information.service';

interface PersistedProctoringSessionState {
  attemptId: string;
  sessionId: string;
  assessmentId: string;
}

interface AssessmentRunnerNavigationState {
  attempt?: StudentAttemptRecovery;
  session?: ProctorSessionResponse;
  startInFullscreen?: boolean;
}

@Component({
  selector: 'app-student-assessment-runner',
  standalone: false,
  templateUrl: './assessment-runner.component.html',
  styleUrl: './assessment-runner.component.scss'
})
export class AssessmentRunnerComponent implements OnInit, OnDestroy {
  private static readonly sessionStoragePrefix = 'taskverse.proctoring.session.';
  private static readonly resumeStoragePrefix = 'taskverse.proctoring.resume.';

  attempt: StudentAttemptRecovery | null = null;
  result: StudentResult | null = null;
  currentQuestionIndex = 0;
  countdownSeconds = 0;
  violationCount = 0;
  proctorRules: ProctorSessionRuleResponse[] = [];
  proctorEnforcement: ProctorSessionEnforcementResponse = { action: 'NONE' };
  isBootstrapping = true;
  isStartingAttempt = false;
  isSavingAnswer = false;
  isSubmittingAttempt = false;
  isAutoSubmitting = false;
  isLoadingImmediateResult = false;
  requiresFullscreenRecovery = false;
  attemptErrorMessage = '';
  resultErrorMessage = '';
  violationMessage = '';
  @ViewChild('fullscreenShell') private fullscreenShell?: ElementRef<HTMLElement>;
  private attemptId = '';
  private proctorSessionId: string | null = null;
  private readonly subscriptions = new Subscription();
  private countdownTimerId: number | null = null;
  private heartbeatTimerId: number | null = null;
  private readonly heartbeatIntervalMs = 25000;
  private readonly lastViolationTimestamps = new Map<string, number>();
  private shouldAutoStartRuntime = false;

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly router: Router,
    private readonly studentAssessmentsService: StudentAssessmentsService,
    private readonly assessmentProctoringService: AssessmentProctoringService,
    private readonly deviceInformationService: DeviceInformationService,
    private readonly session: Session,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.attemptId = this.activatedRoute.snapshot.paramMap.get('attemptId') ?? '';
    if (!this.attemptId) {
      this.navigateToAssessments();
      return;
    }

    void this.bootstrapRunner();
  }

  ngOnDestroy(): void {
    this.stopAttemptRuntime();
    this.subscriptions.unsubscribe();
  }

  @HostListener('window:beforeunload')
  onBeforeUnload(): void {
    if (!this.isActiveAttemptState()) {
      return;
    }

    this.markResumeInterruptionPending();
  }

  @HostListener('window:pagehide')
  onPageHide(): void {
    if (!this.isActiveAttemptState()) {
      return;
    }

    this.markResumeInterruptionPending();
  }

  @HostListener('window:popstate', ['$event'])
  onPopState(event: PopStateEvent): void {
    if (!this.isActiveAttemptState()) {
      return;
    }

    event.preventDefault?.();
    window.history.pushState({ assessmentRunner: true }, '', window.location.href);
    this.markResumeInterruptionPending();
    this.processViolation({
      eventType: 'TAB_SWITCHED',
      severity: 'Warning',
      countsTowardViolation: true,
      questionId: this.currentQuestion?.questionId ?? null,
      metadata: { source: 'popstate' }
    });
  }

  get currentQuestion(): StudentAttemptRecoveryQuestion | null {
    if (!this.attempt?.questions?.length) {
      return null;
    }

    return this.attempt.questions[this.currentQuestionIndex] ?? null;
  }

  get hasImmediateResult(): boolean {
    return !!this.result && this.result.showResultsImmediately;
  }

  get isResultPending(): boolean {
    return !!this.result && !this.result.showResultsImmediately;
  }

  get rankSummary(): string {
    if (!this.result) {
      return '--';
    }

    const participantCount = Math.max(this.result.participantCount, this.result.rank);
    return `${this.result.rank} / ${participantCount}`;
  }

  get isFullscreenActive(): boolean {
    return this.deviceInformationService.isFullscreenActive();
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
    return !!this.attempt?.questions?.length && this.currentQuestionIndex === this.attempt.questions.length - 1;
  }

  getQuestionPositionLabel(): string {
    if (!this.attempt?.questions?.length) {
      return 'Question';
    }

    return `Question ${this.currentQuestionIndex + 1} of ${this.attempt.questions.length}`;
  }

  get autoSubmitRuleCount(): number {
    return this.proctorRules.filter(rule => rule.autoSubmitOnLimitExceeded).length;
  }

  isCodingQuestion(question: StudentAttemptRecoveryQuestion | null | undefined): boolean {
    return question?.questionType?.trim().toLowerCase() === 'coding';
  }

  isMultipleAnswerQuestion(question: StudentAttemptRecoveryQuestion): boolean {
    return question.allowsMultipleAnswers;
  }

  updateCodingAnswer(question: StudentAttemptRecoveryQuestion, code: string): void {
    question.selectedAnswer = code;
    question.selectedAnswers = code ? [code] : [];
  }

  trackByQuestionOption(_: number, option: string): string {
    return option;
  }

  trackByQuestionResult(_: number, item: StudentResultQuestionResult): string {
    return item.questionId;
  }

  getJoinedAnswers(values: string[] | null | undefined): string {
    return values?.length ? values.join(', ') : 'Not answered';
  }

  goToResults(): void {
    void this.router.navigateByUrl(`/${RouteAddress.Student.Results}`);
  }

  async beginProctoredAttempt(): Promise<void> {
    if (this.isStartingAttempt || this.result) {
      return;
    }

    this.isStartingAttempt = true;
    this.attemptErrorMessage = '';
    this.changeDetectorRef.detectChanges();

    const enteredFullscreen = await this.assessmentProctoringService.enterFullscreen(this.fullscreenShell?.nativeElement);
    if (!enteredFullscreen) {
      this.isStartingAttempt = false;
      this.attemptErrorMessage = 'Full screen access is required before you can continue with this assessment.';
      this.changeDetectorRef.detectChanges();
      return;
    }

    this.requiresFullscreenRecovery = false;
    this.startAttemptRuntime();
    this.isStartingAttempt = false;

    if (this.consumePendingResumeInterruption()) {
      this.processViolation({
        eventType: 'TAB_SWITCHED',
        severity: 'Warning',
        countsTowardViolation: true,
        questionId: this.currentQuestion?.questionId ?? null,
        metadata: { source: 'resume' }
      });
    }

    this.changeDetectorRef.detectChanges();
  }

  async reEnterFullscreen(): Promise<void> {
    await this.beginProctoredAttempt();
  }

  showPreviousQuestion(): void {
    if (this.currentQuestionIndex <= 0 || this.isSavingAnswer || this.isSubmittingAttempt) {
      return;
    }

    this.currentQuestionIndex -= 1;
  }

  showNextQuestion(): void {
    if (
      !this.attempt ||
      this.isSavingAnswer ||
      this.isSubmittingAttempt ||
      this.currentQuestionIndex >= this.attempt.questions.length - 1
    ) {
      return;
    }

    this.persistCurrentAnswer(savedAnswer => {
      const currentQuestion = this.currentQuestion;
      if (currentQuestion) {
        this.applySavedAnswer(currentQuestion, savedAnswer);
      }

      this.currentQuestionIndex += 1;
      this.changeDetectorRef.detectChanges();
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

  submitCurrentAttempt(): void {
    if (!this.attempt || this.isSavingAnswer || this.isSubmittingAttempt) {
      return;
    }

    this.persistCurrentAnswer(savedAnswer => {
      const currentQuestion = this.currentQuestion;
      if (currentQuestion) {
        this.applySavedAnswer(currentQuestion, savedAnswer);
      }

      this.finalizeAttemptSubmission(false);
    });
  }

  returnToAssessments(): void {
    this.navigateToAssessments();
  }

  private async bootstrapRunner(): Promise<void> {
    const navigationState = this.readNavigationState();
    const initialAttempt = navigationState?.attempt ?? null;
    const initialSession = navigationState?.session ?? null;
    this.shouldAutoStartRuntime = navigationState?.startInFullscreen === true || (!!initialAttempt && !!initialSession);

    this.attempt = initialAttempt;
    if (this.attempt) {
      this.applyAttemptState(this.attempt);
    }

    if (initialAttempt && initialSession && !this.consumePendingResumeInterruption() && !this.readPersistedSessionState()) {
      this.proctorSessionId = initialSession.sessionId;
      this.persistProctoringSessionState(initialSession);
      this.isBootstrapping = false;
      this.changeDetectorRef.detectChanges();
      this.refreshProctorSessionState(false);
      return;
    }

    this.refreshProctorSessionState(true);
  }

  private readNavigationState(): AssessmentRunnerNavigationState | undefined {
    const currentNavigationState = this.router.getCurrentNavigation()?.extras.state as AssessmentRunnerNavigationState | undefined;
    if (currentNavigationState?.attempt || currentNavigationState?.session) {
      return currentNavigationState;
    }

    const historyState = window.history.state as AssessmentRunnerNavigationState | undefined;
    if (historyState?.attempt || historyState?.session) {
      return historyState;
    }

    return undefined;
  }

  private refreshProctorSessionState(shouldRecoverAttemptWhenMissing: boolean): void {
    const sessionSubscription = this.studentAssessmentsService
      .getProctorSessionStateByAttempt(this.attemptId)
      .subscribe({
        next: sessionState => {
          this.proctorSessionId = sessionState.sessionId;
          this.persistProctoringSessionState({
            sessionId: sessionState.sessionId,
            attemptId: sessionState.attemptId,
            assessmentId: sessionState.assessmentId,
            studentId: sessionState.studentId,
            status: sessionState.status,
            startedAt: sessionState.startedAt ?? null,
            endedAt: sessionState.endedAt ?? null
          });
          this.applyProctorSessionState(sessionState);
          this.handleServerEnforcement(sessionState.enforcement);

          if (shouldRecoverAttemptWhenMissing && !this.attempt) {
            const recoverySubscription = this.studentAssessmentsService
              .getAttemptRecovery(this.attemptId)
              .pipe(finalize(() => {
                this.isBootstrapping = false;
                this.changeDetectorRef.detectChanges();
              }))
              .subscribe({
                next: attempt => {
                  if (this.isAttemptClosed(attempt.attemptStatus)) {
                    void this.resolveClosedAttemptOutcome(attempt.attemptId);
                    return;
                  }

                  this.attempt = attempt;
                  this.applyAttemptState(attempt);
                  this.tryAutoStartRuntime();
                  this.changeDetectorRef.detectChanges();
                },
                error: error => {
                  console.error('Failed to restore student assessment attempt.', error);
                  this.attemptErrorMessage = error?.error?.message || 'Unable to restore this assessment attempt right now.';
                  this.changeDetectorRef.detectChanges();
                }
              });

            this.subscriptions.add(recoverySubscription);
            return;
          }

          this.isBootstrapping = false;
          this.tryAutoStartRuntime();
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          console.error('Failed to restore proctor session state.', error);
          this.attemptErrorMessage = error?.error?.message || 'Unable to restore the proctoring session right now.';
          this.isBootstrapping = false;
          this.changeDetectorRef.detectChanges();
        }
      });

    this.subscriptions.add(sessionSubscription);
  }

  private applyAttemptState(attempt: StudentAttemptRecovery): void {
    this.currentQuestionIndex = 0;
    this.countdownSeconds = this.resolveInitialCountdown(attempt);
    window.history.pushState({ assessmentRunner: true }, '', window.location.href);
  }

  private startAttemptRuntime(): void {
    if (!this.attempt || !this.proctorSessionId) {
      return;
    }

    this.stopAttemptRuntime();
    this.assessmentProctoringService.startMonitoring({
      getCurrentQuestionId: () => this.currentQuestion?.questionId ?? null,
      onViolation: event => this.processViolation(event)
    });
    this.sendHeartbeat();
    this.startHeartbeat();
    this.startCountdown();
  }

  private tryAutoStartRuntime(): void {
    if (
      !this.shouldAutoStartRuntime ||
      !this.isFullscreenActive ||
      !this.attempt ||
      !this.proctorSessionId ||
      this.proctorEnforcement.action !== 'NONE'
    ) {
      return;
    }

    this.shouldAutoStartRuntime = false;
    this.requiresFullscreenRecovery = false;
    this.startAttemptRuntime();
  }

  private stopAttemptRuntime(): void {
    this.stopCountdown();
    this.stopHeartbeat();
    this.assessmentProctoringService.stopMonitoring();
  }

  private startCountdown(): void {
    this.stopCountdown();

    if (this.countdownSeconds <= 0) {
      this.finalizeAttemptSubmission(true);
      return;
    }

    this.countdownTimerId = window.setInterval(() => {
      if (this.countdownSeconds <= 1) {
        this.countdownSeconds = 0;
        this.stopCountdown();
        this.finalizeAttemptSubmission(true);
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

    if (!this.attempt || !this.proctorSessionId) {
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
    if (!this.attempt || !this.proctorSessionId) {
      return;
    }

    const heartbeatSubscription = this.studentAssessmentsService
      .sendSessionHeartbeat(this.proctorSessionId, {
        attemptId: this.attempt.attemptId,
        clientTimestamp: new Date().toISOString(),
        visibilityState: this.deviceInformationService.getVisibilityState(),
        isFullscreen: this.deviceInformationService.isFullscreenActive(),
        networkStatus: this.deviceInformationService.getNetworkStatus(),
        questionId: this.currentQuestion?.questionId ?? null
      })
      .subscribe({
        next: response => {
          this.applyProctorSessionState(response.sessionState);
          this.handleServerEnforcement(response.sessionState.enforcement);
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          console.warn('Failed to send proctoring heartbeat.', error);
        }
      });

    this.subscriptions.add(heartbeatSubscription);
  }

  private persistCurrentAnswer(onSuccess: (savedAnswer: StudentAttemptAnswer) => void): void {
    if (!this.attempt) {
      return;
    }

    const currentQuestion = this.currentQuestion;
    if (!currentQuestion) {
      return;
    }

    this.isSavingAnswer = true;
    this.attemptErrorMessage = '';

    const saveSubscription = this.studentAssessmentsService
      .saveAttemptAnswer(this.attempt.attemptId, currentQuestion.questionId, {
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

    this.subscriptions.add(saveSubscription);
  }

  private finalizeAttemptSubmission(triggeredAutomatically: boolean): void {
    if (!this.attempt || this.isSubmittingAttempt) {
      return;
    }

    if (triggeredAutomatically && !this.isSavingAnswer && this.currentQuestion) {
      this.persistCurrentAnswer(savedAnswer => {
        const currentQuestion = this.currentQuestion;
        if (currentQuestion) {
          this.applySavedAnswer(currentQuestion, savedAnswer);
        }

        this.executeAttemptSubmission(triggeredAutomatically);
      });
      return;
    }

    this.executeAttemptSubmission(triggeredAutomatically);
  }

  private executeAttemptSubmission(triggeredAutomatically: boolean): void {
    if (!this.attempt || this.isSubmittingAttempt) {
      return;
    }

    this.stopAttemptRuntime();
    this.assessmentProctoringService.flushQueuedEvents(this.proctorSessionId ?? '').subscribe({
      error: error => {
        console.warn('Failed to flush queued proctoring events before submission.', error);
      }
    });
    this.isSubmittingAttempt = true;
    this.isAutoSubmitting = triggeredAutomatically;
    this.attemptErrorMessage = triggeredAutomatically
      ? 'Proctoring threshold reached. Your assessment is being submitted automatically.'
      : '';

    const submitSubscription = this.studentAssessmentsService
      .submitAttempt(this.attempt.attemptId)
      .pipe(finalize(() => {
        this.isSubmittingAttempt = false;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: () => {
          void this.completeAttemptAndResolveOutcome();
        },
        error: error => {
          console.error('Failed to submit student assessment attempt.', error);

          if (this.isAttemptAlreadySubmittedError(error)) {
            const submittedAttemptId = this.attempt?.attemptId ?? this.attemptId;
            void this.resolveClosedAttemptOutcome(submittedAttemptId);
            return;
          }

          this.attemptErrorMessage = error?.error?.message || 'Unable to submit this assessment right now.';
          this.startAttemptRuntime();
          this.changeDetectorRef.detectChanges();
        }
      });

    this.subscriptions.add(submitSubscription);
  }

  private async completeAttemptAndResolveOutcome(): Promise<void> {
    const submittedAttemptId = this.attempt?.attemptId ?? this.attemptId;
    this.attemptErrorMessage = '';

    await this.finalizeAttemptTeardownAndNavigateAsync(
      submittedAttemptId,
      () => this.endProctoringSession(
        this.isAutoSubmitting ? 'ASSESSMENT_AUTO_SUBMITTED' : 'ASSESSMENT_SUBMITTED',
        'Warning',
        {
          autoSubmitted: this.isAutoSubmitting,
          violationCount: this.violationCount
        })
    );
  }

  private loadImmediateResultOrReturn(attemptId: string): void {
    this.isLoadingImmediateResult = true;
    this.resultErrorMessage = '';
    this.changeDetectorRef.detectChanges();
    this.pollForResult(attemptId, 6);
  }

  private pollForResult(attemptId: string, remainingPolls: number): void {
    const resultSubscription = this.studentAssessmentsService
      .getStudentAttemptResult(attemptId)
      .pipe(finalize(() => {
        this.isLoadingImmediateResult = false;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: result => {
          if (result) {
            this.result = result;
            this.attempt = null;
            this.violationMessage = '';
            this.changeDetectorRef.detectChanges();
            return;
          }

          if (remainingPolls <= 1) {
            this.navigateToAssessments();
            return;
          }

          this.isLoadingImmediateResult = true;
          window.setTimeout(() => this.pollForResult(attemptId, remainingPolls - 1), 1500);
        },
        error: error => {
          console.error('Failed to resolve immediate student result.', error);
          if (remainingPolls <= 1) {
            this.navigateToAssessments();
            return;
          }

          this.isLoadingImmediateResult = true;
          window.setTimeout(() => this.pollForResult(attemptId, remainingPolls - 1), 1500);
        }
      });

    this.subscriptions.add(resultSubscription);
  }

  private processViolation(event: ProctoringViolationEvent): void {
    if (!this.attempt || !this.proctorSessionId || !this.shouldRecordViolation(event)) {
      return;
    }

    this.assessmentProctoringService.queueEvent(this.attempt.attemptId, event);

    if (event.eventType === 'FULLSCREEN_EXITED') {
      this.requiresFullscreenRecovery = true;
      this.stopHeartbeat();
    }

    this.violationMessage = this.buildLocalViolationMessage(event);
    this.changeDetectorRef.detectChanges();

    const flushSubscription = this.assessmentProctoringService.flushQueuedEvents(this.proctorSessionId).subscribe({
      next: response => {
        if (!response) {
          return;
        }

        this.applyProctorSessionState(response.sessionState);
        this.handleServerEnforcement(response.sessionState.enforcement);
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        console.warn('Failed to record proctoring violation.', error);
      }
    });

    this.subscriptions.add(flushSubscription);
  }

  private shouldRecordViolation(event: ProctoringViolationEvent): boolean {
    const now = Date.now();
    const key = event.eventType;
    const lastTimestamp = this.lastViolationTimestamps.get(key) ?? 0;

    if (now - lastTimestamp < 1200) {
      return false;
    }

    this.lastViolationTimestamps.set(key, now);
    return true;
  }

  private buildLocalViolationMessage(event: ProctoringViolationEvent): string {
    switch (event.eventType) {
      case 'TAB_SWITCHED':
        return 'Tab switching or leaving the assessment window was detected.';
      case 'FULLSCREEN_EXITED':
        return 'Full screen was exited. Re-enter full screen to continue.';
      case 'POSSIBLE_DEVTOOLS_OPENED':
        return 'A restricted developer tools shortcut was detected.';
      case 'BLOCKED_KEYBOARD_SHORTCUT':
        return 'A restricted keyboard shortcut was detected.';
      case 'NETWORK_DISCONNECTED':
        return 'Your network connection dropped during the assessment.';
      case 'COPY_ATTEMPTED':
      case 'PASTE_ATTEMPTED':
      case 'CUT_ATTEMPTED':
      case 'CONTEXT_MENU_ATTEMPTED':
        return 'A restricted interaction was detected during the assessment.';
      default:
        return 'A proctoring violation was detected.';
    }
  }

  private applyProctorSessionState(sessionState: ProctorSessionStateResponse): void {
    this.violationCount = this.calculateViolationCount(sessionState);
    this.proctorRules = sessionState.rules ?? [];
    this.proctorEnforcement = sessionState.enforcement ?? { action: 'NONE' };

    const triggeredRule = this.findRuleByEventType(this.proctorEnforcement.triggeredByEventType);
    if (this.proctorEnforcement.message) {
      this.violationMessage = this.proctorEnforcement.message;
    } else if (triggeredRule?.warningMessage) {
      this.violationMessage = triggeredRule.warningMessage;
    }
  }

  private handleServerEnforcement(enforcement: ProctorSessionEnforcementResponse): void {
    if (!enforcement || enforcement.action === 'NONE') {
      return;
    }

    if (enforcement.action === 'LOCK') {
      this.requiresFullscreenRecovery = true;
      this.attemptErrorMessage = enforcement.message || 'A proctoring threshold was reached. Follow the proctoring instructions to continue.';
      this.stopHeartbeat();
      return;
    }

    if (enforcement.action === 'AUTO_SUBMIT') {
      this.stopAttemptRuntime();
      this.isAutoSubmitting = true;
      this.attemptErrorMessage = enforcement.message || 'A proctoring threshold was reached. Your assessment was submitted automatically.';
      void this.completeServerAutoSubmittedAttempt();
    }
  }

  private async completeServerAutoSubmittedAttempt(): Promise<void> {
    const submittedAttemptId = this.attempt?.attemptId ?? this.attemptId;
    await this.finalizeAttemptTeardownAndNavigateAsync(submittedAttemptId);
  }

  private isAttemptClosed(attemptStatus: string | null | undefined): boolean {
    const normalizedStatus = attemptStatus?.trim().toUpperCase();
    return normalizedStatus === 'SUBMITTED' || normalizedStatus === 'AUTO_SUBMITTED';
  }

  private isAttemptAlreadySubmittedError(error: unknown): boolean {
    const candidateMessage = this.extractErrorMessage(error).toLowerCase();
    return candidateMessage.includes('already been submitted');
  }

  private extractErrorMessage(error: unknown): string {
    if (!error || typeof error !== 'object') {
      return '';
    }

    const responseError = (error as { error?: { message?: string } }).error;
    if (responseError?.message) {
      return responseError.message;
    }

    const directMessage = (error as { message?: string }).message;
    return directMessage ?? '';
  }

  private async resolveClosedAttemptOutcome(attemptId: string): Promise<void> {
    this.stopAttemptRuntime();
    this.attemptErrorMessage = '';
    await this.finalizeAttemptTeardownAndNavigateAsync(attemptId);
  }

  private async finalizeAttemptTeardownAndNavigateAsync(
    attemptId: string,
    beforeNavigate?: () => Promise<void>
  ): Promise<void> {
    try {
      await beforeNavigate?.();
    } catch (error) {
      console.warn('Failed to finish pre-navigation assessment cleanup.', error);
    }

    this.attempt = null;
    this.clearPersistedAttemptState();
    this.assessmentProctoringService.clearQueuedEvents();

    try {
      await this.assessmentProctoringService.exitFullscreen();
    } catch (error) {
      console.warn('Failed to exit fullscreen during assessment teardown.', error);
    } finally {
      this.loadImmediateResultOrReturn(attemptId);
    }
  }

  private findRuleByEventType(eventType: string | null | undefined): ProctorSessionRuleResponse | undefined {
    if (!eventType) {
      return undefined;
    }

    return this.proctorRules.find(rule => rule.eventType?.toUpperCase() === eventType.toUpperCase());
  }

  private calculateViolationCount(sessionState: ProctorSessionStateResponse): number {
    const summary = sessionState.summary;
    return summary.tabSwitchCount
      + summary.fullScreenExitCount
      + summary.copyAttemptCount
      + summary.pasteAttemptCount
      + summary.cutAttemptCount
      + summary.contextMenuAttemptCount
      + summary.blockedShortcutCount
      + summary.possibleDevtoolsCount
      + summary.networkDisconnectCount;
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

  private applySavedAnswer(question: StudentAttemptRecoveryQuestion, savedAnswer: StudentAttemptAnswer): void {
    question.selectedAnswer = savedAnswer.selectedAnswer ?? null;
    question.selectedAnswers = savedAnswer.selectedAnswers ?? null;
    question.answeredAt = savedAnswer.answeredAt ?? null;
  }

  private persistProctoringSessionState(session: ProctorSessionResponse): void {
    const key = `${AssessmentRunnerComponent.sessionStoragePrefix}${session.attemptId}`;
    const state: PersistedProctoringSessionState = {
      attemptId: session.attemptId,
      sessionId: session.sessionId,
      assessmentId: session.assessmentId
    };
    sessionStorage.setItem(key, JSON.stringify(state));
  }

  private readPersistedSessionState(): PersistedProctoringSessionState | null {
    const key = `${AssessmentRunnerComponent.sessionStoragePrefix}${this.attemptId}`;
    const value = sessionStorage.getItem(key);
    if (!value) {
      return null;
    }

    try {
      return JSON.parse(value) as PersistedProctoringSessionState;
    } catch {
      sessionStorage.removeItem(key);
      return null;
    }
  }

  private markResumeInterruptionPending(): void {
    const key = `${AssessmentRunnerComponent.resumeStoragePrefix}${this.attemptId}`;
    sessionStorage.setItem(key, '1');
  }

  private consumePendingResumeInterruption(): boolean {
    const key = `${AssessmentRunnerComponent.resumeStoragePrefix}${this.attemptId}`;
    const hasPendingMarker = sessionStorage.getItem(key) === '1';
    if (hasPendingMarker) {
      sessionStorage.removeItem(key);
    }

    return hasPendingMarker;
  }

  private clearPersistedAttemptState(): void {
    sessionStorage.removeItem(`${AssessmentRunnerComponent.sessionStoragePrefix}${this.attemptId}`);
    sessionStorage.removeItem(`${AssessmentRunnerComponent.resumeStoragePrefix}${this.attemptId}`);
  }

  private async endProctoringSession(
    eventType: string,
    severity: string,
    metadata: Record<string, unknown>
  ): Promise<void> {
    if (!this.proctorSessionId || !this.attempt) {
      return;
    }

    await new Promise<void>(resolve => {
      const endSubscription = this.studentAssessmentsService.endProctorSession(this.proctorSessionId!, {
        attemptId: this.attempt!.attemptId,
        eventType,
        severity,
        clientTimestamp: new Date().toISOString(),
        metadata
      }).subscribe({
        next: () => resolve(),
        error: () => resolve()
      });

      this.subscriptions.add(endSubscription);
    });
  }

  private isActiveAttemptState(): boolean {
    return !!this.attempt && !this.result && !this.isBootstrapping;
  }

  private navigateToAssessments(): void {
    void this.router.navigateByUrl(`/${RouteAddress.Student.MyAssessments}`);
  }
}
