import { Component } from '@angular/core';
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
    { label: 'Dashboard', route: 'dashboard', icon: 'dashboard', iconPath: 'assets/icons/nav/dashboard.svg' },
    { label: 'Courses',   route: 'courses',   icon: 'menu_book', iconPath: 'assets/icons/nav/courses.svg' },
    { label: 'Tasks',     route: 'tasks',     icon: 'task_alt',  iconPath: 'assets/icons/nav/tasks.svg' },
    { label: 'Manage',    route: 'manage',    icon: 'tune',      iconPath: 'assets/icons/nav/manage.svg' }
  ];

  constructor(private readonly authSessionService: AuthSessionService) {}

  logout(): void {
    this.authSessionService.logout();
  }
}
