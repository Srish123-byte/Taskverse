import { ChangeDetectorRef, Component, HostBinding, Input, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AssessmentAdminService,
  AssessmentSubjectCatalogItem,
  AssessmentSubjectTopicCatalog,
  QuestionBankItem
} from '../../services/api/assessment-admin.service';

interface DifficultyOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-assessment-creator',
  standalone: false,
  templateUrl: './assessment-creator.component.html',
  styleUrl: './assessment-creator.component.scss'
})
export class AssessmentCreatorComponent implements OnInit {
  @Input() theme: 'college-admin' | 'trainer' = 'college-admin';
  @Input() backRoute = '';

  readonly difficultyOptions: DifficultyOption[] = [
    { value: 'all', label: 'All levels' },
    { value: '1', label: 'Easy' },
    { value: '2', label: 'Medium' },
    { value: '3', label: 'Hard' }
  ];

  isCatalogLoading = true;
  isQuestionBankLoading = true;
  catalogErrorMessage = '';
  questionBankErrorMessage = '';

  subjectCatalog: AssessmentSubjectTopicCatalog = { subjects: [] };
  questions: QuestionBankItem[] = [];

  selectedBatchIds = new Set<string>();
  selectedQuestionIds = new Set<string>();

  assessmentName = '';
  selectedSubjectId = '';
  selectedTopicId = '';
  selectedDifficulty = 'all';
  durationMinutes: number | null = 60;
  totalMarks: number | null = 100;
  startDate = '';
  endDate = '';
  instructions = '';
  allowLateEntry = false;
  showResultsImmediately = false;
  allowQuestionReview = true;
  negativeMarking = false;

  questionSearchTerm = '';

  @HostBinding('class.theme-trainer')
  get isTrainerTheme(): boolean {
    return this.theme === 'trainer';
  }

  @HostBinding('class.theme-college-admin')
  get isCollegeAdminTheme(): boolean {
    return this.theme === 'college-admin';
  }

  get backRouteSegments(): string[] {
    return this.backRoute.split('/').filter(segment => segment.length > 0);
  }

  get selectedBatchCount(): number {
    return this.selectedBatchIds.size;
  }

  get selectedQuestionCount(): number {
    return this.selectedQuestionIds.size;
  }

  get filteredQuestions(): QuestionBankItem[] {
    const normalizedSearch = this.questionSearchTerm.trim().toLowerCase();

    return this.questions.filter(question => {
      const matchesSearch =
        normalizedSearch.length === 0 ||
        question.questionText.toLowerCase().includes(normalizedSearch) ||
        (question.subject ?? '').toLowerCase().includes(normalizedSearch) ||
        (question.topic ?? '').toLowerCase().includes(normalizedSearch);

      const matchesDifficulty =
        this.selectedDifficulty === 'all' ||
        question.difficultyLevel === Number(this.selectedDifficulty);

      const matchesSubject =
        !this.selectedSubjectId ||
        question.subjectId === this.selectedSubjectId;

      const matchesTopic =
        !this.selectedTopicId ||
        question.topicId === this.selectedTopicId;

      return matchesSearch && matchesDifficulty && matchesSubject && matchesTopic;
    });
  }

  get visibleSubjects(): AssessmentSubjectCatalogItem[] {
    if (this.selectedBatchIds.size === 0) {
      return this.subjectCatalog.subjects;
    }

    return this.subjectCatalog.subjects.filter(subject =>
      subject.batchIds.some(batchId => this.selectedBatchIds.has(batchId)));
  }

  get visibleTopics() {
    const subject = this.visibleSubjects.find(item => item.subjectId === this.selectedSubjectId);
    if (!subject) {
      return [];
    }

    if (this.selectedBatchIds.size === 0) {
      return subject.topics;
    }

    return subject.topics.filter(topic =>
      topic.batchIds.some(batchId => this.selectedBatchIds.has(batchId)));
  }

  constructor(
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadSubjectTopicCatalog();
    this.loadQuestionBank();
  }

  onSubjectChange(): void {
    if (this.selectedTopicId &&
        !this.visibleTopics.some(topic => topic.topicId === this.selectedTopicId)) {
      this.selectedTopicId = '';
    }
  }

  isQuestionSelected(questionId: string): boolean {
    return this.selectedQuestionIds.has(questionId);
  }

  toggleQuestionSelection(questionId: string): void {
    if (this.selectedQuestionIds.has(questionId)) {
      this.selectedQuestionIds.delete(questionId);
      return;
    }

    this.selectedQuestionIds.add(questionId);
  }

  getDifficultyLabel(difficultyLevel: number): string {
    switch (difficultyLevel) {
      case 1:
        return 'Easy';
      case 2:
        return 'Medium';
      case 3:
        return 'Hard';
      default:
        return 'Unspecified';
    }
  }

  getDifficultyClass(difficultyLevel: number): string {
    switch (difficultyLevel) {
      case 1:
        return 'difficulty-easy';
      case 2:
        return 'difficulty-medium';
      case 3:
        return 'difficulty-hard';
      default:
        return 'difficulty-default';
    }
  }

  openAddQuestionPlaceholder(): void {
    this.openDeferredActionMessage('Add New Question');
  }

  saveDraft(): void {
    this.openDeferredActionMessage('Save as Draft');
  }

  scheduleAssessment(): void {
    this.openDeferredActionMessage('Schedule Assessment');
  }

  trackByQuestionId(_: number, question: QuestionBankItem): string {
    return question.questionId;
  }

  trackBySubjectId(_: number, subject: AssessmentSubjectCatalogItem): string {
    return subject.subjectId;
  }

  private loadSubjectTopicCatalog(): void {
    this.isCatalogLoading = true;
    this.catalogErrorMessage = '';

    this.assessmentAdminService.getSubjectTopicCatalog().subscribe({
      next: catalog => {
        this.subjectCatalog = catalog ?? { subjects: [] };
        this.isCatalogLoading = false;
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.catalogErrorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          'Unable to load the subject and topic catalog right now.';
        this.isCatalogLoading = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private loadQuestionBank(): void {
    this.isQuestionBankLoading = true;
    this.questionBankErrorMessage = '';

    this.assessmentAdminService.searchQuestionBank({
      pageNumber: 1,
      pageSize: 50
    }).subscribe({
      next: result => {
        this.questions = result?.items ?? [];
        this.isQuestionBankLoading = false;
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.questionBankErrorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          'Unable to load the question bank right now.';
        this.isQuestionBankLoading = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private openDeferredActionMessage(actionName: string): void {
    this.snackBar.open(`${actionName} wiring will be added in the next pass.`, 'Close', {
      duration: 3500,
      horizontalPosition: 'center',
      verticalPosition: 'top',
      panelClass: ['question-editor-success-snackbar']
    });
  }
}
