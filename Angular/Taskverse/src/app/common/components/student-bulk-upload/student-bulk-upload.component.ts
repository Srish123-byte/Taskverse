import { ChangeDetectorRef, Component, Input } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BulkStudentUploadResult } from '../../models/super-admin.model';
import { CollegeAdminService } from '../../services/api/college-admin.service';
import { SuperAdminService } from '../../services/api/super-admin.service';
import { ParsedStudentImportFile, StudentImportParserService } from '../../services/utilities/student-import-parser.service';

@Component({
  selector: 'app-student-bulk-upload',
  standalone: false,
  templateUrl: './student-bulk-upload.component.html',
  styleUrl: './student-bulk-upload.component.scss'
})
export class StudentBulkUploadComponent {
  private static readonly successSnackBarConfig = {
    duration: 3500,
    horizontalPosition: 'center' as const,
    verticalPosition: 'top' as const,
    panelClass: ['question-editor-success-snackbar']
  };

  @Input() scope: 'super-admin' | 'college-admin' = 'super-admin';

  isParsing = false;
  isUploading = false;
  errorMessage = '';
  parsedFile: ParsedStudentImportFile | null = null;
  lastResult: BulkStudentUploadResult | null = null;

  constructor(
    private readonly parser: StudentImportParserService,
    private readonly superAdminService: SuperAdminService,
    private readonly collegeAdminService: CollegeAdminService,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  get previewRows() {
    return this.parsedFile?.rows.slice(0, 5) ?? [];
  }

  get isCollegeAdminScope(): boolean {
    return this.scope === 'college-admin';
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';

    if (!file) {
      return;
    }

    this.errorMessage = '';
    this.lastResult = null;
    this.isParsing = true;

    try {
      this.parsedFile = await this.parser.parse(file, this.scope);
    } catch (error) {
      this.parsedFile = null;
      this.errorMessage = error instanceof Error ? error.message : 'Unable to parse the uploaded file.';
    } finally {
      this.isParsing = false;
      this.changeDetectorRef.detectChanges();
    }
  }

  clearSelection(): void {
    this.parsedFile = null;
    this.lastResult = null;
    this.errorMessage = '';
    this.changeDetectorRef.detectChanges();
  }

  upload(): void {
    if (!this.parsedFile || this.isUploading) {
      return;
    }

    this.errorMessage = '';
    this.lastResult = null;
    this.isUploading = true;

    const request = { rows: this.parsedFile.rows };
    const upload$ = this.scope === 'college-admin'
      ? this.collegeAdminService.bulkUploadStudents(request)
      : this.superAdminService.bulkUploadStudents(request);

    upload$.subscribe({
      next: result => {
        this.isUploading = false;
        this.lastResult = result;

        if (result.createdCount > 0) {
          this.snackBar.open(
            `${result.createdCount} student${result.createdCount === 1 ? '' : 's'} created successfully.`,
            'Close',
            StudentBulkUploadComponent.successSnackBarConfig);
        }

        if (result.duplicateCount > 0) {
          this.snackBar.open(
            'Some rows were skipped because the email already exists or was duplicated in the file.',
            'Close',
            StudentBulkUploadComponent.successSnackBarConfig);
        }

        if (!result.summaryEmailSent && result.summaryEmailWarning) {
          this.snackBar.open(
            result.summaryEmailWarning,
            'Close',
            StudentBulkUploadComponent.successSnackBarConfig);
        }

        if (result.createdCount > 0) {
          this.parsedFile = null;
        }

        this.changeDetectorRef.detectChanges();
      },
      error: err => {
        this.isUploading = false;
        this.errorMessage = err?.error?.message || 'Unable to upload these students right now.';
        this.changeDetectorRef.detectChanges();
      }
    });
  }
}
