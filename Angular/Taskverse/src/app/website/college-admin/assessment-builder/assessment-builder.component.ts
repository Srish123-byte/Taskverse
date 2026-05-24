import { Component } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { AssessmentAdminService } from '../../../common/services/api/assessment-admin.service';

@Component({
  selector: 'app-college-admin-assessment-builder',
  standalone: false,
  templateUrl: './assessment-builder.component.html',
  styleUrl: './assessment-builder.component.scss'
})
export class AssessmentBuilderComponent {
  assessmentId = '';
  isDeleting = false;
  successMessage = '';
  errorMessage = '';

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
          this.successMessage = 'Assessment soft deleted. It will stay recoverable for 30 days by SuperAdmin only.';
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
