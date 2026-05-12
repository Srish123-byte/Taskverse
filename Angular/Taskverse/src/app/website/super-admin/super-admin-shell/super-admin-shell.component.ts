import { Component } from '@angular/core';
import { AuthSessionService } from '../../../common/services/session/auth-session.service';

interface SuperAdminNavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-super-admin-shell',
  standalone: false,
  templateUrl: './super-admin-shell.component.html',
  styleUrl: './super-admin-shell.component.scss'
})
export class SuperAdminShellComponent {
  constructor(private readonly authSessionService: AuthSessionService) {}

  readonly navItems: SuperAdminNavItem[] = [
    { label: 'Dashboard', route: 'dashboard', icon: 'dashboard' },
    { label: 'Colleges', route: 'colleges', icon: 'school' },
    { label: 'Users', route: 'users', icon: 'groups' },
    { label: 'Analytics', route: 'analytics', icon: 'query_stats' },
    { label: 'Assessments', route: 'assessments', icon: 'assignment' },
    { label: 'Settings', route: 'settings', icon: 'settings' }
  ];

  logout(): void {
    this.authSessionService.logout();
  }
}
