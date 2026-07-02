import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import * as XLSX from 'xlsx';
import { forkJoin } from 'rxjs';
import { ReportEmailService } from '../../../common/services/api/report-email.service';
import {
  TrainerAttendanceService,
  AttendanceBatchGroup,
  AttendanceBatchOption
} from '../../../common/services/api/trainer-attendance.service';
import {
  CollegeAdminService,
  CollegeClassSummary,
  ApprovedStudent,
  ClassConfiguration
} from '../../../common/services/api/college-admin.service';
import { HttpClientService } from '../../../common/services/http/http-client.service';

interface QuestionResult {
  displayOrder: number;
  questionText: string;
  questionType: string;
  marks: number;
  awardedMarks: number;
  status: string;
  userAnswers: string[];
  correctAnswers: string[];
}

interface StudentResult {
  resultId: string;
  attemptId: string;
  assessmentName: string;
  submittedAt?: Date;
  totalMarks: number;
  obtainedMarks: number;
  percentage: number;
  status: string;
  totalQuestions: number;
  correctAnswers: number;
  wrongAnswers: number;
  unansweredQuestions: number;
  questionResults: QuestionResult[];
  questionsLoading: boolean;
  questionsFetched: boolean;
}

interface FlatBatch {
  batchId: string;
  batchName: string;
  classId: string;
  className: string;
  students: ApprovedStudent[];
}

