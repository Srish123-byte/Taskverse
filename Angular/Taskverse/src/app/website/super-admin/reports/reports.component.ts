import { Component, OnInit } from '@angular/core';
import { ReportsService } from '../../../common/services/api/reports.service';
import { SuperAdminService } from '../../../common/services/api/super-admin.service';
import { UserService, RegistrationClassOption } from '../../../common/services/api/user.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { College, PendingUser } from '../../../common/models/super-admin.model';

@Component({
  selector: 'app-super-admin-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  isExportingPdf = false;
  isExportingExcel = false;
  activeReportType: string | null = null;
  activeView: string | null = null; // 'college' | 'branch' | 'student' | null

  colleges: College[] = [];
  branches: RegistrationClassOption[] = [];
  students: PendingUser[] = [];

  selectedCollegeId: string = '';
  selectedBranchId: string = '';
  selectedStudentId: string = '';

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
    private superAdminService: SuperAdminService,
    private userService: UserService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.superAdminService.getColleges().subscribe({
      next: (colleges) => {
        this.colleges = colleges;
      }
    });
    // Load all students for the dropdown
    this.superAdminService.searchUsers({ pageNumber: 1, pageSize: 1000, role: 'Student' }).subscribe({
      next: (res) => {
        this.students = res.items || [];
      }
    });
  }

  onCollegeSelect(): void {
    this.selectedBranchId = '';
    this.branches = [];
    if (this.selectedCollegeId) {
      this.userService.getRegistrationClasses(this.selectedCollegeId).subscribe({
        next: (classes) => {
          this.branches = classes;
        }
      });
    }
  }

  exportReport(type: 'college' | 'branch' | 'student', format: 'pdf' | 'excel'): void {
    let request$;
    let filename = '';

    if (type === 'college') {
      if (!this.selectedCollegeId) {
        this.snackBar.open('Please select a college first.', 'Close', ReportsComponent.errorSnackBarConfig);
        return;
      }
      request$ = this.reportsService.exportCollegeReport(this.selectedCollegeId, format);
      filename = `CollegeReport_${new Date().getTime()}.${format === 'excel' ? 'xlsx' : 'pdf'}`;
    } else if (type === 'branch') {
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

  viewReport(type: 'college' | 'branch' | 'student'): void {
    if (type === 'college' && !this.selectedCollegeId) {
      this.snackBar.open('Please select a college first.', 'Close', ReportsComponent.errorSnackBarConfig);
      return;
    }
    if (type === 'branch' && !this.selectedBranchId) {
      this.snackBar.open('Please select a branch first.', 'Close', ReportsComponent.errorSnackBarConfig);
      return;
    }
    if (type === 'student' && !this.selectedStudentId) {
      this.snackBar.open('Please select a student first.', 'Close', ReportsComponent.errorSnackBarConfig);
      return;
    }
    this.activeView = type;
  }
  
  closeView(): void {
    this.activeView = null;
  }

  // Email Modal State
  isEmailModalOpen = false;
  emailTargetType: 'college' | 'branch' | 'student' | null = null;
  emailFormat: 'pdf' | 'excel' = 'pdf';
  emailAddress: string = '';
  isSendingEmail = false;

  openEmailModal(type: 'college' | 'branch' | 'student', format: 'pdf' | 'excel'): void {
    if (type === 'college' && !this.selectedCollegeId) {
      this.snackBar.open('Please select a college first.', 'Close', ReportsComponent.errorSnackBarConfig);
      return;
    }
    if (type === 'branch' && !this.selectedBranchId) {
      this.snackBar.open('Please select a branch first.', 'Close', ReportsComponent.errorSnackBarConfig);
      return;
    }
    if (type === 'student' && !this.selectedStudentId) {
      this.snackBar.open('Please select a student first.', 'Close', ReportsComponent.errorSnackBarConfig);
      return;
    }

    this.emailTargetType = type;
    this.emailFormat = format;
    this.emailAddress = '';
    this.isEmailModalOpen = true;
  }

  closeEmailModal(): void {
    this.isEmailModalOpen = false;
    this.emailTargetType = null;
  }

  sendEmail(): void {
    if (!this.emailAddress || !this.emailTargetType) return;

    let entityId = '';
    if (this.emailTargetType === 'college') entityId = this.selectedCollegeId;
    if (this.emailTargetType === 'branch') entityId = this.selectedBranchId;
    if (this.emailTargetType === 'student') entityId = this.selectedStudentId;

    this.isSendingEmail = true;
    this.reportsService.emailReport({
      targetEmail: this.emailAddress,
      reportType: this.emailTargetType,
      entityId: entityId,
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
}
