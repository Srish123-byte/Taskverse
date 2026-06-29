import { Component } from '@angular/core';
import { AuthSessionService } from '../../../common/services/session/auth-session.service';

interface TrainerNavItem {
  label: string;
  route: string;
  icon: string;
  iconPath: string;
}

@Component({
  selector: 'app-trainer-shell',
  standalone: false,
  templateUrl: './trainer-shell.component.html',
  styleUrl: './trainer-shell.component.scss'
})
export class TrainerShellComponent {
  readonly navItems: TrainerNavItem[] = [
    { label: 'Dashboard',   route: 'dashboard',   icon: 'dashboard',   iconPath: 'assets/icons/nav/dashboard.svg' },
    { label: 'Courses',     route: 'courses',     icon: 'menu_book',   iconPath: 'assets/icons/nav/courses.svg' },
    { label: 'Students',    route: 'students',    icon: 'groups',      iconPath: 'assets/icons/nav/students.svg' },
    { label: 'Question Bank', route: 'questions-management', icon: 'quiz', iconPath: 'assets/icons/nav/tasks.svg' },
    { label: 'Assessments Management', route: 'assessments-management', icon: 'assignment', iconPath: 'assets/icons/nav/assessment-builder.svg' },
    { label: 'Reports',     route: 'reports',     icon: 'summarize',   iconPath: 'assets/icons/nav/reports.svg' },
    { label: 'Manage',      route: 'manage',      icon: 'tune',        iconPath: 'assets/icons/nav/manage.svg' },
    { label: 'Help Center', route: 'help-center', icon: 'help_center', iconPath: 'assets/icons/nav/help-center.svg' }
  ];

  constructor(private readonly authSessionService: AuthSessionService) {}

  logout(): void {
    this.authSessionService.confirmLogout();
  }
}
