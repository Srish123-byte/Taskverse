import { Component, OnInit } from '@angular/core';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { Session } from '../../../common/services/session/session.service';

interface UpcomingAssessment {
  title: string;
  subtitle: string;
  schedule: string;
  duration: string;
  accent: 'blue' | 'gold' | 'cyan';
  countdown?: string;
  ctaLabel: string;
  ctaState: 'disabled' | 'ready';
}

interface RecentResult {
  name: string;
  date: string;
  score: string;
  rank: string;
}

interface TrendBar {
  value: number;
  active?: boolean;
}

@Component({
  selector: 'app-student-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  userName = '';
  firstName = '';
  readonly routeAddress = RouteAddress;
  readonly upcomingAssessments: UpcomingAssessment[] = [
    {
      title: 'Python Basics',
      subtitle: 'Computer Science Dept • Final Assessment',
      schedule: 'Starts 10:00 AM',
      duration: '60 mins duration',
      accent: 'blue',
      countdown: '02h 15m 26s',
      ctaLabel: 'Start Assessment',
      ctaState: 'disabled'
    },
    {
      title: 'Data Structures Midterm',
      subtitle: 'Engineering Faculty • Midterm Exam',
      schedule: 'Oct 24, 2:00 PM',
      duration: '90 mins duration',
      accent: 'gold',
      ctaLabel: 'Start Assessment',
      ctaState: 'ready'
    },
    {
      title: 'Advanced Mathematics',
      subtitle: 'Math Lab • Weekly Quiz',
      schedule: 'Oct 26, 9:00 AM',
      duration: '45 mins duration',
      accent: 'cyan',
      ctaLabel: 'Start Assessment',
      ctaState: 'ready'
    }
  ];
  readonly recentResults: RecentResult[] = [
    { name: 'SQL Queries Expert', date: 'Oct 18, 2023', score: '92 / 100', rank: '2nd / 42' },
    { name: 'Operating Systems', date: 'Oct 15, 2023', score: '88 / 100', rank: '5th / 42' },
    { name: 'Statistics Checkpoint', date: 'Oct 12, 2023', score: '95 / 100', rank: '1st / 42' }
  ];
  readonly performanceTrend: TrendBar[] = [
    { value: 42 },
    { value: 58 },
    { value: 54 },
    { value: 72 },
    { value: 84, active: true },
    { value: 79 }
  ];

  constructor(private readonly session: Session) {}

  ngOnInit(): void {
    const user = this.session.user;
    const claimName = this.resolveNameFromTokenClaims();
    this.userName = claimName || (user ? `${user.firstName} ${user.lastName}`.trim() : '');
    this.firstName = this.userName.split(' ')[0] ?? '';
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
