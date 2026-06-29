import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { RouteAddress } from '../../../common/constants/routes.constants';
import {
  StudentAssessmentItem,
  StudentAssessmentsService,
  StudentStreak
} from '../../../common/services/api/student-assessments.service';
import { Session } from '../../../common/services/session/session.service';

@Component({
  selector: 'app-student-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private static readonly accents: ReadonlyArray<'blue' | 'gold' | 'cyan'> = ['blue', 'gold', 'cyan'];

  userName = '';
  firstName = '';
  readonly routeAddress = RouteAddress;
  upcomingAssessments: StudentAssessmentItem[] = [];
  streak: StudentStreak | null = null;
  isLoadingAssessments = false;
  private readonly subscriptions = new Subscription();

  constructor(
    private readonly session: Session,
    private readonly studentAssessmentsService: StudentAssessmentsService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const user = this.session.user;
    const claimName = this.resolveNameFromTokenClaims();
    this.userName = claimName || (user ? `${user.firstName} ${user.lastName}`.trim() : '');
    this.firstName = this.userName.split(' ')[0] ?? '';

    this.loadUpcomingAssessments();
    this.loadStreak();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get currentStreakDays(): number {
    return this.streak?.currentStreak ?? 0;
  }

  trackByAssessmentId(_: number, assessment: StudentAssessmentItem): string {
    return assessment.assessmentId;
  }

  getAccentClass(index: number): string {
    return `assessment-item-${DashboardComponent.accents[index % DashboardComponent.accents.length]}`;
  }

  getAssessmentContext(assessment: StudentAssessmentItem): string {
    const context = [assessment.subjectName, assessment.topicName]
      .map(value => value?.trim())
      .filter((value): value is string => !!value)
      .join(' - ');

    return context || this.getStatusLabel(assessment.assessmentStatus);
  }

  getStatusLabel(status: string): string {
    switch (status?.toUpperCase()) {
      case 'LIVE':
        return 'Live Now';
      case 'SCHEDULED':
        return 'Upcoming';
      default:
        return status;
    }
  }

  getScheduleLabel(assessment: StudentAssessmentItem): string {
    if (!assessment.startDateTime) {
      return 'Schedule unavailable';
    }

    return new Date(assessment.startDateTime).toLocaleString(undefined, {
      day: 'numeric',
      month: 'short',
      hour: 'numeric',
      minute: '2-digit'
    });
  }

  getDurationLabel(assessment: StudentAssessmentItem): string {
    return `${assessment.durationMinutes} mins duration`;
  }

  getActionLabel(status: string): string {
    return status?.toUpperCase() === 'LIVE' ? 'Start Assessment' : 'View Details';
  }

  goToAssessments(): void {
    void this.router.navigateByUrl(`/${RouteAddress.Student.MyAssessments}`);
  }

  private loadUpcomingAssessments(): void {
    this.isLoadingAssessments = true;

    const subscription = this.studentAssessmentsService
      .getAssessments(['LIVE', 'SCHEDULED'])
      .pipe(finalize(() => {
        this.isLoadingAssessments = false;
        this.changeDetectorRef.detectChanges();
      }))
      .subscribe({
        next: assessments => {
          this.upcomingAssessments = assessments;
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          console.error('Failed to load upcoming assessments for the dashboard.', error);
        }
      });

    this.subscriptions.add(subscription);
  }

  private loadStreak(): void {
    const subscription = this.studentAssessmentsService.getStudentStreak().subscribe({
      next: streak => {
        this.streak = streak;
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        console.error('Failed to load student streak for the dashboard.', error);
      }
    });

    this.subscriptions.add(subscription);
  }

  private resolveNameFromTokenClaims(): string {
    const token = this.session.jwtToken;
    if (!token) {
      return '';
    }

    const payload = token.split('.')[1];
    if (!payload) {
      return '';
    }

    try {
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const decoded = atob(normalized.padEnd(normalized.length + ((4 - normalized.length % 4) % 4), '='));
      const claims = JSON.parse(decoded) as Record<string, unknown>;
      const firstName = this.readClaim(claims, ['firstName', 'given_name', 'givenname']);
      const lastName = this.readClaim(claims, ['lastName', 'family_name', 'surname']);
      const fullName = this.readClaim(claims, ['name', 'unique_name']);

      const joined = [firstName, lastName].filter(Boolean).join(' ').trim();
      return joined || fullName;
    } catch {
      return '';
    }
  }

  private readClaim(claims: Record<string, unknown>, keys: string[]): string {
    const match = Object.entries(claims).find(([key, value]) => {
      return typeof value === 'string' && keys.some((candidate) => key.toLowerCase().endsWith(candidate.toLowerCase()));
    });

    return typeof match?.[1] === 'string' ? match[1].trim() : '';
  }
}
