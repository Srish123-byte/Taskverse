import { Component, OnInit } from '@angular/core';
import { ReportStudent, ReportsService } from '../../../common/services/api/reports.service';
import { StudentAssessmentsService, StudentResult } from '../../../common/services/api/student-assessments.service';
import { Session } from '../../../common/services/session/session.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-student-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  isExportingPdf = false;
  isExportingExcel = false;
  hasAssessments = false;
  activeView: string | null = null; // 'student' | null
  student: ReportStudent | null = null;
  results: StudentResult[] = [];

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
    private studentAssessmentsService: StudentAssessmentsService,
    private session: Session,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    const userId = this.session.userId;
    if (!userId) {
      this.snackBar.open('Unable to resolve student session.', 'Close', ReportsComponent.errorSnackBarConfig);
      return;
    }

    this.studentAssessmentsService.getStudentResults(userId).subscribe({
      next: (results) => {
        this.results = results ?? [];
        this.hasAssessments = this.results.length > 0;
        const user = this.session.user;
        this.student = {
          studentId: userId,
          userId,
          collegeId: this.session.collegeId,
          fullName: user ? `${user.firstName} ${user.lastName}`.trim() : 'Student',
          email: user?.email ?? this.session.userEmail,
          averagePercentage: this.averagePercentage,
          assessmentCount: this.results.length
        };
      },
      error: (err) => {
        console.error('Unable to load student results:', err);
        this.snackBar.open('Unable to load your report data.', 'Close', ReportsComponent.errorSnackBarConfig);
      }
    });
  }

  exportReport(format: 'pdf' | 'excel'): void {
    const studentId = this.student?.studentId;
    if (!studentId) {
      this.snackBar.open('Unable to resolve student session.', 'Close', ReportsComponent.errorSnackBarConfig);
      return;
    }

    if (format === 'pdf') this.isExportingPdf = true;
    else this.isExportingExcel = true;

    const request$ = this.reportsService.exportStudentReport(studentId, format);
    const filename = `StudentReport_${new Date().getTime()}.${format === 'excel' ? 'xlsx' : 'pdf'}`;

    request$.pipe(
      finalize(() => {
        if (format === 'pdf') this.isExportingPdf = false;
        else this.isExportingExcel = false;
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

  viewDetailedReport(): void {
    this.activeView = 'student';
  }
  
  closeView(): void {
    this.activeView = null;
  }

  // Email Modal State
  isEmailModalOpen = false;
  emailFormat: 'pdf' | 'excel' = 'pdf';
  emailAddress: string = '';
  isSendingEmail = false;

  openEmailModal(format: 'pdf' | 'excel'): void {
    const studentId = this.student?.studentId;
    if (!studentId) {
      this.snackBar.open('Unable to resolve student session.', 'Close', ReportsComponent.errorSnackBarConfig);
      return;
    }
    
    this.emailFormat = format;
    this.emailAddress = '';
    this.isEmailModalOpen = true;
  }

  closeEmailModal(): void {
    this.isEmailModalOpen = false;
  }

  sendEmail(): void {
    if (this.emailRecipients.length === 0) return;
    const studentId = this.student?.studentId;
    if (!studentId) return;

    this.isSendingEmail = true;
    this.reportsService.emailReport({
      targetEmails: this.emailRecipients,
      reportType: 'student',
      entityId: studentId,
      format: this.emailFormat
    }).pipe(
      finalize(() => {
        this.isSendingEmail = false;
        this.closeEmailModal();
      })
    ).subscribe({
      next: () => {
        this.snackBar.open('Report emailed successfully!', 'Close', ReportsComponent.snackBarConfig);
      },
      error: (err) => {
        console.error('Email failed:', err);
        this.snackBar.open('Failed to email report.', 'Close', ReportsComponent.errorSnackBarConfig);
      }
    });
  }

  get emailRecipients(): string[] {
    return this.emailAddress
      .split(',')
      .map(email => email.trim())
      .filter(email => !!email);
  }

  get averagePercentage(): number {
    return this.results.length
      ? this.results.reduce((sum, result) => sum + result.percentage, 0) / this.results.length
      : 0;
  }

  get highestPercentage(): number {
    return this.results.length
      ? Math.max(...this.results.map(result => result.percentage))
      : 0;
  }

  get passCount(): number {
    return this.results.filter(result => result.resultStatus?.toLowerCase() === 'pass').length;
  }
}
