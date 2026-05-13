import { Component } from '@angular/core';
import { RouteAddress } from '../../../common/constants/routes.constants';
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
    { label: 'Dashboard', route: `/${RouteAddress.SuperAdmin.Dashboard}`, icon: 'dashboard' },
    { label: 'Colleges', route: `/${RouteAddress.SuperAdmin.Colleges}`, icon: 'school' },
    { label: 'Users', route: `/${RouteAddress.SuperAdmin.Users}`, icon: 'groups' },
    { label: 'Analytics', route: `/${RouteAddress.SuperAdmin.Analytics}`, icon: 'query_stats' },
    { label: 'Assessments', route: `/${RouteAddress.SuperAdmin.Assessments}`, icon: 'assignment' },
    { label: 'Settings', route: `/${RouteAddress.SuperAdmin.Settings}`, icon: 'settings' }
  ];

  logout(): void {
    this.authSessionService.logout();
  }
}
