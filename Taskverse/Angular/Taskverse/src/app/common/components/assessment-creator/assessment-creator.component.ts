import { ChangeDetectorRef, Component, HostBinding, Input, OnDestroy, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import {
  AssessmentRecord,
  AssessmentAdminService,
  AssessmentAssignmentBatch,
  AssessmentAssignmentCatalog,
  AssessmentAssignmentClass,
  CreateAssessmentRequest,
  PublishAssessmentRequest,
  QuestionClassificationCatalog,
  QuestionBankItem
} from '../../services/api/assessment-admin.service';
import { RouteAddress } from '../../constants/routes.constants';
import { CollegeAdminService, CollegeBatchSummary, CollegeClassSummary } from '../../services/api/college-admin.service';
import { distinctUntilChanged, forkJoin, map, Subject, switchMap, takeUntil } from 'rxjs';

interface DifficultyOption {
  value: string;
  label: string;
}

interface AssessmentCreatorTopicOption {
  topicId: string;
  topicName: string;
}

interface AssessmentCreatorSubjectOption {
  subjectId: string;
  subjectName: string;
  topics: AssessmentCreatorTopicOption[];
}

type AssessmentBuilderMode = 'create' | 'edit';

@Component({
  selector: 'app-assessment-creator',
  standalone: false,
  templateUrl: './assessment-creator.component.html',
  styleUrl: './assessment-creator.component.scss'
})
export class AssessmentCreatorComponent implements OnInit, OnDestroy {
  private static readonly maxInstructionWordCount = 1000;
  private static readonly questionBankPageSize = 10;
  @Input() theme: 'college-admin' | 'trainer' = 'college-admin';
  @Input() backRoute = '';

  readonly difficultyOptions: DifficultyOption[] = [
    { value: 'all', label: 'All levels' },
    { value: '1', label: 'Easy' },
    { value: '2', label: 'Medium' },
    { value: '3', label: 'Hard' }
  ];

  isQuestionBankLoading = false;
  isAssignmentLoading = false;
  isAssessmentLoading = false;
  questionBankErrorMessage = '';
  assignmentErrorMessage = '';
  assessmentLoadErrorMessage = '';
  private loadedAssessmentRecord: AssessmentRecord | null = null;
  private readonly destroy$ = new Subject<void>();
  private assignmentCatalogLoadSubscription?: Subscription;
  private questionBankLoadSubscription?: Subscription;
  private assessmentLoadSubscription?: Subscription;
  private questionClassificationLoadSubscription?: Subscription;

  questions: QuestionBankItem[] = [];
  questionClassificationCatalog: QuestionClassificationCatalog = { subjects: [] };
  assignmentCatalog: AssessmentAssignmentCatalog = { classes: [] };

  selectedBatchIds = new Set<string>();
  selectedQuestionIds = new Set<string>();

  assessmentName = '';
  selectedSubjectId = '';
  selectedTopicId = '';
  selectedDifficulty = 'all';
  selectedQuestionBankSubjectId = '';
  selectedQuestionBankTopicId = '';
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
  builderMode: AssessmentBuilderMode = 'create';
  editingAssessmentId: string | null = null;
  minimumScheduleDateTime = '';
  private pendingSubmitAction: 'draft' | 'schedule' | null = null;

  questionSearchTerm = '';
  questionBankCurrentPage = 1;
  questionBankTotalCount = 0;

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

  get addQuestionRoute(): string {
    return this.theme === 'trainer'
      ? RouteAddress.Trainer.AddQuestion
      : RouteAddress.CollegeAdmin.AddQuestion;
  }

  get currentAssessmentRoute(): string {
    const baseRoute = this.theme === 'trainer'
      ? (this.isEditMode ? RouteAddress.Trainer.EditAssessment : RouteAddress.Trainer.NewAssessment)
      : (this.isEditMode ? RouteAddress.CollegeAdmin.EditAssessment : RouteAddress.CollegeAdmin.NewAssessment);

    if (this.isEditMode && this.editingAssessmentId) {
      return `${baseRoute}/${this.editingAssessmentId}`;
    }

    return baseRoute;
  }

  get isEditMode(): boolean {
    return this.builderMode === 'edit';
  }

  get pageKicker(): string {
    return this.isEditMode ? 'Edit Assessment' : 'Create New';
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit Assessment' : 'Create New Assessment';
  }

  get pageDescription(): string {
    return this.isEditMode
      ? 'Review and update the selected assessment with the latest batches, questions, timing, and instructions.'
      : 'Design, schedule and assign high-precision assessments with role-aware batch and topic access.';
  }

  get primaryActionLabel(): string {
    if (!this.isEditMode) {
      return this.scheduleButtonLabel;
    }

    if (this.isSubmitting) {
      return this.isDraftAssessment ? 'Publishing...' : 'Updating...';
    }

    return this.isDraftAssessment ? 'Publish Assessment' : 'Update Assessment';
  }

  get isInitialPageLoading(): boolean {
    return (this.isEditMode && this.isAssessmentLoading) || this.shouldShowInitialQuestionBankLoader();
  }

  get hasBlockingLoadError(): boolean {
    return this.isEditMode && !!this.assessmentLoadErrorMessage;
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

  get instructionWordCount(): number {
    return this.countWords(this.instructions);
  }

  get scheduleInputMin(): string | null {
    return this.isEditMode ? null : this.minimumScheduleDateTime;
  }

  get saveDraftButtonLabel(): string {
    if (this.isSubmitting && this.pendingSubmitAction === 'draft') {
      return 'Saving...';
    }

    return 'Save as Draft';
  }

  get isDraftAssessment(): boolean {
    return (this.loadedAssessmentRecord?.assessmentStatus ?? '').trim().toLowerCase() === 'draft';
  }

  get scheduleButtonLabel(): string {
    if (this.isSubmitting && this.pendingSubmitAction === 'schedule') {
      return 'Scheduling...';
    }

    return 'Schedule Assessment';
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
      const previewText = (question.questionText ?? question.problemStatement ?? question.questionTitle ?? '').toLowerCase();
      const matchesSearch =
        normalizedSearch.length === 0 ||
        previewText.includes(normalizedSearch) ||
        (question.subject ?? '').toLowerCase().includes(normalizedSearch) ||
        (question.topic ?? '').toLowerCase().includes(normalizedSearch);

      return matchesSearch;
    });
  }

  get visibleSubjects(): AssessmentCreatorSubjectOption[] {
    const catalogSubjects = this.mapQuestionClassificationSubjects();
    if (catalogSubjects.length > 0) {
      return this.includeAssessmentSubjectIfMissing(catalogSubjects, this.loadedAssessmentRecord);
    }

    return this.buildSubjectOptions(this.questions, this.loadedAssessmentRecord);
  }

  get visibleTopics() {
    const subject = this.visibleSubjects.find(item => item.subjectId === this.selectedSubjectId);
    if (!subject) {
      return [];
    }

    return subject.topics;
  }

  get questionBankSubjects(): AssessmentCreatorSubjectOption[] {
    const catalogSubjects = this.mapQuestionClassificationSubjects();
    return catalogSubjects.length > 0
      ? this.includeAssessmentSubjectIfMissing(catalogSubjects, this.loadedAssessmentRecord)
      : this.visibleSubjects;
  }

  get questionBankTopics() {
    const subject = this.questionBankSubjects.find(item => item.subjectId === this.selectedQuestionBankSubjectId);
    return subject?.topics ?? [];
  }

  get totalQuestionBankPages(): number {
    return Math.max(1, Math.ceil(this.questionBankTotalCount / AssessmentCreatorComponent.questionBankPageSize));
  }

  get questionBankPageStart(): number {
    return this.questionBankTotalCount === 0
      ? 0
      : (this.questionBankCurrentPage - 1) * AssessmentCreatorComponent.questionBankPageSize + 1;
  }

  get questionBankPageEnd(): number {
    return Math.min(
      this.questionBankCurrentPage * AssessmentCreatorComponent.questionBankPageSize,
      this.questionBankTotalCount
    );
  }

  get questionBankPageNumbers(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.questionBankCurrentPage - 2);
    const end = Math.min(this.totalQuestionBankPages, this.questionBankCurrentPage + 2);

    for (let page = start; page <= end; page += 1) {
      pages.push(page);
    }

    return pages;
  }

  constructor(
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly collegeAdminService: CollegeAdminService,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.refreshMinimumScheduleDateTime();
    this.loadQuestionClassificationCatalog();

    this.activatedRoute.paramMap
      .pipe(
        map(paramMap => paramMap.get('id')),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(assessmentId => this.applyRouteContext(assessmentId));
  }

  ngOnDestroy(): void {
    this.assignmentCatalogLoadSubscription?.unsubscribe();
    this.questionBankLoadSubscription?.unsubscribe();
    this.assessmentLoadSubscription?.unsubscribe();
    this.questionClassificationLoadSubscription?.unsubscribe();
    this.destroy$.next();
    this.destroy$.complete();
  }

  saveDraft(): void {
    if (this.isEditMode) {
      this.updateAssessment('draft');
      return;
    }

    this.submitAssessment('draft');
  }

  scheduleAssessment(): void {
    if (this.isEditMode) {
      if (this.isDraftAssessment) {
        this.publishDraftAssessment();
        return;
      }

      this.updateAssessment('update');
      return;
    }

    this.submitAssessment('schedule');
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

  onQuestionBankSubjectChange(): void {
    if (this.selectedQuestionBankSubjectId &&
        !this.questionBankSubjects.some(subject => subject.subjectId === this.selectedQuestionBankSubjectId)) {
      this.selectedQuestionBankSubjectId = '';
    }

    if (this.selectedQuestionBankTopicId &&
        !this.questionBankTopics.some(topic => topic.topicId === this.selectedQuestionBankTopicId)) {
      this.selectedQuestionBankTopicId = '';
    }
  }

  onQuestionBankFilterChange(): void {
    this.onQuestionBankSubjectChange();
    this.questionBankCurrentPage = 1;
    this.loadQuestionBank();
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
    void this.router.navigateByUrl(`/${this.addQuestionRoute}`, {
      state: { returnUrl: `/${this.currentAssessmentRoute}` }
    });
  }

  trackByQuestionId(_: number, question: QuestionBankItem): string {
    return question.questionId;
  }

  trackBySubjectId(_: number, subject: AssessmentCreatorSubjectOption): string {
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

  goToQuestionBankPage(page: number): void {
    if (page < 1 || page > this.totalQuestionBankPages || page === this.questionBankCurrentPage) {
      return;
    }

    this.questionBankCurrentPage = page;
    this.loadQuestionBank();
  }

  previousQuestionBankPage(): void {
    this.goToQuestionBankPage(this.questionBankCurrentPage - 1);
  }

  nextQuestionBankPage(): void {
    this.goToQuestionBankPage(this.questionBankCurrentPage + 1);
  }

  private loadAssignmentCatalog(): void {
    this.assignmentCatalogLoadSubscription?.unsubscribe();
    this.isAssignmentLoading = true;
    this.assignmentErrorMessage = '';

    if (this.theme === 'trainer') {
      this.assignmentCatalogLoadSubscription = this.assessmentAdminService.getTrainerAssignedClassesAndBatches().subscribe({
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

    this.assignmentCatalogLoadSubscription = this.collegeAdminService.getClassConfiguration().subscribe({
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

  private loadQuestionClassificationCatalog(): void {
    this.questionClassificationLoadSubscription?.unsubscribe();
    this.questionClassificationLoadSubscription = this.assessmentAdminService
      .getQuestionClassificationCatalog()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: catalog => {
          this.questionClassificationCatalog = catalog ?? { subjects: [] };
          this.onSubjectChange();
          this.onQuestionBankSubjectChange();
          this.changeDetectorRef.detectChanges();
        },
        error: () => {
          this.questionClassificationCatalog = { subjects: [] };
          this.changeDetectorRef.detectChanges();
        }
      });
  }

  private loadQuestionBank(subjectId?: string | null, topicId?: string | null): void {
    this.questionBankLoadSubscription?.unsubscribe();
    this.isQuestionBankLoading = true;
    this.questionBankErrorMessage = '';

    this.questionBankLoadSubscription = this.assessmentAdminService.searchQuestionBank(
      {
        difficultyLevel: this.selectedDifficulty === 'all' ? undefined : Number(this.selectedDifficulty),
        subjectId: subjectId?.trim() || this.selectedQuestionBankSubjectId || undefined,
        topicId: topicId?.trim() || this.selectedQuestionBankTopicId || undefined,
        pageNumber: this.questionBankCurrentPage,
        pageSize: AssessmentCreatorComponent.questionBankPageSize
      },
      true
    )
      .pipe(takeUntil(this.destroy$))
      .subscribe({
      next: result => {
        this.questions = result?.items ?? [];
        this.questionBankTotalCount = result?.totalCount ?? 0;
        this.questionBankCurrentPage = result?.pageNumber > 0 ? result.pageNumber : this.questionBankCurrentPage;
        this.isQuestionBankLoading = false;
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.questions = [];
        this.questionBankTotalCount = 0;
        this.questionBankErrorMessage = this.getQuestionBankLoadErrorMessage(error);
        this.isQuestionBankLoading = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private applyRouteContext(assessmentId: string | null): void {
    if (assessmentId) {
      this.builderMode = 'edit';
      this.editingAssessmentId = assessmentId;
      this.questionBankCurrentPage = 1;
      this.loadAssignmentCatalog();
      this.loadAssessmentForEdit(assessmentId);
      return;
    }

    this.builderMode = 'create';
    this.editingAssessmentId = null;
    this.loadedAssessmentRecord = null;
    this.isAssessmentLoading = false;
    this.assessmentLoadErrorMessage = '';
    this.resetBuilderForm();
    this.refreshMinimumScheduleDateTime();
    this.loadQuestionBank();
    this.loadAssignmentCatalog();
    this.changeDetectorRef.detectChanges();
  }

  private submitAssessment(action: 'draft' | 'schedule'): void {
    if (this.isSubmitting) {
      return;
    }

    const validationError = this.validateAssessmentSubmission(action);
    if (validationError) {
      this.submissionErrorMessage = validationError;
      this.changeDetectorRef.detectChanges();
      return;
    }

    const payload = this.buildCreateAssessmentPayload(action);
    if (!payload) {
      this.changeDetectorRef.detectChanges();
      return;
    }

    this.isSubmitting = true;
    this.pendingSubmitAction = action;
    this.submissionErrorMessage = '';

    const request$ = action === 'draft'
      ? this.assessmentAdminService.createAssessment(payload, true)
      : this.assessmentAdminService.publishAssessment(payload, true);

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
          this.getAssessmentSubmissionErrorMessage(error, action));
      }
    });
  }

  private updateAssessment(action: 'draft' | 'update'): void {
    if (this.isSubmitting || !this.editingAssessmentId) {
      return;
    }

    const validationError = this.validateAssessmentSubmission(action);
    if (validationError) {
      this.submissionErrorMessage = validationError;
      this.changeDetectorRef.detectChanges();
      return;
    }

    const payload = this.buildCreateAssessmentPayload(action);
    if (!payload) {
      this.changeDetectorRef.detectChanges();
      return;
    }

    payload.isDraftSave = action === 'draft';

    this.isSubmitting = true;
    this.pendingSubmitAction = action === 'draft' ? 'draft' : null;
    this.submissionErrorMessage = '';

    this.assessmentAdminService.updateAssessment(this.editingAssessmentId, payload, true).subscribe({
      next: assessment => {
        this.isSubmitting = false;
        this.submissionErrorMessage = '';
        this.applyAssessmentRecord(assessment);
        this.snackBar.open(action === 'draft' ? 'Assessment saved as draft successfully.' : 'Assessment updated successfully.', 'Close', {
          duration: 3500,
          horizontalPosition: 'center',
          verticalPosition: 'top',
          panelClass: ['question-editor-success-snackbar']
        });
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.handleSubmissionError(
          error?.error?.detail ||
          error?.error?.message ||
          'Unable to update the assessment right now.');
      }
    });
  }

  private publishDraftAssessment(): void {
    if (this.isSubmitting || !this.editingAssessmentId) {
      return;
    }

    const validationError = this.validateAssessmentSubmission('schedule');
    if (validationError) {
      this.submissionErrorMessage = validationError;
      this.changeDetectorRef.detectChanges();
      return;
    }

    const payload = this.buildPublishAssessmentPayload(this.editingAssessmentId);
    if (!payload) {
      this.changeDetectorRef.detectChanges();
      return;
    }

    const draftSyncPayload: CreateAssessmentRequest = {
      ...payload,
      isDraftSave: true
    };

    this.isSubmitting = true;
    this.pendingSubmitAction = null;
    this.submissionErrorMessage = '';

    this.assessmentAdminService
      .updateAssessment(this.editingAssessmentId, draftSyncPayload, true)
      .pipe(switchMap(() => this.assessmentAdminService.publishAssessment(payload, true)))
      .subscribe({
      next: assessment => {
        this.isSubmitting = false;
        this.submissionErrorMessage = '';
        this.applyAssessmentRecord(assessment);
        this.snackBar.open('Assessment published successfully.', 'Close', {
          duration: 3500,
          horizontalPosition: 'center',
          verticalPosition: 'top',
          panelClass: ['question-editor-success-snackbar']
        });
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.handleSubmissionError(this.getAssessmentSubmissionErrorMessage(error, 'schedule'));
      }
      });
  }

  private shouldShowInitialQuestionBankLoader(): boolean {
    return this.isQuestionBankLoading && this.questions.length === 0 && !this.questionBankErrorMessage;
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

  private getAssessmentSubmissionErrorMessage(error: any, action: 'draft' | 'schedule'): string {
    const detail = error?.error?.detail;
    const message = error?.error?.message;
    const normalizedMessage = `${detail ?? ''} ${message ?? ''}`.toLowerCase();

    if (normalizedMessage.includes('already been created for the selected batches')) {
      return 'Assessment has already been created for the selected batches.';
    }

    return detail ||
      message ||
      (action === 'draft'
        ? 'Unable to create the assessment right now.'
        : 'Unable to publish the assessment right now.');
  }

  private validateAssessmentSubmission(action: 'draft' | 'schedule' | 'update'): string {
    if (action === 'draft') {
      return '';
    }

    const startDateTime = this.parseDateTimeLocalValue(this.startDate);
    const endDateTime = this.parseDateTimeLocalValue(this.endDate);

    if (startDateTime && endDateTime && endDateTime <= startDateTime) {
      return 'End time must be later than the start time.';
    }

    if (this.instructionWordCount > AssessmentCreatorComponent.maxInstructionWordCount) {
      return `Instructions cannot exceed ${AssessmentCreatorComponent.maxInstructionWordCount} words.`;
    }

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

    const now = new Date();
    if (startDateTime && startDateTime < now) {
      return 'Start time must be later than the current time.';
    }

    if (endDateTime && endDateTime < now) {
      return 'End time must be later than the current time.';
    }

    if (this.selectedBatchIds.size === 0) {
      return 'Select at least one batch before saving this assessment.';
    }

    if (this.selectedQuestionIds.size === 0) {
      return 'Select at least one question before saving this assessment.';
    }

    return '';
  }

  private buildCreateAssessmentPayload(action: 'draft' | 'schedule' | 'update' = 'schedule'): CreateAssessmentRequest | null {
    const persistedTotalMarks = this.resolvePersistedTotalMarks(action === 'draft');
    if (persistedTotalMarks === null) {
      return null;
    }

    const passingPercentage = this.resolvePassingPercentage(action === 'draft');
    if (passingPercentage === null) {
      return null;
    }

    const selectedSubject = this.visibleSubjects.find(subject => subject.subjectId === this.selectedSubjectId);
    const selectedTopic = selectedSubject?.topics.find(topic => topic.topicId === this.selectedTopicId);

    return {
      assessmentName: this.assessmentName.trim(),
      subjectId: this.selectedSubjectId || null,
      subjectName: selectedSubject?.subjectName ?? null,
      topicId: this.selectedTopicId || null,
      topicName: selectedTopic?.topicName ?? null,
      instructions: this.normalizeInstructions(),
      allowLateEntry: this.allowLateEntry,
      allowQuestionReview: this.allowQuestionReview,
      negativeMarking: this.negativeMarking,
      passingPercentage,
      assignedBatchIds: Array.from(this.selectedBatchIds),
      questionIds: Array.from(this.selectedQuestionIds),
      durationMinutes: Number(this.durationMinutes),
      totalMarks: persistedTotalMarks,
      startDateTime: this.toUtcApiDateTimeValue(this.startDate),
      endDateTime: this.toUtcApiDateTimeValue(this.endDate)
    };
  }

  private buildPublishAssessmentPayload(assessmentId?: string | null): PublishAssessmentRequest | null {
    const payload = this.buildCreateAssessmentPayload('schedule');
    if (!payload) {
      return null;
    }

    return {
      ...payload,
      assessmentId: assessmentId ?? null
    };
  }

  private resolvePersistedTotalMarks(allowDraftFallback = false): number | null {
    const totalMarks = this.totalMarks;

    if (allowDraftFallback) {
      if (!Number.isFinite(totalMarks) || totalMarks < 0) {
        return 0;
      }

      if (!Number.isInteger(totalMarks)) {
        return Math.max(0, Math.floor(totalMarks));
      }

      return totalMarks;
    }

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

  private resolvePassingPercentage(allowDraftFallback = false): number | null {
    if (allowDraftFallback) {
      if (this.passingPercentage == null || !Number.isFinite(this.passingPercentage)) {
        return 0;
      }

      return Math.min(100, Math.max(0, Math.floor(this.passingPercentage)));
    }

    if (this.passingPercentage == null || !Number.isFinite(this.passingPercentage)) {
      this.submissionErrorMessage = 'Passing percentage is required.';
      return null;
    }

    if (!Number.isInteger(this.passingPercentage)) {
      this.submissionErrorMessage = 'Passing percentage must be a whole number.';
      return null;
    }

    if (this.passingPercentage < 0 || this.passingPercentage > 100) {
      this.submissionErrorMessage = 'Passing percentage must be between 0 and 100.';
      return null;
    }

    return this.passingPercentage;
  }

  private loadAssessmentForEdit(assessmentId: string): void {
    this.assessmentLoadSubscription?.unsubscribe();
    this.isAssessmentLoading = true;
    this.assessmentLoadErrorMessage = '';
    this.submissionErrorMessage = '';

    this.assessmentLoadSubscription = this.assessmentAdminService.getAssessment(assessmentId).subscribe({
      next: assessment => {
        this.applyAssessmentRecord(assessment);
        this.loadQuestionBank();
        this.isAssessmentLoading = false;
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.isAssessmentLoading = false;
        this.assessmentLoadErrorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          'Unable to load the selected assessment right now.';
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private applyAssessmentRecord(assessment: AssessmentRecord): void {
    this.loadedAssessmentRecord = assessment;
    this.assessmentName = assessment.assessmentName ?? '';
    this.selectedSubjectId = assessment.subjectId ?? '';
    this.selectedTopicId = assessment.topicId ?? '';
    this.selectedDifficulty = 'all';
    this.selectedQuestionBankSubjectId = assessment.subjectId ?? '';
    this.selectedQuestionBankTopicId = assessment.topicId ?? '';
    this.durationMinutes = assessment.durationMinutes ?? 60;
    this.passingPercentage = assessment.passingPercentage ?? 50;
    this.startDate = this.toDateTimeLocalInputValue(assessment.startDateTime);
    this.endDate = this.toDateTimeLocalInputValue(assessment.endDateTime);
    this.instructions = assessment.instructions ?? '';
    this.allowLateEntry = assessment.allowLateEntry;
    this.showResultsImmediately = assessment.showResultsImmediately;
    this.allowQuestionReview = assessment.allowQuestionReview;
    this.negativeMarking = assessment.negativeMarking;
    this.selectedBatchIds = new Set(assessment.assignedBatchIds ?? []);
    this.selectedQuestionIds = new Set(assessment.questionIds ?? []);
    this.questionSearchTerm = '';
    this.ensureSelectedQuestionsLoaded(assessment.questionIds ?? []);
    this.onSubjectChange();
    this.onQuestionBankSubjectChange();
  }

  private resetBuilderAfterSubmission(assessment: AssessmentRecord): void {
    if (this.isEditMode) {
      this.applyAssessmentRecord(assessment);
      return;
    }

    this.resetBuilderForm();
    this.allowLateEntry = assessment.allowLateEntry;
    this.showResultsImmediately = assessment.showResultsImmediately;
    this.allowQuestionReview = assessment.allowQuestionReview;
    this.negativeMarking = assessment.negativeMarking;
  }

  private resetBuilderForm(): void {
    this.loadedAssessmentRecord = null;
    this.assessmentName = '';
    this.selectedSubjectId = '';
    this.selectedTopicId = '';
    this.selectedDifficulty = 'all';
    this.selectedQuestionBankSubjectId = '';
    this.selectedQuestionBankTopicId = '';
    this.durationMinutes = 60;
    this.passingPercentage = 50;
    this.startDate = '';
    this.endDate = '';
    this.instructions = '';
    this.allowLateEntry = false;
    this.showResultsImmediately = false;
    this.allowQuestionReview = true;
    this.negativeMarking = false;
    this.questionSearchTerm = '';
    this.questionBankCurrentPage = 1;
    this.questionBankTotalCount = 0;
    this.selectedBatchIds.clear();
    this.selectedQuestionIds.clear();
  }

  private formatMarks(totalMarks: number): string {
    if (Number.isInteger(totalMarks)) {
      return totalMarks.toString();
    }

    return totalMarks.toFixed(2).replace(/\.?0+$/, '');
  }

  private normalizeInstructions(): string | null {
    const normalized = this.instructions.trim();
    return normalized.length > 0 ? normalized : null;
  }

  private countWords(value: string): number {
    const normalized = value.trim();
    return normalized ? normalized.split(/\s+/).length : 0;
  }

  private parseDateTimeLocalValue(value: string): Date | null {
    if (!value.trim()) {
      return null;
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }

  private formatDateTimeLocalValue(value: Date): string {
    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    const hours = `${value.getHours()}`.padStart(2, '0');
    const minutes = `${value.getMinutes()}`.padStart(2, '0');

    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  private refreshMinimumScheduleDateTime(): void {
    this.minimumScheduleDateTime = this.formatDateTimeLocalValue(new Date());
  }

  private toUtcApiDateTimeValue(value: string): string | null {
    const parsed = this.parseDateTimeLocalValue(value);
    return parsed ? parsed.toISOString() : null;
  }

  private toDateTimeLocalInputValue(value?: string | null): string {
    if (!value) {
      return '';
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
      return '';
    }

    return this.formatDateTimeLocalValue(parsed);
  }

  private buildSubjectOptions(
    questions: QuestionBankItem[],
    assessment?: AssessmentRecord | null): AssessmentCreatorSubjectOption[] {
    const subjectMap = new Map<string, AssessmentCreatorSubjectOption>();

    for (const question of questions) {
      const subjectId = question.subjectId?.trim();
      const subjectName = question.subject?.trim();
      const topicId = question.topicId?.trim();
      const topicName = question.topic?.trim();

      if (!subjectId || !subjectName) {
        continue;
      }

      const subject = subjectMap.get(subjectId) ?? {
        subjectId,
        subjectName,
        topics: []
      };

      if (!subjectMap.has(subjectId)) {
        subjectMap.set(subjectId, subject);
      }

      if (topicId && topicName && !subject.topics.some(topic => topic.topicId === topicId)) {
        subject.topics.push({ topicId, topicName });
      }
    }

    const subjectId = assessment?.subjectId?.trim();
    const subjectName = assessment?.subjectName?.trim();
    const topicId = assessment?.topicId?.trim();
    const topicName = assessment?.topicName?.trim();

    if (subjectId && subjectName) {
      const subject = subjectMap.get(subjectId) ?? {
        subjectId,
        subjectName,
        topics: []
      };

      if (!subjectMap.has(subjectId)) {
        subjectMap.set(subjectId, subject);
      }

      if (topicId && topicName && !subject.topics.some(topic => topic.topicId === topicId)) {
        subject.topics.push({ topicId, topicName });
      }
    }

    return Array.from(subjectMap.values())
      .map(subject => ({
        ...subject,
        topics: subject.topics.sort((left, right) => left.topicName.localeCompare(right.topicName))
      }))
      .sort((left, right) => left.subjectName.localeCompare(right.subjectName));
  }

  private mapQuestionClassificationSubjects(): AssessmentCreatorSubjectOption[] {
    return (this.questionClassificationCatalog.subjects ?? [])
      .map(subject => ({
        subjectId: subject.subjectId,
        subjectName: subject.subjectName,
        topics: (subject.topics ?? [])
          .map(topic => ({
            topicId: topic.topicId,
            topicName: topic.topicName
          }))
          .sort((left, right) => left.topicName.localeCompare(right.topicName))
      }))
      .sort((left, right) => left.subjectName.localeCompare(right.subjectName));
  }

  private includeAssessmentSubjectIfMissing(
    subjects: AssessmentCreatorSubjectOption[],
    assessment?: AssessmentRecord | null
  ): AssessmentCreatorSubjectOption[] {
    const subjectId = assessment?.subjectId?.trim();
    const subjectName = assessment?.subjectName?.trim();
    const topicId = assessment?.topicId?.trim();
    const topicName = assessment?.topicName?.trim();

    if (!subjectId || !subjectName) {
      return subjects;
    }

    const subjectMap = new Map<string, AssessmentCreatorSubjectOption>(
      subjects.map(subject => [subject.subjectId, {
        subjectId: subject.subjectId,
        subjectName: subject.subjectName,
        topics: [...subject.topics]
      }])
    );

    const subject = subjectMap.get(subjectId) ?? {
      subjectId,
      subjectName,
      topics: []
    };

    if (topicId && topicName && !subject.topics.some(topic => topic.topicId === topicId)) {
      subject.topics.push({ topicId, topicName });
      subject.topics.sort((left: AssessmentCreatorTopicOption, right: AssessmentCreatorTopicOption) =>
        left.topicName.localeCompare(right.topicName));
    }

    subjectMap.set(subjectId, subject);

    return Array.from(subjectMap.values())
      .sort((left, right) => left.subjectName.localeCompare(right.subjectName));
  }

  private ensureSelectedQuestionsLoaded(questionIds: string[]): void {
    const missingQuestionIds = questionIds.filter(questionId =>
      !!questionId && !this.questions.some(question => question.questionId === questionId));

    if (missingQuestionIds.length === 0) {
      return;
    }

    forkJoin(
      missingQuestionIds.map(questionId => this.assessmentAdminService.getQuestion(questionId, true))
    )
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: questions => {
          this.mergeQuestions(questions.filter((question): question is QuestionBankItem => !!question));
          this.onSubjectChange();
          this.onQuestionBankSubjectChange();
          this.changeDetectorRef.detectChanges();
        },
        error: () => {
          this.changeDetectorRef.detectChanges();
        }
      });
  }

  private mergeQuestions(questions: QuestionBankItem[]): void {
    if (questions.length === 0) {
      return;
    }

    const questionMap = new Map(this.questions.map(question => [question.questionId, question] as const));

    for (const question of questions) {
      questionMap.set(question.questionId, question);
    }

    this.questions = Array.from(questionMap.values());
  }

  private getQuestionBankLoadErrorMessage(error: any): string {
    const serverMessage = `${error?.error?.detail ?? ''} ${error?.error?.message ?? ''}`.toLowerCase();

    if (serverMessage.includes('i/o operation has been aborted') ||
        serverMessage.includes('request was canceled') ||
        error?.status === 499) {
      return 'Question bank loading was interrupted. Please try again.';
    }

    if (error?.status === 503) {
      return 'Question bank is temporarily unavailable. Please try again in a moment.';
    }

    return 'Unable to load the question bank right now.';
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
