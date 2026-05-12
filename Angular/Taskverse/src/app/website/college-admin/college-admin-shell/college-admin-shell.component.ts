import { Component } from '@angular/core';
import { AuthSessionService } from '../../../common/services/session/auth-session.service';

interface CollegeAdminNavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-college-admin-shell',
  standalone: false,
  templateUrl: './college-admin-shell.component.html',
  styleUrl: './college-admin-shell.component.scss'
})
export class CollegeAdminShellComponent {
  readonly navItems: CollegeAdminNavItem[] = [
    { label: 'Dashboard', route: 'dashboard', icon: 'dashboard' },
    { label: 'Courses', route: 'courses', icon: 'menu_book' },
    { label: 'Trainers', route: 'trainers', icon: 'co_present' },
    { label: 'Students', route: 'students', icon: 'school' },
    { label: 'Manage', route: 'manage', icon: 'tune' }
  ];

  constructor(private readonly authSessionService: AuthSessionService) {}

  logout(): void {
    this.authSessionService.logout();
  }
}
