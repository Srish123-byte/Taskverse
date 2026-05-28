import { Component } from '@angular/core';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { AuthSessionService } from '../../../common/services/session/auth-session.service';

interface SuperAdminNavItem {
  label: string;
  route: string;
  icon: string;
  iconPath: string;
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
    { label: 'Dashboard',   route: `/${RouteAddress.SuperAdmin.Dashboard}`,   icon: 'dashboard',   iconPath: 'assets/icons/nav/dashboard.svg' },
    { label: 'Colleges',    route: `/${RouteAddress.SuperAdmin.Colleges}`,    icon: 'school',      iconPath: 'assets/icons/nav/colleges.svg' },
    { label: 'Users',       route: `/${RouteAddress.SuperAdmin.Users}`,       icon: 'groups',      iconPath: 'assets/icons/nav/users.svg' },
    { label: 'Analytics',   route: `/${RouteAddress.SuperAdmin.Analytics}`,   icon: 'query_stats', iconPath: 'assets/icons/nav/analytics.svg' },
    { label: 'Assessments', route: `/${RouteAddress.SuperAdmin.Assessments}`, icon: 'assignment',  iconPath: 'assets/icons/nav/assessments.svg' },
    { label: 'Settings',    route: `/${RouteAddress.SuperAdmin.Settings}`,    icon: 'settings',    iconPath: 'assets/icons/nav/settings.svg' }
  ];

  logout(): void {
    this.authSessionService.confirmLogout();
  }
}
