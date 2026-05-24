import { Component } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { AssessmentAdminService } from '../../../common/services/api/assessment-admin.service';

@Component({
  selector: 'app-super-admin-assessments',
  standalone: false,
  templateUrl: './assessments.component.html',
  styleUrl: './assessments.component.scss'
})
export class AssessmentsComponent {
  assessmentId = '';
  isDeleting = false;
  successMessage = '';
  errorMessage = '';

  readonly spotlight = [
    { title: 'Assessment Catalog', body: 'Review platform-wide assessment definitions, ownership, and rollout readiness.' },
    { title: 'Completion Monitoring', body: 'Track active sessions, bottlenecks, and drop-off trends across colleges.' },
    { title: 'Policy Controls', body: 'Define approval, archival, and visibility rules before publishing new assessments.' }
  ];

  constructor(private readonly assessmentAdminService: AssessmentAdminService) {}

  deleteAssessment(): void {
    const normalizedAssessmentId = this.assessmentId.trim();
    if (!normalizedAssessmentId || this.isDeleting) {
      return;
    }

    this.isDeleting = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.assessmentAdminService
      .deleteAssessment(normalizedAssessmentId)
      .pipe(finalize(() => (this.isDeleting = false)))
      .subscribe({
        next: () => {
          this.successMessage = 'Assessment soft deleted. Recovery remains available to SuperAdmin for 30 days.';
          this.assessmentId = '';
        },
        error: error => {
          this.errorMessage =
            error?.error?.message ??
            'Unable to delete the assessment right now.';
        }
      });
  }
}
