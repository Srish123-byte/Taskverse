import { Component } from '@angular/core';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { AuthSessionService } from '../../../common/services/session/auth-session.service';
import { Session } from '../../../common/services/session/session.service';

interface CollegeAdminNavItem {
  label: string;
  route: string;
  icon: string;
  badge?: string;
}

@Component({
  selector: 'app-college-admin-shell',
  standalone: false,
  templateUrl: './college-admin-shell.component.html',
  styleUrl: './college-admin-shell.component.scss'
})
export class CollegeAdminShellComponent {
  readonly navItems: CollegeAdminNavItem[] = [
    { label: 'Dashboard', route: `/${RouteAddress.CollegeAdmin.Dashboard}`, icon: 'space_dashboard' },
    { label: 'User Approvals', route: `/${RouteAddress.CollegeAdmin.Approvals}`, icon: 'how_to_reg', badge: '12' },
    { label: 'User Management', route: `/${RouteAddress.CollegeAdmin.Users}`, icon: 'groups_2' },
    { label: 'Classes Management', route: `/${RouteAddress.CollegeAdmin.ClassesManagement}`, icon: 'account_tree' },
    { label: 'Assessment Builder', route: `/${RouteAddress.CollegeAdmin.AssessmentBuilder}`, icon: 'assignment' },
    { label: 'Reports', route: `/${RouteAddress.CollegeAdmin.Reports}`, icon: 'bar_chart' }
  ];

  readonly supportItems: CollegeAdminNavItem[] = [
    { label: 'Help Center', route: `/${RouteAddress.CollegeAdmin.Reports}`, icon: 'help_outline' },
    { label: 'Settings', route: `/${RouteAddress.CollegeAdmin.Settings}`, icon: 'settings' }
  ];

  constructor(
    private readonly authSessionService: AuthSessionService,
    private readonly session: Session) {}

  get institutionName(): string {
    return this.session.user?.collegeName?.trim() || 'Institution';
  }

  logout(): void {
    this.authSessionService.logout();
  }
}
