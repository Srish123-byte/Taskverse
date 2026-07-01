import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import * as XLSX from 'xlsx';
import { forkJoin } from 'rxjs';
import {
  CollegeAdminService,
  ApprovedStudent,
  ClassConfiguration,
  CollegeClassSummary,
} from '../../../common/services/api/college-admin.service';
import {
  AssessmentAdminService,
  AssessmentManagementItem,
} from '../../../common/services/api/assessment-admin.service';
import { HttpClientService } from '../../../common/services/http/http-client.service';
import { ReportEmailService } from '../../../common/services/api/report-email.service';

export type ReportTab = 'overview' | 'batch' | 'student';

interface KpiCard {
  label: string;
  value: string | number;
  icon: string;
}

interface BranchRow {
  classId: string;
  name: string;
  studentCount: number;
  trainerCount: number;
  assessmentCount: number;
}

interface BatchRow {
  batchId: string;
  classId: string;
  className: string;
  name: string;
  subjectName?: string;
  studentCount: number;
  trainerCount: number;
  assessmentCount: number;
}

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

@Component({
  selector: 'app-college-admin-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  activeTab: ReportTab = 'overview';
  isLoading = false;

  // Overview tab
  kpiCards: KpiCard[] = [];
  branchRows: BranchRow[] = [];

  // Batch tab
  classes: CollegeClassSummary[] = [];
  selectedClassId = '';
  allBatchRows: BatchRow[] = [];
  filteredBatchRows: BatchRow[] = [];

  // Student tab
  students: ApprovedStudent[] = [];
  selectedStudentId = '';
  isLoadingStudentResults = false;
  studentResults: StudentResult[] = [];
  expandedResultId: string | null = null;

  readonly today = new Date().toLocaleString();

  // Mail panel state
  showMailPanel = false;
  mailPanelFor: 'main' | string = 'main';
  mailRecipients = '';
  isSendingEmail = false;
  emailSendResult: 'success' | 'error' | null = null;
  emailSendMessage = '';

  constructor(
    private readonly collegeAdminService: CollegeAdminService,
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly http: HttpClientService,
    private readonly emailService: ReportEmailService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadReportData();
  }

  setActiveTab(tab: ReportTab): void {
    this.activeTab = tab;
    this.expandedResultId = null;
  }

  onClassFilterChange(): void {
    this.filteredBatchRows = this.selectedClassId
      ? this.allBatchRows.filter(b => b.classId === this.selectedClassId)
      : this.allBatchRows;
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
    return this.students.find(s => s.studentId === this.selectedStudentId);
  }

  get studentAvgScore(): string {
    if (!this.studentResults.length) return '0%';
    const avg = this.studentResults.reduce((s, r) => s + Number(r.percentage), 0) / this.studentResults.length;
    return `${avg.toFixed(1)}%`;
  }

  get studentPassCount(): number {
    return this.studentResults.filter(r => r.status?.toLowerCase() === 'pass').length;
  }

  get selectedClassName(): string {
    return this.classes.find(c => c.classId === this.selectedClassId)?.name ?? '';
  }

  exportToExcel(): void {
    const wb = this.buildMainWorkbook();
    XLSX.writeFile(wb, `taskverse-${this.activeTab}-report-${Date.now()}.xlsx`);
  }

  exportToPdf(): void {
    window.print();
  }

  exportResultToExcel(result: StudentResult): void {
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
    XLSX.writeFile(wb, `taskverse-questions-${result.assessmentName.replace(/\s+/g, '-')}-${Date.now()}.xlsx`);
  }

  openMailPanel(forTarget: 'main' | string): void {
    this.mailPanelFor = forTarget;
    this.showMailPanel = true;
    this.mailRecipients = '';
    this.emailSendResult = null;
    this.emailSendMessage = '';
    this.cdr.detectChanges();
  }

  closeMailPanel(): void {
    this.showMailPanel = false;
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
      fileName = `taskverse-${this.activeTab}-report-${Date.now()}.xlsx`;
    } else {
      const result = this.studentResults.find(r => r.resultId === this.mailPanelFor);
      if (!result) return;
      wb = this.buildResultWorkbook(result);
      fileName = `taskverse-questions-${result.assessmentName.replace(/\s+/g, '-')}-${Date.now()}.xlsx`;
    }

    const fileBytes = XLSX.write(wb, { type: 'array', bookType: 'xlsx' }) as Uint8Array;
    const base64 = btoa(String.fromCharCode(...fileBytes));

    this.isSendingEmail = true;
    this.emailSendResult = null;
    this.cdr.detectChanges();

    this.emailService.sendEmail({ recipients, fileName, fileContentBase64: base64 }).subscribe({
      next: () => {
        this.isSendingEmail = false;
        this.emailSendResult = 'success';
        this.emailSendMessage = `Report sent to ${recipients.join(', ')}.`;
        this.cdr.detectChanges();
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

    if (this.activeTab === 'overview') {
      const rows: any[][] = [
        ['Taskverse — College Overview Report'],
        ['Generated', this.today],
        [],
        ['Metric', 'Value'],
        ...this.kpiCards.map(c => [c.label, c.value]),
        [],
        ['Branch Performance'],
        ['Branch / Class', 'Students', 'Trainers', 'Assessments'],
        ...this.branchRows.map(r => [r.name, r.studentCount, r.trainerCount, r.assessmentCount])
      ];
      XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(rows), 'Overview');

    } else if (this.activeTab === 'batch') {
      const rows: any[][] = [
        ['Taskverse — Batch Performance Report'],
        ['Generated', this.today],
        [],
        ['Branch', 'Batch', 'Subject', 'Students', 'Trainers', 'Assessments'],
        ...this.filteredBatchRows.map(r => [r.className, r.name, r.subjectName ?? '—', r.studentCount, r.trainerCount, r.assessmentCount])
      ];
      XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(rows), 'Batch Performance');

    } else if (this.activeTab === 'student' && this.selectedStudent) {
      const student = this.selectedStudent;
      const summaryRows: any[][] = [
        ['Taskverse — Student Performance Report'],
        ['Student', student.fullName],
        ['Email', student.email],
        ['Generated', this.today],
        [],
        ['Total Assessments', this.studentResults.length],
        ['Average Score', this.studentAvgScore],
        ['Passed', this.studentPassCount],
        [],
        ['Assessment', 'Date', 'Total Marks', 'Score', 'Percentage', 'Status', 'Correct', 'Wrong', 'Unanswered'],
        ...this.studentResults.map(r => [
          r.assessmentName,
          r.submittedAt ? r.submittedAt.toLocaleDateString() : '—',
          r.totalMarks, r.obtainedMarks,
          `${Number(r.percentage).toFixed(1)}%`,
          r.status, r.correctAnswers, r.wrongAnswers, r.unansweredQuestions
        ])
      ];
      XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(summaryRows), 'Student Report');

      const qRows: any[][] = [
        ['Assessment', 'Q#', 'Question', 'Type', 'Max Marks', 'Awarded', 'Status', 'Your Answer', 'Correct Answer']
      ];
      for (const result of this.studentResults) {
        for (const q of result.questionResults) {
          qRows.push([result.assessmentName, q.displayOrder, q.questionText, q.questionType, q.marks, q.awardedMarks, q.status, q.userAnswers?.join(', ') || '', q.correctAnswers?.join(', ') || '']);
        }
      }
      if (qRows.length > 1) {
        XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(qRows), 'Question Summary');
      }
    }

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

  private loadReportData(): void {
    this.isLoading = true;
    forkJoin({
      dashboard: this.collegeAdminService.getDashboard(),
      classConfig: this.collegeAdminService.getClassConfiguration(),
      assessments: this.assessmentAdminService.searchAssessments({ pageNumber: 1, pageSize: 200 }),
      students: this.collegeAdminService.getApprovedStudents()
    }).subscribe({
      next: ({ dashboard, classConfig, assessments, students }) => {
        this.students = students;
        this.classes = classConfig.classes;
        this.buildOverview(dashboard, classConfig, assessments.items);
        this.buildBatchData(classConfig, assessments.items);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private buildOverview(dashboard: any, classConfig: ClassConfiguration, assessments: AssessmentManagementItem[]): void {
    this.kpiCards = [
      { label: 'Total Branches', value: classConfig.classes.length, icon: 'business' },
      { label: 'Total Students', value: dashboard.totals.registeredStudents, icon: 'groups' },
      { label: 'Total Trainers', value: dashboard.totals.registeredTrainers, icon: 'person_check' },
      { label: 'Total Assessments', value: dashboard.totals.totalAssessments, icon: 'assignment' }
    ];

    this.branchRows = classConfig.classes.map(cls => {
      const trainerSet = new Set<string>();
      cls.batches.forEach(b => b.assignedTrainers.forEach(t => trainerSet.add(t.trainerId)));

      const classBatchIds = new Set(cls.batches.map(b => b.batchId));
      const assessmentCount = assessments.filter(a =>
        (a.assignedBatchIds ?? []).some(id => classBatchIds.has(id))
      ).length;

      return {
        classId: cls.classId,
        name: cls.name,
        studentCount: cls.totalStudents,
        trainerCount: trainerSet.size,
        assessmentCount
      };
    });
  }

  private buildBatchData(classConfig: ClassConfiguration, assessments: AssessmentManagementItem[]): void {
    const rows: BatchRow[] = [];

    for (const cls of classConfig.classes) {
      for (const batch of cls.batches) {
        const assessmentCount = assessments.filter(a =>
          (a.assignedBatchIds ?? []).includes(batch.batchId)
        ).length;

        rows.push({
          batchId: batch.batchId,
          classId: batch.classId,
          className: cls.name,
          name: batch.name,
          subjectName: batch.subjectName,
          studentCount: batch.studentCount,
          trainerCount: batch.assignedTrainers.length,
          assessmentCount
        });
      }
    }

    this.allBatchRows = rows;
    this.filteredBatchRows = rows;
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
