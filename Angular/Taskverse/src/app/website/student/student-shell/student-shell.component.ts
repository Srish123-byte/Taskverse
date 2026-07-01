import { Component } from '@angular/core';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { AuthSessionService } from '../../../common/services/session/auth-session.service';

interface StudentNavItem {
  label: string;
  route: string;
  icon: string;
  iconPath: string;
}

@Component({
  selector: 'app-student-shell',
  standalone: false,
  templateUrl: './student-shell.component.html',
  styleUrl: './student-shell.component.scss'
})
export class StudentShellComponent {
  readonly navItems: StudentNavItem[] = [
    { label: 'Dashboard', route: `/${RouteAddress.Student.Dashboard}`, icon: 'dashboard', iconPath: 'assets/icons/nav/dashboard.svg' },
    { label: 'My Assessments', route: `/${RouteAddress.Student.MyAssessments}`, icon: 'assignment', iconPath: 'assets/icons/nav/assessments.svg' },
    { label: 'Results', route: `/${RouteAddress.Student.Results}`, icon: 'analytics', iconPath: 'assets/icons/nav/reports.svg' },
    { label: 'My Reports', route: `/${RouteAddress.Student.Reports}`, icon: 'bar_chart', iconPath: 'assets/icons/nav/reports.svg' }
  ];
  readonly routeAddress = RouteAddress;

  constructor(private readonly authSessionService: AuthSessionService) {}

  logout(): void {
    this.authSessionService.confirmLogout();
  }
}
