import { Component, OnDestroy, OnInit } from '@angular/core';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { CollegeAdminService } from '../../../common/services/api/college-admin.service';
import { AuthSessionService } from '../../../common/services/session/auth-session.service';
import { Session } from '../../../common/services/session/session.service';
import { Subscription } from 'rxjs';

interface CollegeAdminNavItem {
  label: string;
  route: string;
  icon: string;
  badge?: string | null;
}

@Component({
  selector: 'app-college-admin-shell',
  standalone: false,
  templateUrl: './college-admin-shell.component.html',
  styleUrl: './college-admin-shell.component.scss'
})
export class CollegeAdminShellComponent implements OnInit, OnDestroy {
  readonly navItems: CollegeAdminNavItem[] = [
    { label: 'Dashboard', route: `/${RouteAddress.CollegeAdmin.Dashboard}`, icon: 'space_dashboard' },
    { label: 'User Management', route: `/${RouteAddress.CollegeAdmin.Users}`, icon: 'groups_2', badge: null },
    { label: 'Classes Management', route: `/${RouteAddress.CollegeAdmin.ClassesManagement}`, icon: 'account_tree' },
    { label: 'Assessment Builder', route: `/${RouteAddress.CollegeAdmin.AssessmentBuilder}`, icon: 'assignment' },
    { label: 'Reports', route: `/${RouteAddress.CollegeAdmin.Reports}`, icon: 'bar_chart' }
  ];

  readonly supportItems: CollegeAdminNavItem[] = [
    { label: 'Help Center', route: `/${RouteAddress.CollegeAdmin.Reports}`, icon: 'help_outline' },
    { label: 'Settings', route: `/${RouteAddress.CollegeAdmin.Settings}`, icon: 'settings' }
  ];
  private readonly subscriptions = new Subscription();

  constructor(
    private readonly collegeAdminService: CollegeAdminService,
    private readonly authSessionService: AuthSessionService,
    private readonly session: Session) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.collegeAdminService.pendingUsers$.subscribe(users => {
        const pendingCount = users.filter(user => user.role === 'Student' || user.role === 'Trainer').length;
        this.updateUserManagementBadge(pendingCount);
      })
    );

    this.subscriptions.add(
      this.collegeAdminService.getPendingUsers().subscribe({
        error: () => {
          this.updateUserManagementBadge(0);
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get institutionName(): string {
    return this.session.user?.collegeName?.trim() || 'Institution';
  }

  logout(): void {
    this.authSessionService.logout();
  }

  private updateUserManagementBadge(pendingCount: number): void {
    const userManagementItem = this.navItems.find(item => item.route === `/${RouteAddress.CollegeAdmin.Users}`);
    if (!userManagementItem) {
      return;
    }

    userManagementItem.badge = pendingCount > 0 ? `${pendingCount}` : null;
  }
}
