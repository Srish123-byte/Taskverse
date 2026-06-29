import { Component, OnInit } from '@angular/core';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { CollegeAdminService, CollegeAdminDashboardData } from '../../../common/services/api/college-admin.service';
import { AssessmentAdminService } from '../../../common/services/api/assessment-admin.service';
import { Session } from '../../../common/services/session/session.service';
import { forkJoin } from 'rxjs';

interface MetricCard {
  label: string;
  value: string;
  icon: string;
  accent: string;
  link?: string;
  linkLabel?: string;
}

interface AssessmentItem {
  title: string;
  subtitle: string;
  status: string;
}

@Component({
  selector: 'app-college-admin-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  readonly routeAddress = RouteAddress;
  userName = '';
  isLoading = false;

  metricCards: MetricCard[] = [];
  recentAssessments: AssessmentItem[] = [];

  constructor(
    private readonly collegeAdminService: CollegeAdminService,
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly session: Session
  ) {}

  ngOnInit(): void {
    this.isLoading = true;
    const user = this.session.user;
    this.userName = user ? `${user.firstName} ${user.lastName}` : '';
    forkJoin({
      dashboard: this.collegeAdminService.getDashboard(),
      assessments: this.assessmentAdminService.searchAssessments({ pageNumber: 1, pageSize: 5 })
    }).subscribe({
      next: (results) => {
        this.setupCards(results.dashboard);
        this.recentAssessments = results.assessments.items.map(a => ({
          title: a.assessmentName,
          subtitle: a.startDateTime ? `Scheduled: ${new Date(a.startDateTime).toLocaleDateString()}` : '',
          status: a.assessmentStatus
        }));
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  private setupCards(data: CollegeAdminDashboardData): void {
    this.metricCards = [
      { label: 'Total Students', value: `${data.totals.registeredStudents}`, icon: 'groups', accent: 'blue' },
      { label: 'Assessments Pending', value: `${data.totals.assessmentsThisMonth}`, icon: 'assignment', accent: 'gold' },
      { label: 'Pending Approvals', value: `${data.totals.pendingApprovals}`, icon: 'verified_user', accent: 'action', link: '/college-admin/users', linkLabel: 'Review approvals' },
    ];
  }
}
