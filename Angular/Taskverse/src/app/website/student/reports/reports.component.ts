import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import * as XLSX from 'xlsx';
import { Session } from '../../../common/services/session/session.service';
import {
  StudentAssessmentsService,
  StudentResult,
  StudentResultQuestionResult
} from '../../../common/services/api/student-assessments.service';

interface AssessmentGroup {
  assessmentId: string;
  assessmentName: string;
  attempts: AttemptSummary[];
  avgPercentage: number;
  bestPercentage: number;
  totalAttempts: number;
  passCount: number;
}

interface AttemptSummary {
  resultId: string;
  attemptId: string;
  assessmentId: string;
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
  questionResults: StudentResultQuestionResult[];
  questionsLoading: boolean;
  questionsFetched: boolean;
}

@Component({
  selector: 'app-student-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  isLoading = false;
  errorMessage = '';

  allResults: AttemptSummary[] = [];
  assessmentGroups: AssessmentGroup[] = [];

  selectedAssessmentId = '';
  selectedAttemptId = '';
  selectedAttempt: AttemptSummary | null = null;

  readonly today = new Date().toLocaleString();

  constructor(
    private readonly session: Session,
    private readonly studentService: StudentAssessmentsService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAllResults();
  }

  onAssessmentChange(): void {
    this.selectedAttemptId = '';
    this.selectedAttempt = null;
    this.cdr.detectChanges();
  }

  onAttemptChange(): void {
    if (!this.selectedAttemptId) {
      this.selectedAttempt = null;
      return;
    }
    const attempt = this.allResults.find(r => r.attemptId === this.selectedAttemptId) ?? null;
    this.selectedAttempt = attempt;

    if (attempt && !attempt.questionsFetched && !attempt.questionsLoading) {
      attempt.questionsLoading = true;
      this.cdr.detectChanges();

      this.studentService.getStudentAttemptResult(attempt.attemptId).subscribe({
        next: (detail) => {
          if (detail) {
            attempt.questionResults = detail.questionResults ?? [];
          }
          attempt.questionsLoading = false;
          attempt.questionsFetched = true;
          this.cdr.detectChanges();
        },
        error: () => {
          attempt.questionsLoading = false;
          attempt.questionsFetched = true;
          this.cdr.detectChanges();
        }
      });
    }
  }

  get selectedGroup(): AssessmentGroup | null {
    return this.assessmentGroups.find(g => g.assessmentId === this.selectedAssessmentId) ?? null;
  }

  get attemptsForSelectedAssessment(): AttemptSummary[] {
    return this.allResults.filter(r => {
      const group = this.assessmentGroups.find(g => g.assessmentId === this.selectedAssessmentId);
      return group?.attempts.some(a => a.attemptId === r.attemptId);
    });
  }

  get totalTaken(): number { return this.allResults.length; }

  get avgScore(): string {
    if (!this.allResults.length) return '0%';
    const avg = this.allResults.reduce((s, r) => s + r.percentage, 0) / this.allResults.length;
    return `${avg.toFixed(1)}%`;
  }

  get passCount(): number {
    return this.allResults.filter(r => r.status?.toLowerCase() === 'pass').length;
  }

  get passRate(): string {
    if (!this.allResults.length) return '0%';
    return `${((this.passCount / this.allResults.length) * 100).toFixed(0)}%`;
  }

  get bestScore(): string {
    if (!this.allResults.length) return '0%';
    const best = Math.max(...this.allResults.map(r => r.percentage));
    return `${best.toFixed(1)}%`;
  }

  exportToPdf(): void {
    window.print();
  }

  exportToExcel(): void {
    const wb = XLSX.utils.book_new();

    const summaryRows: any[][] = [
      ['Taskverse — My Performance Report'],
      ['Generated', this.today],
      [],
      ['Total Assessments Taken', this.totalTaken],
      ['Average Score', this.avgScore],
      ['Pass Rate', this.passRate],
      ['Best Score', this.bestScore],
      [],
      ['Assessment', 'Date', 'Total Marks', 'Score', '%', 'Status', 'Correct', 'Wrong', 'Skipped'],
      ...this.allResults.map(r => [
        r.assessmentName,
        r.submittedAt ? r.submittedAt.toLocaleDateString() : '—',
        r.totalMarks, r.obtainedMarks,
        `${r.percentage.toFixed(1)}%`,
        r.status, r.correctAnswers, r.wrongAnswers, r.unansweredQuestions
      ])
    ];
    XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(summaryRows), 'My Reports');

    const qRows: any[][] = [['Assessment', 'Q#', 'Question', 'Type', 'Max', 'Awarded', 'Status', 'Your Answer', 'Correct Answer']];
    for (const result of this.allResults) {
      for (const q of result.questionResults) {
        qRows.push([result.assessmentName, q.displayOrder, q.questionText, q.questionType, q.marks, q.awardedMarks, q.status, q.userAnswers?.join(', ') || '', q.correctAnswers?.join(', ') || '']);
      }
    }
    if (qRows.length > 1) {
      XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(qRows), 'Question Details');
    }

    XLSX.writeFile(wb, `taskverse-my-report-${Date.now()}.xlsx`);
  }

  private loadAllResults(): void {
    const studentId = this.session.userId;
    if (!studentId) {
      this.errorMessage = 'Unable to determine your student account. Please log in again.';
      return;
    }

    this.isLoading = true;
    this.studentService.getStudentResults(studentId).subscribe({
      next: (results) => {
        this.allResults = (results ?? []).map(r => this.mapAttempt(r));
        this.assessmentGroups = this.buildGroups(this.allResults);
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Failed to load your reports. Please try again.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private buildGroups(attempts: AttemptSummary[]): AssessmentGroup[] {
    const map = new Map<string, AssessmentGroup>();

    for (const a of attempts) {
      const key = a.assessmentId || a.assessmentName;
      if (!map.has(key)) {
        map.set(key, {
          assessmentId: key,
          assessmentName: a.assessmentName,
          attempts: [],
          avgPercentage: 0,
          bestPercentage: 0,
          totalAttempts: 0,
          passCount: 0
        });
      }
      map.get(key)!.attempts.push(a);
    }

    for (const group of map.values()) {
      group.totalAttempts = group.attempts.length;
      group.passCount = group.attempts.filter(a => a.status?.toLowerCase() === 'pass').length;
      group.avgPercentage = group.attempts.reduce((s, a) => s + a.percentage, 0) / group.totalAttempts;
      group.bestPercentage = Math.max(...group.attempts.map(a => a.percentage));
    }

    return Array.from(map.values()).sort((a, b) => a.assessmentName.localeCompare(b.assessmentName));
  }

  private mapAttempt(r: StudentResult): AttemptSummary {
    return {
      resultId: r.resultId,
      attemptId: r.attemptId,
      assessmentId: r.assessmentId,
      assessmentName: r.assessmentName,
      submittedAt: r.submittedAt ? new Date(r.submittedAt as any) : undefined,
      totalMarks: r.totalMarks,
      obtainedMarks: r.obtainedMarks,
      percentage: r.percentage ?? 0,
      status: r.resultStatus ?? '',
      totalQuestions: r.totalQuestions ?? 0,
      correctAnswers: r.correctAnswers ?? 0,
      wrongAnswers: r.wrongAnswers ?? 0,
      unansweredQuestions: r.unansweredQuestions ?? 0,
      questionResults: r.questionResults ?? [],
      questionsLoading: false,
      questionsFetched: (r.questionResults?.length ?? 0) > 0
    };
  }
}
