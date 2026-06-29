import { Component } from '@angular/core';
import { ReportsService } from '../../../common/services/api/reports.service';
import { Session } from '../../../common/services/session/session.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-college-admin-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent {
  isExportingPdf = false;
  isExportingExcel = false;
  activeReportType: string | null = null;

  selectedBranchId: string = '00000000-0000-0000-0000-000000000000';
  selectedStudentId: string = '00000000-0000-0000-0000-000000000000';

  private static readonly snackBarConfig = {
    duration: 3000,
    horizontalPosition: 'right' as const,
    verticalPosition: 'top' as const,
    panelClass: ['question-editor-success-snackbar']
  };

  private static readonly errorSnackBarConfig = {
    ...ReportsComponent.snackBarConfig,
    panelClass: ['question-bank-restriction-snackbar']
  };

  constructor(
    private reportsService: ReportsService,
    private session: Session,
    private snackBar: MatSnackBar
  ) {}

  exportReport(type: 'branch' | 'student', format: 'pdf' | 'excel'): void {
    let request$;
    let filename = '';

    if (type === 'branch') {
      if (!this.selectedBranchId) {
        this.snackBar.open('Please select a branch first.', 'Close', ReportsComponent.errorSnackBarConfig);
        return;
      }
      request$ = this.reportsService.exportBranchReport(this.selectedBranchId, format);
      filename = `BranchReport_${new Date().getTime()}.${format === 'excel' ? 'xlsx' : 'pdf'}`;
    } else if (type === 'student') {
      if (!this.selectedStudentId) {
        this.snackBar.open('Please select a student first.', 'Close', ReportsComponent.errorSnackBarConfig);
        return;
      }
      request$ = this.reportsService.exportStudentReport(this.selectedStudentId, format);
      filename = `StudentReport_${new Date().getTime()}.${format === 'excel' ? 'xlsx' : 'pdf'}`;
    }

    if (!request$) return;

    if (format === 'pdf') this.isExportingPdf = true;
    else this.isExportingExcel = true;
    this.activeReportType = type;

    request$.pipe(
      finalize(() => {
        if (format === 'pdf') this.isExportingPdf = false;
        else this.isExportingExcel = false;
        this.activeReportType = null;
      })
    ).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.click();
        window.URL.revokeObjectURL(url);
        this.snackBar.open(`${format.toUpperCase()} exported successfully!`, 'Close', ReportsComponent.snackBarConfig);
      },
      error: (err) => {
        console.error('Export failed:', err);
        this.snackBar.open('Failed to export report. Please try again.', 'Close', ReportsComponent.errorSnackBarConfig);
      }
    });
  }

  viewReport(type: 'branch' | 'student'): void {
    this.snackBar.open(`Viewing ${type} report directly in the browser is coming soon. Use PDF/Excel exports for now.`, 'Close', ReportsComponent.snackBarConfig);
  }
}
