import { Component, OnInit } from '@angular/core';
import { SuperAdminDashboard } from '../../../common/models/super-admin.model';
import { SuperAdminService } from '../../../common/services/api/super-admin.service';

@Component({
  selector: 'app-super-admin-analytics',
  standalone: false,
  templateUrl: './analytics.component.html',
  styleUrl: './analytics.component.scss'
})
export class AnalyticsComponent implements OnInit {
  dashboard: SuperAdminDashboard | null = null;
  isLoading = false;
  errorMessage = '';

  constructor(private readonly superAdminService: SuperAdminService) {}

  ngOnInit(): void {
    this.isLoading = true;
    this.superAdminService.getDashboard().subscribe({
      next: dashboard => {
        this.dashboard = dashboard;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load analytics right now.';
        this.isLoading = false;
      }
    });
  }

  get studentsAssessed(): number {
    return this.dashboard?.averageScoresByCollege.reduce((total, item) => total + item.studentsAssessed, 0) ?? 0;
  }
}
