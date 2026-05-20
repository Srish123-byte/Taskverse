import { Component, OnDestroy, OnInit } from '@angular/core';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { CollegeAdminService } from '../../../common/services/api/college-admin.service';
import { Session } from '../../../common/services/session/session.service';
import { Subscription } from 'rxjs';

interface DashboardMetricCard {
  label: string;
  value: string;
  caption: string;
  icon: string;
  accent: 'blue' | 'gold' | 'sky' | 'action';
}

interface RecentAssessment {
  title: string;
  subtitle: string;
  department: string;
  status: 'Upcoming' | 'Live' | 'Completed';
  progress: string;
}

interface BatchRanking {
  rank: string;
  name: string;
  average: string;
  trend: 'up' | 'down';
  tone: 'top' | 'alert';
}

@Component({
  selector: 'app-college-admin-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  readonly routeAddress = RouteAddress;
  userName = '';
  isLoading = false;
  private readonly subscriptions = new Subscription();

  readonly metricCards: DashboardMetricCard[] = [
    { label: 'Total Students', value: '4,829', caption: '+12% this month', icon: 'groups', accent: 'blue' },
    { label: 'Assessments Pending', value: '28', caption: '142 created', icon: 'assignment', accent: 'gold' },
    { label: 'Avg. Performance', value: '76.4%', caption: 'Stable completion trend', icon: 'insights', accent: 'sky' },
    { label: 'Pending Approvals', value: '12', caption: 'New requests today: 5', icon: 'verified_user', accent: 'action' }
  ];

  readonly recentAssessments: RecentAssessment[] = [
    {
      title: 'Q3 Midterm - Data Structures',
      subtitle: 'Scheduled for Oct 24, 2024',
      department: 'CS & Engineering',
      status: 'Upcoming',
      progress: '0/420'
    },
    {
      title: 'Cybersecurity Ethics Final',
      subtitle: 'Active since 09:00 AM',
      department: 'IT Management',
      status: 'Live',
      progress: '312/350'
    },
    {
      title: 'Advanced Calculus Quiz',
      subtitle: 'Ended Oct 20, 2024',
      department: 'Applied Sciences',
      status: 'Completed',
      progress: '100%'
    },
    {
      title: 'Modern World History',
      subtitle: 'Ended Oct 18, 2024',
      department: 'Humanities',
      status: 'Completed',
      progress: '100%'
    },
    {
      title: 'Intro to Macroeconomics',
      subtitle: 'Scheduled for Oct 28, 2024',
      department: 'Economics',
      status: 'Upcoming',
      progress: '0/180'
    }
  ];

  readonly topBatches: BatchRanking[] = [
    { rank: '01', name: 'Batch B.Tech CS-A', average: 'Avg: 92.4%', trend: 'up', tone: 'top' },
    { rank: '02', name: 'Batch MBA Finance', average: 'Avg: 88.1%', trend: 'up', tone: 'top' }
  ];

  readonly criticalBatches: BatchRanking[] = [
    { rank: '18', name: 'Batch B.Sc Math-C', average: 'Avg: 42.6%', trend: 'down', tone: 'alert' },
    { rank: '17', name: 'Batch B.Arch 2nd Yr', average: 'Avg: 48.9%', trend: 'down', tone: 'alert' }
  ];

  constructor(
    private readonly collegeAdminService: CollegeAdminService,
    private readonly session: Session
  ) {}

  ngOnInit(): void {
    this.isLoading = true;
    const user = this.session.user;
    this.userName = user ? `${user.firstName} ${user.lastName}` : '';
    this.subscriptions.add(
      this.collegeAdminService.pendingUsers$.subscribe(users => {
        const pendingUsers = users.filter(item => item.role === 'Student' || item.role === 'Trainer');
        this.pendingApprovals.value = `${pendingUsers.length}`;
        this.pendingApprovals.caption = pendingUsers.length > 0
          ? 'Students and trainers awaiting review'
          : 'No pending student or trainer approvals';
      })
    );
    window.setTimeout(() => {
      this.isLoading = false;
    }, 400);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get pendingApprovals(): DashboardMetricCard {
    return this.metricCards[3];
  }
}
