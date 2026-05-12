import { Component, OnInit } from '@angular/core';
import { College } from '../../../common/models/super-admin.model';
import { SuperAdminService } from '../../../common/services/api/super-admin.service';

@Component({
  selector: 'app-super-admin-colleges',
  standalone: false,
  templateUrl: './colleges.component.html',
  styleUrl: './colleges.component.scss'
})
export class CollegesComponent implements OnInit {
  colleges: College[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(private readonly superAdminService: SuperAdminService) {}

  ngOnInit(): void {
    this.loadColleges();
  }

  approveCollege(collegeId: string): void {
    this.superAdminService.approveCollege(collegeId, { reason: 'Approved from Super Admin console' }).subscribe({
      next: updated => this.replaceCollege(updated),
      error: () => {
        this.errorMessage = 'Unable to approve college right now.';
      }
    });
  }

  toggleCollegeStatus(college: College): void {
    const request = { reason: college.isActive ? 'Deactivated from Super Admin console' : 'Reactivated from Super Admin console' };
    const action = college.isActive
      ? this.superAdminService.deactivateCollege(college.collegeId, request)
      : this.superAdminService.reactivateCollege(college.collegeId, request);

    action.subscribe({
      next: updated => this.replaceCollege(updated),
      error: () => {
        this.errorMessage = 'Unable to update college status right now.';
      }
    });
  }

  private loadColleges(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.superAdminService.getColleges().subscribe({
      next: colleges => {
        this.colleges = colleges;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load colleges right now.';
        this.isLoading = false;
      }
    });
  }

  private replaceCollege(updated: College): void {
    this.colleges = this.colleges.map(college => college.collegeId === updated.collegeId ? updated : college);
  }
}
