import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import {
  AttendanceBatchGroup,
  AttendanceEntryType,
  AttendanceHistory,
  AttendanceRoster,
  AttendanceSessionType,
  AttendanceStudent,
  TrainerAttendanceService
} from '../../../common/services/api/trainer-attendance.service';

@Component({
  selector: 'app-trainer-students',
  standalone: false,
  templateUrl: './students.component.html',
  styleUrl: './students.component.scss'
})
export class StudentsComponent implements OnInit {
  readonly attendanceSessions = [
    { value: AttendanceSessionType.PreBreak, label: 'Pre-Break' },
    { value: AttendanceSessionType.PostBreak, label: 'Post-Break' }
  ];

  readonly today = this.toDateInput(new Date());
  readonly AttendanceEntryType = AttendanceEntryType;

  batchGroups: AttendanceBatchGroup[] = [];
  roster: AttendanceRoster | null = null;
  history: AttendanceHistory | null = null;

  selectedBatchId = '';
  selectedAttendanceDate = this.today;
  selectedAttendanceSession = AttendanceSessionType.PreBreak;
  historyFromDate = this.today;
  historyToDate = this.today;
  searchTerm = '';
  emailRecipientsInput = '';

  isLoadingBatches = false;
  isLoadingRoster = false;
  isLoadingHistory = false;
  isSubmitting = false;
  isExporting = false;
  isSendingEmail = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private readonly trainerAttendanceService: TrainerAttendanceService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAttendanceBatches();
  }

  get filteredStudents(): AttendanceStudent[] {
    const students = this.roster?.students ?? [];
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      return students;
    }

    return students.filter((student: AttendanceStudent) =>
      student.fullName.toLowerCase().includes(term) ||
      student.email.toLowerCase().includes(term) ||
      (student.enrollmentNumber ?? '').toLowerCase().includes(term));
  }

  get canSubmit(): boolean {
    return !!this.roster && this.roster.canEdit && !this.isSubmitting;
  }

  loadAttendanceBatches(): void {
    this.isLoadingBatches = true;
    this.errorMessage = '';

    this.trainerAttendanceService.getAttendanceBatches().subscribe({
      next: groups => {
        this.batchGroups = groups;
        this.selectedBatchId = this.selectedBatchId || groups.flatMap(group => group.batches).at(0)?.batchId || '';
        this.isLoadingBatches = false;
        this.changeDetectorRef.detectChanges();

        if (this.selectedBatchId) {
          this.loadRoster();
          this.loadHistory();
        }
      },
      error: error => {
        this.errorMessage = error?.error?.message ?? 'Unable to load attendance batches right now.';
        this.isLoadingBatches = false;
      }
    });
  }

  loadRoster(): void {
    if (!this.selectedBatchId) {
      return;
    }

    this.isLoadingRoster = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.changeDetectorRef.detectChanges();

    this.trainerAttendanceService
      .getAttendanceRoster(this.selectedBatchId, this.selectedAttendanceDate, this.selectedAttendanceSession)
      .subscribe({
        next: roster => {
          this.roster = {
            ...roster,
            students: roster.students.map(student => ({
              ...student,
              attendanceEntry: student.attendanceEntry ?? AttendanceEntryType.Absent
            }))
          };
          this.isLoadingRoster = false;
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          this.errorMessage = error?.error?.message ?? 'Unable to load attendance roster right now.';
          this.isLoadingRoster = false;
          this.changeDetectorRef.detectChanges();
        }
      });
  }

  loadHistory(): void {
    if (!this.selectedBatchId) {
      return;
    }

    this.isLoadingHistory = true;
    this.errorMessage = '';
    this.changeDetectorRef.detectChanges();

    this.trainerAttendanceService
      .getAttendanceHistory(this.selectedBatchId, this.historyFromDate, this.historyToDate)
      .subscribe({
        next: history => {
          this.history = history;
          this.isLoadingHistory = false;
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          this.errorMessage = error?.error?.message ?? 'Unable to load attendance history right now.';
          this.isLoadingHistory = false;
          this.changeDetectorRef.detectChanges();
        }
      });
  }

  onBatchChange(batchId: string): void {
    this.selectedBatchId = batchId;
    this.roster = null;
    this.changeDetectorRef.detectChanges();
    this.loadRoster();
    this.loadHistory();
  }

  onAttendanceDateChange(attendanceDate: string): void {
    this.selectedAttendanceDate = attendanceDate;
    this.roster = null;
    this.changeDetectorRef.detectChanges();
    this.loadRoster();
  }

  onAttendanceSessionChange(attendanceSession: AttendanceSessionType): void {
    this.selectedAttendanceSession = attendanceSession;
    this.roster = null;
    this.changeDetectorRef.detectChanges();
    this.loadRoster();
  }

  onRosterFilterChange(): void {
    this.loadRoster();
  }

  onHistoryFilterChange(): void {
    this.loadHistory();
  }

  markAllPresent(): void {
    if (!this.roster?.canEdit) {
      return;
    }

    this.roster.students = this.roster.students.map((student: AttendanceStudent) => ({
      ...student,
      attendanceEntry: AttendanceEntryType.Present
    }));
    this.recalculateRosterSummary();
    this.changeDetectorRef.detectChanges();
  }

  setAttendanceEntry(student: AttendanceStudent, attendanceEntry: AttendanceEntryType): void {
    if (!this.roster?.canEdit) {
      return;
    }

    student.attendanceEntry = attendanceEntry;
    this.recalculateRosterSummary();
    this.changeDetectorRef.detectChanges();
  }

  submitAttendance(): void {
    if (!this.roster?.canEdit || !this.selectedBatchId) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.trainerAttendanceService.submitAttendance({
      batchId: this.selectedBatchId,
      attendanceDate: this.selectedAttendanceDate,
      attendanceSession: this.selectedAttendanceSession,
      entries: this.roster.students.map((student: AttendanceStudent) => ({
        studentId: student.studentId,
        attendanceEntry: student.attendanceEntry ?? AttendanceEntryType.Absent
      }))
    }).subscribe({
      next: roster => {
        this.roster = {
          ...roster,
          students: roster.students.map(student => ({
            ...student,
            attendanceEntry: student.attendanceEntry ?? AttendanceEntryType.Absent
          }))
        };
        this.isSubmitting = false;
        this.successMessage = 'Attendance saved successfully.';
        this.changeDetectorRef.detectChanges();
        this.loadHistory();
      },
      error: error => {
        this.errorMessage = error?.error?.message ?? 'Unable to submit attendance right now.';
        this.isSubmitting = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  exportAttendance(): void {
    if (!this.selectedBatchId) {
      return;
    }

    this.isExporting = true;
    this.errorMessage = '';

    this.trainerAttendanceService.exportAttendance(this.selectedBatchId, this.historyFromDate, this.historyToDate).subscribe({
      next: result => {
        const url = window.URL.createObjectURL(result.blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = result.fileName;
        anchor.click();
        window.URL.revokeObjectURL(url);
        this.isExporting = false;
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.errorMessage = error?.error?.message ?? 'Unable to export attendance right now.';
        this.isExporting = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  emailAttendanceReport(): void {
    if (!this.selectedBatchId) {
      return;
    }

    const recipientEmails = this.emailRecipientsInput
      .split(/[\n,;]+/)
      .map(item => item.trim())
      .filter(item => item.length > 0);

    this.isSendingEmail = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.trainerAttendanceService.emailAttendanceReport({
      batchId: this.selectedBatchId,
      fromDate: this.historyFromDate,
      toDate: this.historyToDate,
      recipientEmails
    }).subscribe({
      next: response => {
        this.successMessage = response.message;
        this.isSendingEmail = false;
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.errorMessage = error?.error?.message ?? 'Unable to send attendance report email right now.';
        this.isSendingEmail = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  attendanceSessionLabel(value: AttendanceSessionType): string {
    return value === AttendanceSessionType.PostBreak ? 'Post-Break' : 'Pre-Break';
  }

  private recalculateRosterSummary(): void {
    if (!this.roster) {
      return;
    }

    const students = this.roster.students as AttendanceStudent[];
    const presentCount = students.filter(student => student.attendanceEntry === AttendanceEntryType.Present).length;
    const absentCount = students.filter(student => student.attendanceEntry === AttendanceEntryType.Absent).length;
    const totalStudents = students.length;

    this.roster = {
      ...this.roster,
      presentCount,
      absentCount,
      totalStudents,
      attendancePercentage: totalStudents === 0 ? 0 : Number(((presentCount * 100) / totalStudents).toFixed(2))
    };
  }

  private toDateInput(value: Date): string {
    return value.toISOString().split('T')[0];
  }
}
