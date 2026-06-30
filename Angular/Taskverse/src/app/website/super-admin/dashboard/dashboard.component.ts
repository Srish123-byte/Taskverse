import { Component, OnInit } from '@angular/core';
import { Session } from '../../../common/services/session/session.service';
import { SuperAdminDashboard } from '../../../common/models/super-admin.model';
import { SuperAdminService } from '../../../common/services/api/super-admin.service';

@Component({
  selector: 'app-super-admin-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  userName = '';
  dashboard: SuperAdminDashboard | null = null;
  isLoading = false;
  errorMessage = '';

  constructor(
    private readonly session: Session,
    private readonly superAdminService: SuperAdminService
  ) {}

  ngOnInit(): void {
    const user = this.session.user;
    this.userName = user ? `${user.firstName} ${user.lastName}`.trim() : '';
    this.loadDashboard();
  }

  private loadDashboard(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.superAdminService.getDashboard().subscribe({
      next: dashboard => {
        this.dashboard = dashboard;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load super admin dashboard data right now.';
        this.isLoading = false;
      }
    });
  }
}
