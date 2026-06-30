import { Component, OnInit } from '@angular/core';
import { ReportClass, ReportContextTotals, ReportStudent, ReportsService } from '../../../common/services/api/reports.service';
import { CollegeAdminService, CollegeClassSummary } from '../../../common/services/api/college-admin.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize, forkJoin } from 'rxjs';

@Component({
  selector: 'app-college-admin-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  isExportingPdf = false;
  isExportingExcel = false;
  activeReportType: string | null = null;
  activeView: string | null = null; // 'branch' | 'student' | null

  classes: ReportClass[] = [];
  students: ReportStudent[] = [];
  selectedBranchStudents: ReportStudent[] = [];
  totals: ReportContextTotals = {
    totalClasses: 0,
    totalBatches: 0,
    totalStudents: 0,
    averagePercentage: 0,
    passRate: 0
  };

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
    private collegeAdminService: CollegeAdminService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.collegeAdminService.getClassConfiguration().subscribe({
      next: (configuration) => {
        this.classes = (configuration.classes ?? []).map(item => this.mapCollegeClass(item));
        this.totals = {
          ...this.totals,
          totalClasses: configuration.totals?.totalClasses ?? this.classes.length,
          totalBatches: configuration.totals?.totalBatches ?? this.classes.reduce((sum, item) => sum + item.batches.length, 0)
        };
      },
      error: (err) => {
        console.error('Unable to load college report context:', err);
        this.snackBar.open('Unable to load institution report data.', 'Close', ReportsComponent.errorSnackBarConfig);
      }
    });
  }

  private mapCollegeClass(item: CollegeClassSummary): ReportClass {
    return {
      classId: item.classId,
      collegeId: item.collegeId,
      name: item.name,
      batches: (item.batches ?? []).map(batch => ({
        batchId: batch.batchId,
        classId: batch.classId,
        collegeId: batch.collegeId,
        name: batch.name,
        studentCount: batch.studentCount ?? 0,
        students: []
      }))
    };
  }

  onBranchSelect(): void {
    this.selectedStudentId = '';
    this.students = [];
    this.selectedBranchStudents = [];
    if (this.selectedBranchId) {
      const selectedClass = this.classes.find(c => c.classId === this.selectedBranchId);
      if (selectedClass && selectedClass.batches) {
        const requests = selectedClass.batches.map(batch =>
          this.reportsService.getStudentsForBatch(selectedClass.classId, batch.batchId)
        );

        if (requests.length === 0) {
          return;
        }

        forkJoin(requests).subscribe({
          next: (studentGroups) => {
            selectedClass.batches.forEach((batch, index) => {
              batch.students = studentGroups[index] ?? [];
              batch.studentCount = batch.students.length;
            });
            const allStudents = selectedClass.batches.flatMap(b => b.students || []);
            const uniqueStudents = Array.from(new Map(allStudents.map(s => [s.studentId, s])).values());
            this.students = uniqueStudents;
            this.selectedBranchStudents = uniqueStudents;
            this.refreshTotals();
          },
          error: (err) => {
            console.error('Unable to load students for branch:', err);
            this.snackBar.open('Unable to load students for this branch.', 'Close', ReportsComponent.errorSnackBarConfig);
          }
        });
      }
    }
  }

  private refreshTotals(): void {
    const allStudents = this.classes.flatMap(classItem => classItem.batches.flatMap(batch => batch.students ?? []));
    const uniqueStudents = Array.from(new Map(allStudents.map(student => [student.studentId, student])).values());
    if (uniqueStudents.length === 0) {
      return;
    }

    const totalAssessments = uniqueStudents.reduce((sum, student) => sum + student.assessmentCount, 0);
    this.totals.totalStudents = uniqueStudents.length;
    this.totals.averagePercentage = totalAssessments > 0
      ? uniqueStudents.reduce((sum, student) => sum + student.averagePercentage * student.assessmentCount, 0) / totalAssessments
      : 0;
  }

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
  emailTargetType: 'branch' | 'student' | null = null;
  emailFormat: 'pdf' | 'excel' = 'pdf';
  emailAddress: string = '';
  isSendingEmail = false;

  openEmailModal(type: 'branch' | 'student', format: 'pdf' | 'excel'): void {
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

  get selectedBranchName(): string {
    return this.classes.find(item => item.classId === this.selectedBranchId)?.name ?? 'Selected Branch';
  }

  get selectedStudent(): ReportStudent | undefined {
    return this.students.find(item => item.studentId === this.selectedStudentId);
  }

  get emailRecipients(): string[] {
    return this.emailAddress
      .split(',')
      .map(email => email.trim())
      .filter(email => !!email);
  }

  sendEmail(): void {
    if (this.emailRecipients.length === 0 || !this.emailTargetType) return;

    let entityId = '';
    if (this.emailTargetType === 'branch') entityId = this.selectedBranchId;
    if (this.emailTargetType === 'student') entityId = this.selectedStudentId;

    this.isSendingEmail = true;
    this.reportsService.emailReport({
      targetEmails: this.emailRecipients,
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
