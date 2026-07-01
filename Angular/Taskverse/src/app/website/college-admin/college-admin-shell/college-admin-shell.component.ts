import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { CollegeAdminService } from '../../../common/services/api/college-admin.service';
import { AuthSessionService } from '../../../common/services/session/auth-session.service';
import { Session } from '../../../common/services/session/session.service';
import { Subscription } from 'rxjs';

interface CollegeAdminNavItem {
  label: string;
  route: string;
  icon: string;
  iconPath: string;
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
    { label: 'Dashboard',           route: `/${RouteAddress.CollegeAdmin.Dashboard}`,         icon: 'space_dashboard',  iconPath: 'assets/icons/nav/dashboard.svg' },
    { label: 'User Management',     route: `/${RouteAddress.CollegeAdmin.Users}`,              icon: 'groups_2',         iconPath: 'assets/icons/nav/user-management.svg', badge: null },
    { label: 'Classes Management',  route: `/${RouteAddress.CollegeAdmin.ClassesManagement}`,  icon: 'account_tree',     iconPath: 'assets/icons/nav/classes.svg' },
    { label: 'Questions Management',route: `/${RouteAddress.CollegeAdmin.QuestionsManagement}`,icon: 'quiz',             iconPath: 'assets/icons/nav/tasks.svg' },
    { label: 'Assessments Management', route: `/${RouteAddress.CollegeAdmin.AssessmentsManagement}`, icon: 'assignment', iconPath: 'assets/icons/nav/assessment-builder.svg' },
    { label: 'Reports',             route: `/${RouteAddress.CollegeAdmin.Reports}`,            icon: 'bar_chart',        iconPath: 'assets/icons/nav/reports.svg' }
  ];

  readonly supportItems: CollegeAdminNavItem[] = [
    { label: 'Help Center', route: `/${RouteAddress.CollegeAdmin.HelpCenter}`, icon: 'help_outline', iconPath: 'assets/icons/nav/help-center.svg' },
    { label: 'Settings',    route: `/${RouteAddress.CollegeAdmin.Settings}`,   icon: 'settings',     iconPath: 'assets/icons/nav/settings.svg' }
  ];
  private readonly subscriptions = new Subscription();

  constructor(
    private readonly collegeAdminService: CollegeAdminService,
    private readonly authSessionService: AuthSessionService,
    private readonly session: Session,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.collegeAdminService.pendingUsers$.subscribe(users => {
        const pendingCount = users.filter(user => user.role === 'Student' || user.role === 'Trainer').length;
        this.updateUserManagementBadge(pendingCount);
        this.changeDetectorRef.detectChanges();
      })
    );

    this.subscriptions.add(
      this.collegeAdminService.getPendingUsers().subscribe({
        next: () => {
          this.changeDetectorRef.detectChanges();
        },
        error: () => {
          this.updateUserManagementBadge(0);
          this.changeDetectorRef.detectChanges();
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

  get showHeaderSearch(): boolean {
    return !this.router.url.includes(`/${RouteAddress.CollegeAdmin.QuestionsManagement}`) &&
      !this.router.url.includes(`/${RouteAddress.CollegeAdmin.AssessmentsManagement}`);
  }

  logout(): void {
    this.authSessionService.confirmLogout();
  }

  private updateUserManagementBadge(pendingCount: number): void {
    const userManagementItem = this.navItems.find(item => item.route === `/${RouteAddress.CollegeAdmin.Users}`);
    if (!userManagementItem) {
      return;
    }

    userManagementItem.badge = pendingCount > 0 ? `${pendingCount}` : null;
  }
}
