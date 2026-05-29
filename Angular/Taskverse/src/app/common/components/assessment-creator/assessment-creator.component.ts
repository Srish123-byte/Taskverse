import { ChangeDetectorRef, Component, HostBinding, Input, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AssessmentAdminService,
  AssessmentAssignmentBatch,
  AssessmentAssignmentCatalog,
  AssessmentAssignmentClass,
  AssessmentSubjectCatalogItem,
  AssessmentSubjectTopicCatalog,
  QuestionBankItem
} from '../../services/api/assessment-admin.service';
import { CollegeAdminService, CollegeBatchSummary, CollegeClassSummary } from '../../services/api/college-admin.service';

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
  isAssignmentLoading = true;
  catalogErrorMessage = '';
  questionBankErrorMessage = '';
  assignmentErrorMessage = '';

  subjectCatalog: AssessmentSubjectTopicCatalog = { subjects: [] };
  questions: QuestionBankItem[] = [];
  assignmentCatalog: AssessmentAssignmentCatalog = { classes: [] };

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

  get selectedClassCount(): number {
    return this.selectedAssignmentClasses.length;
  }

  get selectedQuestionCount(): number {
    return this.selectedQuestionIds.size;
  }

  get isInitialPageLoading(): boolean {
    return this.isCatalogLoading || this.isQuestionBankLoading || this.isAssignmentLoading;
  }

  get assignmentClasses(): AssessmentAssignmentClass[] {
    return this.assignmentCatalog.classes;
  }

  get selectedAssignmentClasses(): AssessmentAssignmentClass[] {
    return this.assignmentClasses.filter(classItem =>
      classItem.batches.some(batch => this.selectedBatchIds.has(batch.batchId)));
  }

  get selectedAssignmentBatches(): AssessmentAssignmentBatch[] {
    return this.assignmentClasses.flatMap(classItem =>
      classItem.batches.filter(batch => this.selectedBatchIds.has(batch.batchId)));
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
    private readonly collegeAdminService: CollegeAdminService,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAssignmentCatalog();
    this.loadSubjectTopicCatalog();
    this.loadQuestionBank();
  }

  onSubjectChange(): void {
    if (this.selectedSubjectId &&
        !this.visibleSubjects.some(subject => subject.subjectId === this.selectedSubjectId)) {
      this.selectedSubjectId = '';
    }

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

  trackByAssignmentClassId(_: number, classItem: AssessmentAssignmentClass): string {
    return classItem.classId;
  }

  trackByAssignmentBatchId(_: number, batch: AssessmentAssignmentBatch): string {
    return batch.batchId;
  }

  isBatchSelected(batchId: string): boolean {
    return this.selectedBatchIds.has(batchId);
  }

  toggleBatchSelection(batchId: string): void {
    if (this.selectedBatchIds.has(batchId)) {
      this.selectedBatchIds.delete(batchId);
    } else {
      this.selectedBatchIds.add(batchId);
    }

    this.onSubjectChange();
  }

  private loadAssignmentCatalog(): void {
    this.isAssignmentLoading = true;
    this.assignmentErrorMessage = '';

    if (this.theme === 'trainer') {
      this.assessmentAdminService.getTrainerAssignedClassesAndBatches().subscribe({
        next: catalog => {
          this.assignmentCatalog = catalog ?? { classes: [] };
          this.isAssignmentLoading = false;
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          this.assignmentErrorMessage =
            error?.error?.detail ||
            error?.error?.message ||
            'Unable to load assigned classes and batches right now.';
          this.isAssignmentLoading = false;
          this.changeDetectorRef.detectChanges();
        }
      });

      return;
    }

    this.collegeAdminService.getClassConfiguration().subscribe({
      next: configuration => {
        this.assignmentCatalog = {
          classes: (configuration?.classes ?? []).map(classItem => this.mapCollegeClassToAssignmentClass(classItem))
        };
        this.isAssignmentLoading = false;
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.assignmentErrorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          'Unable to load classes and batches right now.';
        this.isAssignmentLoading = false;
        this.changeDetectorRef.detectChanges();
      }
    });
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

  private mapCollegeClassToAssignmentClass(classItem: CollegeClassSummary): AssessmentAssignmentClass {
    return {
      classId: classItem.classId,
      collegeId: classItem.collegeId,
      name: classItem.name,
      academicYear: classItem.academicYear,
      batches: (classItem.batches ?? []).map(batch => this.mapCollegeBatchToAssignmentBatch(batch))
    };
  }

  private mapCollegeBatchToAssignmentBatch(batch: CollegeBatchSummary): AssessmentAssignmentBatch {
    return {
      batchId: batch.batchId,
      classId: batch.classId,
      collegeId: batch.collegeId,
      name: batch.name
    };
  }
}
