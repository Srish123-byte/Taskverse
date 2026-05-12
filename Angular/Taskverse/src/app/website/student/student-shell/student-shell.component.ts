import { Component } from '@angular/core';
import { AuthSessionService } from '../../../common/services/session/auth-session.service';

interface StudentNavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-student-shell',
  standalone: false,
  templateUrl: './student-shell.component.html',
  styleUrl: './student-shell.component.scss'
})
export class StudentShellComponent {
  readonly navItems: StudentNavItem[] = [
    { label: 'Dashboard', route: 'dashboard', icon: 'dashboard' },
    { label: 'Courses', route: 'courses', icon: 'menu_book' },
    { label: 'Tasks', route: 'tasks', icon: 'task_alt' },
    { label: 'Manage', route: 'manage', icon: 'tune' }
  ];

  constructor(private readonly authSessionService: AuthSessionService) {}

  logout(): void {
    this.authSessionService.logout();
  }
}
