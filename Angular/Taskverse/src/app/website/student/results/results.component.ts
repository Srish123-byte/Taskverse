import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, Subscription } from 'rxjs';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { Session } from '../../../common/services/session/session.service';
import {
  StudentAssessmentsService,
  StudentResult,
  StudentResultQuestionResult
} from '../../../common/services/api/student-assessments.service';

type ResultFilter = 'ALL' | 'CORRECT' | 'INCORRECT' | 'UNANSWERED' | 'PENDING';

interface AttemptResultNavigationState {
  pollForResult?: boolean;
}

@Component({
  selector: 'app-student-results',
  standalone: false,
  templateUrl: './results.component.html',
  styleUrl: './results.component.scss'
})
export class ResultsComponent implements OnInit, OnDestroy {
  result: StudentResult | null = null;
  isLoading = false;
  errorMessage = '';
  activeFilter: ResultFilter = 'ALL';
  isListMode = false;
  resultsList: StudentResult[] = [];
  private attemptId: string | null = null;
  private readonly subscriptions = new Subscription();

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly router: Router,
    private readonly studentAssessmentsService: StudentAssessmentsService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly session: Session
  ) {}

  ngOnInit(): void {
    this.attemptId = this.activatedRoute.snapshot.paramMap.get('attemptId');
    if (!this.attemptId) {
      this.loadStudentResultsList();
      return;
    }

    const navigationState = window.history.state as AttemptResultNavigationState | undefined;
    const shouldPoll = navigationState?.pollForResult === true;
    this.loadAttemptResult(shouldPoll ? 8 : 1);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get hasAttemptResult(): boolean {
    return !!this.result;
  }

  /** True when the assessment is configured to not release results immediately. */
  get isPendingNotification(): boolean {
    return this.result?.showResultsImmediately === false;
  }

  get completedOnLabel(): string {
    const completedAt = this.result?.submittedAt ?? this.result?.generatedAt;
    if (!completedAt) {
      return 'Completed recently';
    }

    return `Completed on ${new Date(completedAt).toLocaleDateString(undefined, {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    })}`;
  }

  get durationLabel(): string {
    const durationMinutes = this.result?.durationMinutes ?? 0;
    return durationMinutes > 0 ? `Duration: ${durationMinutes} mins` : 'Duration unavailable';
  }

  get scoreProgress(): number {
    if (!this.result?.totalMarks) {
      return 0;
    }

    return Math.max(0, Math.min(100, (this.result.obtainedMarks / this.result.totalMarks) * 100));
  }

  get filteredQuestionResults(): StudentResultQuestionResult[] {
    const questionResults = this.result?.questionResults ?? [];
    if (this.activeFilter === 'ALL') {
      return questionResults;
    }

    return questionResults.filter(item => item.status === this.activeFilter);
  }

  get rankSummary(): string {
    if (!this.result) {
      return '--';
    }

    const participantCount = Math.max(this.result.participantCount, this.result.rank);
    return `${this.result.rank} / ${participantCount}`;
  }

  get percentileSummary(): string {
    if (!this.result || this.result.participantCount <= 0) {
      return 'Rank updates as more submissions come in';
    }

    const percentile = Math.max(
      1,
      Math.round(((this.result.participantCount - this.result.rank + 1) / this.result.participantCount) * 100)
    );
    return `Top ${percentile}% of participants`;
  }

  setFilter(filter: ResultFilter): void {
    this.activeFilter = filter;
  }

  trackByQuestionId(_: number, item: StudentResultQuestionResult): string {
    return item.questionId;
  }

  trackByResultId(_: number, item: StudentResult): string {
    return item.resultId;
  }

  getCompletedLabel(item: StudentResult): string {
    const completedAt = item.submittedAt ?? item.generatedAt;
    return new Date(completedAt).toLocaleDateString(undefined, {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  }

  getResultStatusBadgeClass(status: string): string {
    const normalized = status?.toUpperCase();
    if (normalized === 'PASS') {
      return 'status-completed';
    }

    if (normalized === 'FAIL') {
      return 'status-failed';
    }

    return 'status-pending';
  }

  viewResultDetail(attemptId: string): void {
    void this.router.navigateByUrl(`/${RouteAddress.Student.AttemptResults}/${attemptId}`);
  }

  getStatusLabel(status: string): string {
    return status.replace(/_/g, ' ');
  }

  getJoinedAnswers(values: string[] | null | undefined, fallback = 'Not answered'): string {
    if (!values?.length) {
      return fallback;
    }

    return values.join(', ');
  }

  returnToDashboard(): void {
    void this.router.navigateByUrl(`/${RouteAddress.Student.Dashboard}`);
  }

  returnToAssessments(): void {
    void this.router.navigateByUrl(`/${RouteAddress.Student.MyAssessments}`);
  }

  private loadStudentResultsList(): void {
    this.isListMode = true;
    this.isLoading = true;
    this.errorMessage = '';

    const studentId = this.session.userId;
    if (!studentId) {
      this.isLoading = false;
      this.errorMessage = 'Unable to determine your student account. Please log in again.';
      return;
    }

    const listSubscription = this.studentAssessmentsService
      .getStudentResults(studentId)
      .pipe(finalize(() => {
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: results => {
          this.resultsList = results ?? [];
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          console.error('Failed to load student results list.', error);
          this.errorMessage = error?.error?.message || 'Unable to load your results right now.';
          this.changeDetectorRef.detectChanges();
        }
      });

    this.subscriptions.add(listSubscription);
  }

  private loadAttemptResult(remainingPolls: number): void {
    if (!this.attemptId) {
      this.errorMessage = 'Assessment attempt context is unavailable.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const resultSubscription = this.studentAssessmentsService
      .getStudentAttemptResult(this.attemptId)
      .pipe(finalize(() => {
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: result => {
          this.result = result;
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          const errorStatus = error?.status ?? 0;
          if (errorStatus === 404 && remainingPolls > 1) {
            this.isLoading = true;
            this.changeDetectorRef.detectChanges();
            window.setTimeout(() => this.loadAttemptResult(remainingPolls - 1), 1500);
            return;
          }

          if (errorStatus === 404) {
            this.errorMessage = 'Your result is not available yet. Please check back in a moment.';
            this.changeDetectorRef.detectChanges();
            return;
          }

          this.errorMessage = error?.error?.message || 'Unable to load this assessment result right now.';
          this.changeDetectorRef.detectChanges();
        }
      });

    this.subscriptions.add(resultSubscription);
  }
}
