import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import * as XLSX from 'xlsx';
import { forkJoin } from 'rxjs';
import { SuperAdminService } from '../../../common/services/api/super-admin.service';
import { CollegeAdminService, CollegeClassSummary, ApprovedStudent, ClassConfiguration } from '../../../common/services/api/college-admin.service';
import { HttpClientService } from '../../../common/services/http/http-client.service';
import { College } from '../../../common/models/super-admin.model';

export type ReportTab = 'overview' | 'batch' | 'student';

interface KpiCard { label: string; value: string | number; icon: string; }
interface BranchRow { classId: string; name: string; department?: string; studentCount: number; trainerCount: number; }
interface BatchRow { batchId: string; classId: string; className: string; name: string; subjectName?: string; studentCount: number; trainerCount: number; }

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
  selector: 'app-super-admin-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  activeTab: ReportTab = 'overview';
  isLoadingColleges = false;
  isLoading = false;

  colleges: College[] = [];
  selectedCollegeId = '';

  kpiCards: KpiCard[] = [];
  branchRows: BranchRow[] = [];
  classes: CollegeClassSummary[] = [];
  selectedClassId = '';
  allBatchRows: BatchRow[] = [];
  filteredBatchRows: BatchRow[] = [];

  students: ApprovedStudent[] = [];
  selectedStudentId = '';
  isLoadingStudentResults = false;
  studentResults: StudentResult[] = [];
  expandedResultId: string | null = null;

  readonly today = new Date().toLocaleString();

  constructor(
    private readonly superAdminService: SuperAdminService,
    private readonly collegeAdminService: CollegeAdminService,
    private readonly http: HttpClientService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadColleges();
  }

  setActiveTab(tab: ReportTab): void {
    this.activeTab = tab;
    this.expandedResultId = null;
  }

  onCollegeChange(): void {
    this.resetReportData();
    if (!this.selectedCollegeId) return;
    this.loadCollegeReportData();
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

  get selectedCollegeName(): string {
    return this.colleges.find(c => c.collegeId === this.selectedCollegeId)?.name ?? '';
  }

  exportToExcel(): void {
    const wb = XLSX.utils.book_new();

    if (this.activeTab === 'overview') {
      const rows: any[][] = [
        ['Taskverse — College Overview Report'],
        ['College', this.selectedCollegeName],
        ['Generated', this.today],
        [],
        ['Metric', 'Value'],
        ...this.kpiCards.map(c => [c.label, c.value]),
        [],
        ['Branch Performance'],
        ['Branch / Class', 'Department', 'Students', 'Trainers'],
        ...this.branchRows.map(r => [r.name, r.department ?? '—', r.studentCount, r.trainerCount])
      ];
      XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(rows), 'Overview');

    } else if (this.activeTab === 'batch') {
      const rows: any[][] = [
        ['Taskverse — Batch Performance Report'],
        ['College', this.selectedCollegeName],
        ['Generated', this.today],
        [],
        ['Branch', 'Batch', 'Subject', 'Students', 'Trainers'],
        ...this.filteredBatchRows.map(r => [r.className, r.name, r.subjectName ?? '—', r.studentCount, r.trainerCount])
      ];
      XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(rows), 'Batch Performance');

    } else if (this.activeTab === 'student' && this.selectedStudent) {
      const student = this.selectedStudent;
      const summaryRows: any[][] = [
        ['Taskverse — Student Performance Report'],
        ['College', this.selectedCollegeName],
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
          r.assessmentName,
          r.submittedAt ? r.submittedAt.toLocaleDateString() : '—',
          r.totalMarks, r.obtainedMarks,
          `${Number(r.percentage).toFixed(1)}%`,
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
    }

    XLSX.writeFile(wb, `taskverse-${this.activeTab}-report-${Date.now()}.xlsx`);
  }

  exportToPdf(): void {
    window.print();
  }

  private loadColleges(): void {
    this.isLoadingColleges = true;
    this.superAdminService.getColleges().subscribe({
      next: (colleges) => {
        this.colleges = (colleges ?? []).filter(c => c.status?.toLowerCase() === 'active' || c.isActive);
        this.isLoadingColleges = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingColleges = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadCollegeReportData(): void {
    this.isLoading = true;
    forkJoin({
      classConfig: this.superAdminService.getCollegeReportClasses(this.selectedCollegeId),
      students: this.superAdminService.getCollegeReportStudents(this.selectedCollegeId)
    }).subscribe({
      next: ({ classConfig, students }) => {
        const mapped = this.mapClassConfig(classConfig);
        this.classes = mapped.classes;
        this.students = (students ?? []).map((s: any) => this.mapStudent(s));
        this.buildOverview(mapped);
        this.buildBatchData(mapped);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private resetReportData(): void {
    this.kpiCards = [];
    this.branchRows = [];
    this.classes = [];
    this.selectedClassId = '';
    this.allBatchRows = [];
    this.filteredBatchRows = [];
    this.students = [];
    this.selectedStudentId = '';
    this.studentResults = [];
    this.expandedResultId = null;
    this.activeTab = 'overview';
  }

  private buildOverview(classConfig: ClassConfiguration): void {
    const totalStudents = classConfig.classes.reduce((s, c) => s + c.totalStudents, 0);
    const trainerSet = new Set<string>();
    classConfig.classes.forEach(cls => cls.batches.forEach(b => b.assignedTrainers.forEach(t => trainerSet.add(t.trainerId))));

    this.kpiCards = [
      { label: 'Total Branches', value: classConfig.classes.length, icon: 'business' },
      { label: 'Total Students', value: totalStudents, icon: 'groups' },
      { label: 'Total Trainers', value: trainerSet.size, icon: 'person_check' },
      { label: 'Total Batches', value: classConfig.classes.reduce((s, c) => s + c.batches.length, 0), icon: 'groups_3' }
    ];

    this.branchRows = classConfig.classes.map(cls => {
      const clsTrainers = new Set<string>();
      cls.batches.forEach(b => b.assignedTrainers.forEach(t => clsTrainers.add(t.trainerId)));
      return {
        classId: cls.classId,
        name: cls.name,
        department: cls.department,
        studentCount: cls.totalStudents,
        trainerCount: clsTrainers.size
      };
    });
  }

  private buildBatchData(classConfig: ClassConfiguration): void {
    const rows: BatchRow[] = [];
    for (const cls of classConfig.classes) {
      for (const batch of cls.batches) {
        rows.push({
          batchId: batch.batchId,
          classId: batch.classId,
          className: cls.name,
          name: batch.name,
          subjectName: batch.subjectName,
          studentCount: batch.studentCount,
          trainerCount: batch.assignedTrainers.length
        });
      }
    }
    this.allBatchRows = rows;
    this.filteredBatchRows = rows;
  }

  private mapClassConfig(raw: any): ClassConfiguration {
    return {
      totals: {
        totalClasses: raw?.totals?.totalClasses ?? 0,
        totalBatches: raw?.totals?.totalBatches ?? 0,
        totalStudents: raw?.totals?.totalStudents ?? 0,
        capacityUtilization: raw?.totals?.capacityUtilization ?? 0
      },
      classes: (raw?.classes ?? []).map((cls: any) => ({
        classId: cls?.classId ?? '',
        collegeId: cls?.collegeId ?? '',
        name: cls?.name ?? '',
        academicYear: cls?.academicYear,
        department: cls?.department,
        totalStudents: cls?.totalStudents ?? 0,
        totalCapacity: cls?.totalCapacity ?? 0,
        createdAt: cls?.createdAt ?? '',
        batches: (cls?.batches ?? []).map((b: any) => ({
          batchId: b?.batchId ?? '',
          classId: b?.classId ?? '',
          collegeId: b?.collegeId ?? '',
          name: b?.name ?? '',
          description: b?.description,
          subjectId: b?.subjectId,
          subjectName: b?.subjectName,
          capacity: b?.capacity ?? 0,
          studentCount: b?.studentCount ?? 0,
          createdAt: b?.createdAt ?? '',
          assignedTrainers: (b?.assignedTrainers ?? []).map((t: any) => ({
            trainerId: t?.trainerId ?? '',
            userId: t?.userId ?? '',
            fullName: t?.fullName ?? '',
            email: t?.email ?? ''
          })),
          assignedStudents: (b?.assignedStudents ?? []).map((s: any) => this.mapStudent(s))
        }))
      }))
    };
  }

  private mapStudent(s: any): ApprovedStudent {
    return {
      studentId: s?.studentId ?? '',
      userId: s?.userId ?? '',
      fullName: s?.fullName ?? '',
      email: s?.email ?? '',
      currentClassId: s?.currentClassId,
      currentClassName: s?.currentClassName,
      currentBatchId: s?.currentBatchId,
      currentBatchName: s?.currentBatchName
    };
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