@Component({
  selector: 'app-trainer-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  isLoadingBatches = false;
  isLoadingStudentResults = false;
  isLoadingStructure = false;

  batches: FlatBatch[] = [];
  selectedBatchId = '';
  studentsInBatch: ApprovedStudent[] = [];
  selectedStudentId = '';

  studentResults: StudentResult[] = [];
  expandedResultId: string | null = null;

  readonly today = new Date().toLocaleString();

  showMailModal = false;
  mailPanelFor: 'main' | string = 'main';
  mailRecipients = '';
  isSendingEmail = false;
  emailSendResult: 'success' | 'error' | null = null;
  emailSendMessage = '';

  constructor(
    private readonly trainerAttendanceService: TrainerAttendanceService,
    private readonly collegeAdminService: CollegeAdminService,
    private readonly http: HttpClientService,
    private readonly emailService: ReportEmailService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadBatchStructure();
  }

  onBatchChange(): void {
    this.selectedStudentId = '';
    this.studentResults = [];
    this.expandedResultId = null;
    const batch = this.batches.find(b => b.batchId === this.selectedBatchId);
    this.studentsInBatch = batch?.students ?? [];
    this.cdr.detectChanges();
  }

  onStudentChange(): void {
    if (!this.selectedStudentId) {
      this.studentResults = [];
      return;
    }
    this.isLoadingStudentResults = true;
    this.studentResults = [];
    this.expandedResultId = null;

    this.http.get<any[]>(`results/students/${this.selectedStudentId}`).subscribe({
      next: (results) => {
        this.studentResults = (results ?? []).map(r => this.mapStudentResult(r));
        this.isLoadingStudentResults = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingStudentResults = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleQuestionSummary(resultId: string): void {
    if (this.expandedResultId === resultId) {
      this.expandedResultId = null;
      return;
    }
    this.expandedResultId = resultId;

    const result = this.studentResults.find(r => r.resultId === resultId);
    if (!result || result.questionsFetched || result.questionsLoading) return;

    result.questionsLoading = true;
    this.http.get<any>(`results/students/attempts/${result.attemptId}`).subscribe({
      next: (detail) => {
        const mapped = this.mapStudentResult(detail);
        result.questionResults = mapped.questionResults;
        result.questionsLoading = false;
        result.questionsFetched = true;
        this.cdr.detectChanges();
      },
      error: () => {
        result.questionsLoading = false;
        result.questionsFetched = true;
        this.cdr.detectChanges();
      }
    });
  }

  get selectedStudent(): ApprovedStudent | undefined {
    return this.studentsInBatch.find(s => s.studentId === this.selectedStudentId);
  }

  get studentAvgScore(): string {
    if (!this.studentResults.length) return '0%';
    const avg = this.studentResults.reduce((s, r) => s + Number(r.percentage), 0) / this.studentResults.length;
    return `${avg.toFixed(1)}%`;
  }

  get studentPassCount(): number {
    return this.studentResults.filter(r => r.status?.toLowerCase() === 'pass').length;
  }

  get selectedBatchName(): string {
    return this.batches.find(b => b.batchId === this.selectedBatchId)?.batchName ?? '';
  }

  exportToExcel(): void {
    if (!this.selectedStudent) return;
    XLSX.writeFile(this.buildMainWorkbook(), `taskverse-trainer-report-${Date.now()}.xlsx`);
  }

  exportToPdf(): void {
    window.print();
  }

  exportResultToExcel(result: StudentResult): void {
    XLSX.writeFile(this.buildResultWorkbook(result), `taskverse-questions-${result.assessmentName.replace(/\s+/g, '-')}-${Date.now()}.xlsx`);
  }

  openMailPanel(forTarget: 'main' | string): void {
    this.mailPanelFor = forTarget;
    this.showMailModal = true;
    this.mailRecipients = '';
    this.emailSendResult = null;
    this.emailSendMessage = '';
    this.isSendingEmail = false;
    this.cdr.detectChanges();
  }

  closeMailPanel(): void {
    this.showMailModal = false;
    this.emailSendResult = null;
    this.cdr.detectChanges();
  }

  sendEmail(): void {
    const recipients = this.emailService.parseRecipients(this.mailRecipients);
    if (!recipients.length) {
      this.emailSendResult = 'error';
      this.emailSendMessage = 'Please enter at least one valid email address.';
      this.cdr.detectChanges();
      return;
    }

    let wb: XLSX.WorkBook;
    let fileName: string;

    if (this.mailPanelFor === 'main') {
      wb = this.buildMainWorkbook();
      fileName = `taskverse-trainer-report-${Date.now()}.xlsx`;
    } else {
      const result = this.studentResults.find(r => r.resultId === this.mailPanelFor);
      if (!result) return;
      wb = this.buildResultWorkbook(result);
      fileName = `taskverse-questions-${result.assessmentName.replace(/\s+/g, '-')}-${Date.now()}.xlsx`;
    }

    let base64: string;
    try {
      base64 = XLSX.write(wb, { type: 'base64', bookType: 'xlsx' }) as string;
    } catch {
      this.emailSendResult = 'error';
      this.emailSendMessage = 'Could not generate report file. Please try again.';
      this.cdr.detectChanges();
      return;
    }

    if (!base64) {
      this.emailSendResult = 'error';
      this.emailSendMessage = 'Report file is empty. Please try again.';
      this.cdr.detectChanges();
      return;
    }

    this.isSendingEmail = true;
    this.emailSendResult = null;
    this.cdr.detectChanges();

    this.emailService.sendEmail({ recipients, fileName, fileContentBase64: base64 }).subscribe({
      next: () => {
        this.isSendingEmail = false;
        this.emailSendResult = 'success';
        this.emailSendMessage = `Report sent to ${recipients.join(', ')}.`;
        this.cdr.detectChanges();
        setTimeout(() => { this.showMailModal = false; this.cdr.detectChanges(); }, 2500);
      },
      error: () => {
        this.isSendingEmail = false;
        this.emailSendResult = 'error';
        this.emailSendMessage = 'Failed to send email. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  private buildMainWorkbook(): XLSX.WorkBook {
    const wb = XLSX.utils.book_new();
    const student = this.selectedStudent;
    if (!student) return wb;
    const summaryRows: any[][] = [
      ['Taskverse — Trainer Report'],
      ['Batch', this.selectedBatchName],
      ['Student', student.fullName],
      ['Email', student.email],
      ['Generated', this.today],
      [],
      ['Total Assessments', this.studentResults.length],
      ['Average Score', this.studentAvgScore],
      ['Passed', this.studentPassCount],
      [],
      ['Assessment', 'Date', 'Total Marks', 'Score', 'Percentage', 'Status', 'Correct', 'Wrong', 'Skipped'],
      ...this.studentResults.map(r => [
        r.assessmentName, r.submittedAt ? r.submittedAt.toLocaleDateString() : '—',
        r.totalMarks, r.obtainedMarks, `${Number(r.percentage).toFixed(1)}%`,
        r.status, r.correctAnswers, r.wrongAnswers, r.unansweredQuestions
      ])
    ];
    XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(summaryRows), 'Student Report');
    const qRows: any[][] = [['Assessment', 'Q#', 'Question', 'Type', 'Max Marks', 'Awarded', 'Status', 'Your Answer', 'Correct Answer']];
    for (const result of this.studentResults) {
      for (const q of result.questionResults) {
        qRows.push([result.assessmentName, q.displayOrder, q.questionText, q.questionType, q.marks, q.awardedMarks, q.status, q.userAnswers?.join(', ') || '', q.correctAnswers?.join(', ') || '']);
      }
    }
    XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(qRows), 'Question Summary');
    return wb;
  }

  private buildResultWorkbook(result: StudentResult): XLSX.WorkBook {
    const wb = XLSX.utils.book_new();
    const rows: any[][] = [
      ['Taskverse — Question Details'],
      ['Assessment', result.assessmentName],
      ['Student', this.selectedStudent?.fullName ?? ''],
      ['Submitted', result.submittedAt ? result.submittedAt.toLocaleString() : '—'],
      ['Score', `${result.obtainedMarks} / ${result.totalMarks} (${Number(result.percentage).toFixed(1)}%)`],
      ['Status', result.status],
      [],
      ['Q#', 'Question', 'Type', 'Max Marks', 'Awarded', 'Status', 'Your Answer', 'Correct Answer'],
      ...result.questionResults.map(q => [
        q.displayOrder, q.questionText, q.questionType, q.marks, q.awardedMarks,
        q.status, q.userAnswers?.join(', ') || '—', q.correctAnswers?.join(', ') || '—'
      ])
    ];
    XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(rows), 'Question Details');
    return wb;
  }

  private loadBatchStructure(): void {
    this.isLoadingStructure = true;
    forkJoin({
      batchGroups: this.trainerAttendanceService.getAttendanceBatches(),
      classConfig: this.collegeAdminService.getReportClassConfiguration()
    }).subscribe({
      next: ({ batchGroups, classConfig }) => {
        this.batches = this.buildFlatBatches(batchGroups, classConfig);
        this.isLoadingStructure = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingStructure = false;
        this.cdr.detectChanges();
      }
    });
  }

  private buildFlatBatches(batchGroups: AttendanceBatchGroup[], classConfig: ClassConfiguration): FlatBatch[] {
    const result: FlatBatch[] = [];

    for (const group of batchGroups) {
      for (const attendanceBatch of group.batches) {
        const cls = classConfig.classes.find(c => c.classId === group.classId);
        const configBatch = cls?.batches.find(b => b.batchId === attendanceBatch.batchId);

        result.push({
          batchId: attendanceBatch.batchId,
          batchName: attendanceBatch.batchName,
          classId: group.classId,
          className: group.className,
          students: configBatch?.assignedStudents ?? []
        });
      }
    }

    return result;
  }

  private mapStudentResult(r: any): StudentResult {
    const submittedRaw = r.submittedAt ?? r.submitted_at;
    return {
      resultId: r.resultId ?? r.result_id ?? '',
      attemptId: r.attemptId ?? r.attempt_id ?? '',
      assessmentName: r.assessmentName ?? r.assessment_name ?? '',
      submittedAt: submittedRaw ? new Date(submittedRaw) : undefined,
      totalMarks: r.totalMarks ?? r.total_marks ?? 0,
      obtainedMarks: r.obtainedMarks ?? r.obtained_marks ?? 0,
      percentage: r.percentage ?? 0,
      status: r.resultStatus ?? r.result_status ?? '',
      totalQuestions: r.totalQuestions ?? r.total_questions ?? 0,
      correctAnswers: r.correctAnswers ?? r.correct_answers ?? 0,
      wrongAnswers: r.wrongAnswers ?? r.wrong_answers ?? 0,
      unansweredQuestions: r.unansweredQuestions ?? r.unanswered_questions ?? 0,
      questionResults: (r.questionResults ?? r.question_results ?? []).map((q: any) => ({
        displayOrder: q.displayOrder ?? q.display_order ?? 0,
        questionText: q.questionText ?? q.question_text ?? '',
        questionType: q.questionType ?? q.question_type ?? '',
        marks: q.marks ?? 0,
        awardedMarks: q.awardedMarks ?? q.awarded_marks ?? 0,
        status: q.status ?? '',
        userAnswers: q.userAnswers ?? q.user_answers ?? [],
        correctAnswers: q.correctAnswers ?? q.correct_answers ?? []
      })),
      questionsLoading: false,
      questionsFetched: false
    };
  }
}
