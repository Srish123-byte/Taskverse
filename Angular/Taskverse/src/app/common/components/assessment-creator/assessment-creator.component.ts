import { ChangeDetectorRef, Component, HostBinding, Input, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AssessmentRecord,
  AssessmentAdminService,
  AssessmentAssignmentBatch,
  AssessmentAssignmentCatalog,
  AssessmentAssignmentClass,
  AssessmentSubjectCatalogItem,
  CreateAssessmentRequest,
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
  private hasStartedAssignmentLoad = false;

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
  passingPercentage: number | null = 50;
  startDate = '';
  endDate = '';
  instructions = '';
  allowLateEntry = false;
  showResultsImmediately = false;
  allowQuestionReview = true;
  negativeMarking = false;
  isSubmitting = false;
  submissionErrorMessage = '';
  private pendingSubmitAction: 'draft' | 'schedule' | null = null;

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

  get selectedQuestions(): QuestionBankItem[] {
    return this.questions.filter(question => this.selectedQuestionIds.has(question.questionId));
  }

  get totalMarks(): number {
    return this.selectedQuestions.reduce((sum, question) => sum + Number(question.marks ?? 0), 0);
  }

  get totalMarksDisplay(): string {
    return this.formatMarks(this.totalMarks);
  }

  get saveDraftButtonLabel(): string {
    if (this.isSubmitting && this.pendingSubmitAction === 'draft') {
      return 'Saving...';
    }

    return 'Save as Draft';
  }

  get scheduleButtonLabel(): string {
    if (this.isSubmitting && this.pendingSubmitAction === 'schedule') {
      return 'Scheduling...';
    }

    return 'Schedule Assessment';
  }

  get isInitialPageLoading(): boolean {
    return this.isCatalogLoading || this.isQuestionBankLoading;
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
    this.submissionErrorMessage = '';

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
    this.submitAssessment('draft');
  }

  scheduleAssessment(): void {
    this.submitAssessment('schedule');
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

  enforcePassingPercentageRange(): void {
    if (this.passingPercentage == null || Number.isNaN(this.passingPercentage)) {
      this.passingPercentage = null;
      return;
    }

    this.passingPercentage = Math.min(100, Math.max(0, this.passingPercentage));
  }

  closeSubmissionError(): void {
    this.submissionErrorMessage = '';
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
        this.maybeStartAssignmentLoad();
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.catalogErrorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          'Unable to load the subject and topic catalog right now.';
        this.isCatalogLoading = false;
        this.maybeStartAssignmentLoad();
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
        this.maybeStartAssignmentLoad();
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.questionBankErrorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          'Unable to load the question bank right now.';
        this.isQuestionBankLoading = false;
        this.maybeStartAssignmentLoad();
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private maybeStartAssignmentLoad(): void {
    if (this.hasStartedAssignmentLoad || this.isCatalogLoading || this.isQuestionBankLoading) {
      return;
    }

    this.hasStartedAssignmentLoad = true;
    this.loadAssignmentCatalog();
  }

  private openDeferredActionMessage(actionName: string): void {
    this.snackBar.open(`${actionName} wiring will be added in the next pass.`, 'Close', {
      duration: 3500,
      horizontalPosition: 'center',
      verticalPosition: 'top',
      panelClass: ['question-editor-success-snackbar']
    });
  }

  private submitAssessment(action: 'draft' | 'schedule'): void {
    if (this.isSubmitting) {
      return;
    }

    const validationError = this.validateAssessmentSubmission();
    if (validationError) {
      this.submissionErrorMessage = validationError;
      this.changeDetectorRef.detectChanges();
      return;
    }

    const payload = this.buildCreateAssessmentPayload();
    if (!payload) {
      this.changeDetectorRef.detectChanges();
      return;
    }

    this.isSubmitting = true;
    this.pendingSubmitAction = action;
    this.submissionErrorMessage = '';

    const request$ = action === 'draft'
      ? this.assessmentAdminService.createAssessment(payload)
      : this.assessmentAdminService.publishAssessment(payload);

    request$.subscribe({
      next: assessment => {
        this.handleSuccessfulSubmission(
          action === 'draft'
            ? 'Assessment saved as draft successfully.'
            : 'Assessment scheduled successfully.',
          assessment);
      },
      error: error => {
        this.handleSubmissionError(
          error?.error?.detail ||
          error?.error?.message ||
          (action === 'draft'
            ? 'Unable to create the assessment right now.'
            : 'Unable to publish the assessment right now.'));
      }
    });
  }

  private handleSuccessfulSubmission(message: string, assessment: AssessmentRecord): void {
    this.isSubmitting = false;
    this.pendingSubmitAction = null;
    this.submissionErrorMessage = '';
    this.snackBar.open(message, 'Close', {
      duration: 3500,
      horizontalPosition: 'center',
      verticalPosition: 'top',
      panelClass: ['question-editor-success-snackbar']
    });
    this.resetBuilderAfterSubmission(assessment);
    this.changeDetectorRef.detectChanges();
  }

  private handleSubmissionError(message: string): void {
    this.isSubmitting = false;
    this.pendingSubmitAction = null;
    this.submissionErrorMessage = message;
    this.changeDetectorRef.detectChanges();
  }

  private validateAssessmentSubmission(): string {
    if (!this.assessmentName.trim()) {
      return 'Assessment name is required before saving.';
    }

    if (!this.selectedSubjectId) {
      return 'Select a subject before saving this assessment.';
    }

    if (!this.selectedTopicId) {
      return 'Select a topic before saving this assessment.';
    }

    if (!this.durationMinutes || this.durationMinutes <= 0) {
      return 'Duration must be greater than zero.';
    }

    if (this.selectedBatchIds.size === 0) {
      return 'Select at least one batch before saving this assessment.';
    }

    if (this.selectedQuestionIds.size === 0) {
      return 'Select at least one question before saving this assessment.';
    }

    return '';
  }

  private buildCreateAssessmentPayload(): CreateAssessmentRequest | null {
    const persistedTotalMarks = this.resolvePersistedTotalMarks();
    if (persistedTotalMarks === null) {
      return null;
    }

    const selectedSubject = this.visibleSubjects.find(subject => subject.subjectId === this.selectedSubjectId);
    const selectedTopic = this.visibleTopics.find(topic => topic.topicId === this.selectedTopicId);

    return {
      assessmentName: this.assessmentName.trim(),
      subjectId: this.selectedSubjectId || null,
      subjectName: selectedSubject?.subjectName ?? null,
      topicId: this.selectedTopicId || null,
      topicName: selectedTopic?.topicName ?? null,
      assignedBatchIds: Array.from(this.selectedBatchIds),
      questionIds: Array.from(this.selectedQuestionIds),
      durationMinutes: Number(this.durationMinutes),
      totalMarks: persistedTotalMarks,
      startDateTime: this.startDate || null,
      endDateTime: this.endDate || null
    };
  }

  private resolvePersistedTotalMarks(): number | null {
    const totalMarks = this.totalMarks;

    if (!Number.isFinite(totalMarks) || totalMarks < 0) {
      this.submissionErrorMessage = 'Total marks could not be calculated from the selected questions.';
      return null;
    }

    if (!Number.isInteger(totalMarks)) {
      this.submissionErrorMessage =
        'Selected question marks currently sum to a fractional total. Assessment total marks can only be persisted as whole numbers right now.';
      return null;
    }

    return totalMarks;
  }

  private resetBuilderAfterSubmission(assessment: AssessmentRecord): void {
    this.assessmentName = '';
    this.selectedSubjectId = '';
    this.selectedTopicId = '';
    this.selectedDifficulty = 'all';
    this.durationMinutes = 60;
    this.passingPercentage = 50;
    this.startDate = '';
    this.endDate = '';
    this.instructions = '';
    this.allowLateEntry = assessment.allowLateEntry;
    this.showResultsImmediately = assessment.showResultsImmediately;
    this.allowQuestionReview = assessment.allowQuestionReview;
    this.negativeMarking = assessment.negativeMarking;
    this.questionSearchTerm = '';
    this.selectedBatchIds.clear();
    this.selectedQuestionIds.clear();
  }

  private formatMarks(totalMarks: number): string {
    if (Number.isInteger(totalMarks)) {
      return totalMarks.toString();
    }

    return totalMarks.toFixed(2).replace(/\.?0+$/, '');
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
