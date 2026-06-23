import { Location } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, HostBinding, Input, OnDestroy, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import {
  AssessmentAdminService,
  CodingQuestionExample,
  CodingTestCase,
  CreateQuestionRequest,
  PagedQuestionBankResult,
  QuestionClassificationCatalog,
  QuestionBankItem
} from '../../services/api/assessment-admin.service';

type EditorMode = 'coding' | 'non-coding';
type QuestionType = 'mcq' | 'fill in the blanks' | 'coding';

@Component({
  selector: 'app-question-editor',
  standalone: false,
  templateUrl: './question-editor.component.html',
  styleUrl: './question-editor.component.scss'
})
export class QuestionEditorComponent implements OnInit, OnDestroy {
  private static readonly addNewOptionValue = '__add_new__';
  private static readonly fillInTheBlankPlaceholderPattern = /_{3,}/;
  private static readonly codingQuestionType = 'coding';

  @Input() theme: 'college-admin' | 'trainer' = 'college-admin';
  @Input() questionBankRoute = '';
  @Input() heroKicker = 'Shared Repository';

  @HostBinding('class.theme-trainer')
  get isTrainerTheme(): boolean {
    return this.theme === 'trainer';
  }

  @HostBinding('class.theme-college-admin')
  get isCollegeAdminTheme(): boolean {
    return this.theme === 'college-admin';
  }

  readonly difficultyOptions = [
    { value: 1, label: 'Easy' },
    { value: 2, label: 'Medium' },
    { value: 3, label: 'Hard' }
  ];

  readonly questionTypeOptions = [
    { value: 'mcq', label: 'MCQ' },
    { value: 'fill in the blanks', label: 'Fill in the Blanks' }
  ];

  readonly codingLanguageOptions = [
    { value: 'python', label: 'Python' },
    { value: 'javascript', label: 'JavaScript' },
    { value: 'java', label: 'Java' },
    { value: 'csharp', label: 'C#' },
    { value: 'cpp', label: 'C++' },
    { value: 'c', label: 'C' }
  ];

  readonly comparisonModeOptions = [
    { value: 1, label: 'exact' },
    { value: 2, label: 'trimmed' },
    { value: 3, label: 'case_insensitive' },
    { value: 4, label: 'json' },
    { value: 5, label: 'numeric_tolerance' },
    { value: 6, label: 'unordered_json' }
  ];

  readonly form: FormGroup;

  subjectOptions: string[] = [];
  topicOptions: string[] = [];
  streamOptions: string[] = [];
  private catalogSubjectOptions: string[] = [];
  private questionBankTopicsBySubject = new Map<string, string[]>();
  streamSelection = '';
  subjectSelection = '';
  topicSelection = '';

  editorMode: EditorMode = 'non-coding';
  isLoading = false;
  isSaving = false;
  isEditMode = false;
  successMessage = '';
  errorMessage = '';
  private pendingLoadCount = 0;
  private questionId = '';
  private fallbackReturnUrl = '';
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly location: Location,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
    this.form = this.formBuilder.group({
      stream: ['', [Validators.maxLength(100)]],
      subject: ['', [Validators.maxLength(100)]],
      topic: ['', [Validators.maxLength(200)]],
      topicTag: ['', [Validators.required, Validators.maxLength(500)]],
      difficultyLevel: [1, [Validators.required]],
      questionType: ['mcq' as QuestionType, [Validators.required]],
      allowMultipleAnswers: [false],
      questionText: ['', [this.fillInTheBlankQuestionTextValidator()]],
      marks: [1, [Validators.required, Validators.min(0)]],
      negativeMarks: [0, [Validators.required, Validators.min(0)]],
      options: this.formBuilder.array([
        this.createOptionControl(),
        this.createOptionControl(),
        this.createOptionControl(),
        this.createOptionControl()
      ]),
      answer: [''],
      correctAnswers: this.formBuilder.control<string[]>([]),
      explanation: ['', [Validators.maxLength(1000)]],
      questionTitle: ['', [Validators.maxLength(250)]],
      problemStatement: [''],
      detailedDescription: [''],
      inputFormat: [''],
      outputFormat: [''],
      constraintsText: [''],
      defaultLanguageCode: ['python'],
      defaultTimeLimitMs: [3000, [Validators.min(1)]],
      defaultMemoryLimitKb: [262144, [Validators.min(1)]],
      defaultMaxCodeSizeKb: [512, [Validators.min(1)]],
      examples: this.formBuilder.array([this.createExampleGroup()]),
      testCases: this.formBuilder.array([this.createTestCaseGroup(true)])
    });
  }

  ngOnInit(): void {
    this.questionId = this.route.snapshot.paramMap.get('id') ?? '';
    this.isEditMode = this.questionId.length > 0;
    this.fallbackReturnUrl = (history.state?.returnUrl as string | undefined) ?? '';

    this.editorMode = this.resolveEditorMode(this.route.snapshot.queryParamMap.get('question_type'));
    this.applyEditorModeRules();

    if (this.isEditMode) {
      this.loadQuestionForEdit();
    } else {
      this.loadExistingValues();
    }

    this.questionTypeControl.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(value => this.applyQuestionTypeRules((value ?? 'mcq') as QuestionType));

    this.allowMultipleAnswersControl.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.applyQuestionTypeRules((this.questionTypeControl.value ?? 'mcq') as QuestionType));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get optionsArray(): FormArray<FormControl<string | null>> {
    return this.form.get('options') as FormArray<FormControl<string | null>>;
  }

  get examplesArray(): FormArray<FormGroup> {
    return this.form.get('examples') as FormArray<FormGroup>;
  }

  get testCasesArray(): FormArray<FormGroup> {
    return this.form.get('testCases') as FormArray<FormGroup>;
  }

  get questionTypeControl(): FormControl<QuestionType | null> {
    return this.form.get('questionType') as FormControl<QuestionType | null>;
  }

  get answerControl(): FormControl<string | null> {
    return this.form.get('answer') as FormControl<string | null>;
  }

  get allowMultipleAnswersControl(): FormControl<boolean | null> {
    return this.form.get('allowMultipleAnswers') as FormControl<boolean | null>;
  }

  get correctAnswersControl(): FormControl<string[] | null> {
    return this.form.get('correctAnswers') as FormControl<string[] | null>;
  }

  get streamControl(): FormControl<string | null> {
    return this.form.get('stream') as FormControl<string | null>;
  }

  get subjectControl(): FormControl<string | null> {
    return this.form.get('subject') as FormControl<string | null>;
  }

  get topicControl(): FormControl<string | null> {
    return this.form.get('topic') as FormControl<string | null>;
  }

  get topicTagControl(): FormControl<string | null> {
    return this.form.get('topicTag') as FormControl<string | null>;
  }

  get questionTextControl(): FormControl<string | null> {
    return this.form.get('questionText') as FormControl<string | null>;
  }

  get marksControl(): FormControl<number | null> {
    return this.form.get('marks') as FormControl<number | null>;
  }

  get negativeMarksControl(): FormControl<number | null> {
    return this.form.get('negativeMarks') as FormControl<number | null>;
  }

  get difficultyLevelControl(): FormControl<number | null> {
    return this.form.get('difficultyLevel') as FormControl<number | null>;
  }

  get explanationControl(): FormControl<string | null> {
    return this.form.get('explanation') as FormControl<string | null>;
  }

  get questionTitleControl(): FormControl<string | null> {
    return this.form.get('questionTitle') as FormControl<string | null>;
  }

  get problemStatementControl(): FormControl<string | null> {
    return this.form.get('problemStatement') as FormControl<string | null>;
  }

  get detailedDescriptionControl(): FormControl<string | null> {
    return this.form.get('detailedDescription') as FormControl<string | null>;
  }

  get inputFormatControl(): FormControl<string | null> {
    return this.form.get('inputFormat') as FormControl<string | null>;
  }

  get outputFormatControl(): FormControl<string | null> {
    return this.form.get('outputFormat') as FormControl<string | null>;
  }

  get constraintsTextControl(): FormControl<string | null> {
    return this.form.get('constraintsText') as FormControl<string | null>;
  }

  get defaultLanguageCodeControl(): FormControl<string | null> {
    return this.form.get('defaultLanguageCode') as FormControl<string | null>;
  }

  get defaultTimeLimitMsControl(): FormControl<number | null> {
    return this.form.get('defaultTimeLimitMs') as FormControl<number | null>;
  }

  get defaultMemoryLimitKbControl(): FormControl<number | null> {
    return this.form.get('defaultMemoryLimitKb') as FormControl<number | null>;
  }

  get defaultMaxCodeSizeKbControl(): FormControl<number | null> {
    return this.form.get('defaultMaxCodeSizeKb') as FormControl<number | null>;
  }

  get isCodingEditor(): boolean {
    return this.editorMode === 'coding';
  }

  get isMcq(): boolean {
    return this.questionTypeControl.value === 'mcq';
  }

  get isMultiCorrectMcq(): boolean {
    return !this.isCodingEditor && this.isMcq && !!this.allowMultipleAnswersControl.value;
  }

  get usesSelectableOptions(): boolean {
    return !this.isCodingEditor;
  }

  get addNewOptionValue(): string {
    return QuestionEditorComponent.addNewOptionValue;
  }

  get isCustomStream(): boolean {
    return this.streamSelection === this.addNewOptionValue;
  }

  get isCustomSubject(): boolean {
    return this.subjectSelection === this.addNewOptionValue;
  }

  get isCustomTopic(): boolean {
    return this.topicSelection === this.addNewOptionValue;
  }

  get pageTitle(): string {
    if (this.isCodingEditor) {
      return this.isEditMode ? 'Edit Coding Question' : 'Add New Coding Question';
    }

    return this.isEditMode ? 'Edit Question' : 'Add New Question';
  }

  get pageDescription(): string {
    if (this.isCodingEditor) {
      return 'Design a complex algorithmic challenge for technical evaluations.';
    }

    return this.isEditMode
      ? 'Modify the details of this question and keep your shared repository up to date.'
      : 'Create a question and save it directly into the shared repository.';
  }

  get submitButtonLabel(): string {
    if (this.isSaving) {
      return this.isEditMode ? 'Updating...' : 'Saving...';
    }

    return this.isEditMode ? 'Update Question' : 'Save to Repository';
  }

  onStreamSelectionChange(value: string): void {
    this.streamSelection = value;

    if (value === this.addNewOptionValue) {
      this.streamControl.setValue('', { emitEvent: false });
      return;
    }

    this.streamControl.setValue(value, { emitEvent: false });
  }

  onSubjectSelectionChange(value: string): void {
    this.subjectSelection = value;

    if (value === this.addNewOptionValue) {
      this.subjectControl.setValue('', { emitEvent: false });
      this.topicControl.setValue('', { emitEvent: false });
      this.topicSelection = '';
      return;
    }

    this.subjectControl.setValue(value, { emitEvent: false });
    this.syncClassificationSelections();

    if (!this.topicOptions.includes(this.topicControl.value?.trim() ?? '')) {
      this.topicControl.setValue('', { emitEvent: false });
      this.syncClassificationSelections();
    }
  }

  onTopicSelectionChange(value: string): void {
    this.topicSelection = value;

    if (value === this.addNewOptionValue) {
      this.topicControl.setValue('', { emitEvent: false });
      return;
    }

    this.topicControl.setValue(value, { emitEvent: false });
  }

  resetStreamSelection(): void {
    this.streamSelection = '';
    this.streamControl.setValue('', { emitEvent: false });
  }

  resetSubjectSelection(): void {
    this.subjectSelection = '';
    this.subjectControl.setValue('', { emitEvent: false });
    this.topicControl.setValue('', { emitEvent: false });
    this.syncClassificationSelections();
  }

  resetTopicSelection(): void {
    this.topicSelection = '';
    this.topicControl.setValue('', { emitEvent: false });
  }

  addExample(): void {
    this.examplesArray.push(this.createExampleGroup());
  }

  removeExample(index: number): void {
    if (this.examplesArray.length === 1) {
      this.examplesArray.at(0).reset({ input: '', output: '', explanation: '' });
      return;
    }

    this.examplesArray.removeAt(index);
  }

  addTestCase(): void {
    this.testCasesArray.push(this.createTestCaseGroup(false));
  }

  removeTestCase(index: number): void {
    if (this.testCasesArray.length === 1) {
      return;
    }

    this.testCasesArray.removeAt(index);
  }

  goToQuestionBank(): void {
    if (!this.questionBankRoute) {
      return;
    }

    void this.router.navigateByUrl(`/${this.questionBankRoute}`);
  }

  cancel(): void {
    if (window.history.length > 1) {
      this.location.back();
      return;
    }

    if (this.fallbackReturnUrl) {
      void this.router.navigateByUrl(this.fallbackReturnUrl);
      return;
    }

    this.goToQuestionBank();
  }

  closeSuccessMessage(): void {
    this.successMessage = '';
  }

  saveToRepository(): void {
    if (this.isSaving) {
      return;
    }

    this.successMessage = '';
    this.errorMessage = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage = this.getValidationErrorMessage();
      return;
    }

    if (this.parseTopicTags(this.topicTagControl.value).length === 0) {
      this.form.markAllAsTouched();
      this.errorMessage = 'Enter at least one valid topic tag before saving.';
      return;
    }

    const payload = this.buildPayload();
    this.isSaving = true;

    if (this.isEditMode) {
      this.assessmentAdminService.updateQuestion(this.questionId, payload).subscribe({
        next: question => {
          this.successMessage = this.isCodingEditor ? 'Coding question updated successfully.' : 'Question updated successfully.';
          this.errorMessage = '';
          this.patchFormFromQuestion(question);
          this.isSaving = false;
          this.changeDetectorRef.detectChanges();
        },
        error: error => {
          this.errorMessage = this.getQuestionEditErrorMessage(error, 'update');
          this.successMessage = '';
          this.isSaving = false;
          this.changeDetectorRef.detectChanges();
        }
      });

      return;
    }

    this.assessmentAdminService.createQuestions([payload]).subscribe({
      next: () => {
        this.successMessage = this.isCodingEditor ? 'Coding question saved successfully.' : 'Question saved successfully.';
        this.errorMessage = '';
        this.resetForm();
        this.loadExistingValues();
      },
      error: error => {
        this.errorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          (this.isCodingEditor
            ? 'Unable to save the coding question to the repository right now.'
            : 'Unable to save the question to the repository right now.');
        this.successMessage = '';
        this.isSaving = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  getOptionControl(index: number): FormControl<string | null> {
    return this.optionsArray.at(index);
  }

  getOptionLabel(index: number): string {
    return String.fromCharCode(65 + index);
  }

  isCorrectAnswerOptionSelected(index: number): boolean {
    return (this.correctAnswersControl.value ?? []).includes(this.getOptionLabel(index));
  }

  toggleCorrectAnswerOption(index: number, isSelected: boolean): void {
    const label = this.getOptionLabel(index);
    const currentSelections = this.correctAnswersControl.value ?? [];
    const nextSelections = isSelected
      ? [...currentSelections, label]
      : currentSelections.filter(value => value !== label);

    this.correctAnswersControl.setValue(this.normalizeSelectionLabels(nextSelections));
    this.correctAnswersControl.markAsTouched();
  }

  private createOptionControl(): FormControl<string | null> {
    return this.formBuilder.control('', Validators.required);
  }

  private createExampleGroup(example?: CodingQuestionExample): FormGroup {
    return this.formBuilder.group({
      input: [example?.input ?? ''],
      output: [example?.output ?? ''],
      explanation: [example?.explanation ?? '']
    });
  }

  private createTestCaseGroup(isSample: boolean, testCase?: CodingTestCase): FormGroup {
    return this.formBuilder.group({
      testCaseId: [testCase?.testCaseId ?? ''],
      inputFormat: [testCase?.inputFormat ?? 'stdin', [Validators.required]],
      inputData: [testCase?.inputData ?? ''],
      expectedOutput: [testCase?.expectedOutput ?? '', [Validators.required]],
      comparisonMode: [testCase?.comparisonMode ?? 2, [Validators.required, Validators.min(1)]],
      numericTolerance: [testCase?.numericTolerance ?? null],
      isHidden: [testCase?.isHidden ?? false],
      isSample: [testCase?.isSample ?? isSample],
      timeLimitMs: [testCase?.timeLimitMs ?? null],
      memoryLimitKb: [testCase?.memoryLimitKb ?? null]
    });
  }

  private resolveEditorMode(queryValue: string | null): EditorMode {
    return queryValue?.trim().toLowerCase() === 'coding' ? 'coding' : 'non-coding';
  }

  private loadExistingValues(): void {
    if (this.isCodingEditor) {
      this.changeDetectorRef.detectChanges();
      return;
    }

    this.loadQuestionClassificationCatalog();
    if (!this.isEditMode) {
      this.loadQuestionBankOptions();
    }
  }

  private loadQuestionForEdit(): void {
    this.beginLoading();
    this.errorMessage = '';

    if (!this.isCodingEditor) {
      this.loadQuestionClassificationCatalog();
    }

    this.assessmentAdminService.getQuestion(this.questionId, true).subscribe({
      next: question => {
        this.editorMode = question.questionType?.toLowerCase() === QuestionEditorComponent.codingQuestionType
          ? 'coding'
          : 'non-coding';

        if (!this.isCodingEditor) {
          this.streamOptions = this.toDistinctSortedValues([question.stream]);
        }

        this.applyEditorModeRules();
        this.patchFormFromQuestion(question);
        this.endLoading();
        this.changeDetectorRef.detectChanges();
      },
      error: error => {
        this.errorMessage = this.getQuestionEditErrorMessage(error, 'load');
        this.endLoading();
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private applyQuestionBankValues(result: PagedQuestionBankResult): void {
    this.streamOptions = this.toDistinctSortedValues(result.items.map(item => item.stream));
    this.syncClassificationSelections();
    this.changeDetectorRef.detectChanges();
  }

  private applyQuestionClassificationCatalog(catalog: QuestionClassificationCatalog): void {
    const subjects = catalog.subjects ?? [];

    this.catalogSubjectOptions = subjects
      .map(item => item.subjectName?.trim() ?? '')
      .filter(item => item.length > 0)
      .sort((left, right) => left.localeCompare(right));

    this.questionBankTopicsBySubject = new Map<string, string[]>(
      subjects
        .map(item => {
          const subjectName = item.subjectName?.trim() ?? '';
          const topics = this.toDistinctSortedValues((item.topics ?? []).map(topic => topic.topicName));
          return [subjectName, topics] as const;
        })
        .filter(([subjectName]) => subjectName.length > 0)
    );

    this.syncClassificationSelections();
    this.changeDetectorRef.detectChanges();
  }

  private toDistinctSortedValues(values: Array<string | null | undefined>): string[] {
    return [...new Set(values
      .map(value => value?.trim())
      .filter((value): value is string => Boolean(value)))]
      .sort((left, right) => left.localeCompare(right));
  }

  private patchFormFromQuestion(question: QuestionBankItem): void {
    const questionType = (question.questionType?.toLowerCase() ?? 'mcq') as QuestionType;

    if (questionType === 'coding') {
      this.patchCodingQuestion(question);
      return;
    }

    const options = question.options ?? [];
    const correctAnswers = question.correctAnswers?.length
      ? question.correctAnswers
      : this.parseStoredAnswers(question.answer);
    const answerLabels = this.resolveAnswerLabels(options, correctAnswers);
    const allowsMultipleAnswers = question.allowsMultipleAnswers || correctAnswers.length > 1;

    this.form.patchValue({
      stream: question.stream ?? '',
      subject: question.subject ?? '',
      topic: question.topic ?? '',
      topicTag: this.formatTopicTags(question.topicTag),
      difficultyLevel: question.difficultyLevel ?? 1,
      questionType,
      allowMultipleAnswers: allowsMultipleAnswers,
      questionText: question.questionText ?? '',
      marks: question.marks ?? 1,
      negativeMarks: question.negativeMarks ?? 0,
      answer: answerLabels[0] ?? 'A',
      correctAnswers: allowsMultipleAnswers ? answerLabels : [],
      explanation: question.explanation ?? ''
    }, { emitEvent: false });

    this.optionsArray.controls.forEach((control, index) => {
      control.setValue(options[index] ?? '', { emitEvent: false });
    });

    this.syncClassificationSelections();
    this.applyQuestionTypeRules(questionType);
  }

  private patchCodingQuestion(question: QuestionBankItem): void {
    this.setArrayValues(
      this.examplesArray,
      question.examples?.length
        ? question.examples.map(example => this.createExampleGroup(example))
        : [this.createExampleGroup()]
    );

    this.setArrayValues(
      this.testCasesArray,
      question.testCases?.length
        ? question.testCases.map(testCase => this.createTestCaseGroup(!!testCase.isSample, testCase))
        : [this.createTestCaseGroup(true)]
    );

    this.form.patchValue({
      topicTag: this.formatTopicTags(question.topicTag),
      difficultyLevel: question.difficultyLevel ?? 1,
      questionType: 'coding',
      marks: question.marks ?? 100,
      negativeMarks: 0,
      explanation: question.explanation ?? '',
      questionTitle: question.questionTitle ?? '',
      problemStatement: question.problemStatement ?? question.questionText ?? '',
      detailedDescription: question.detailedDescription ?? '',
      inputFormat: question.inputFormat ?? '',
      outputFormat: question.outputFormat ?? '',
      constraintsText: question.constraintsText ?? '',
      defaultLanguageCode: question.defaultLanguageCode ?? 'python',
      defaultTimeLimitMs: question.defaultTimeLimitMs ?? 3000,
      defaultMemoryLimitKb: question.defaultMemoryLimitKb ?? 262144,
      defaultMaxCodeSizeKb: question.defaultMaxCodeSizeKb ?? 512
    }, { emitEvent: false });
  }

  private setArrayValues(target: FormArray<FormGroup>, groups: FormGroup[]): void {
    while (target.length > 0) {
      target.removeAt(0);
    }

    groups.forEach(group => target.push(group));
  }

  private resolveAnswerLabels(options: string[], answers: string[] | null | undefined): string[] {
    const resolvedLabels = (answers ?? [])
      .map(answer => {
        const normalizedAnswer = answer.trim().toLowerCase();
        const selectedIndex = options.findIndex(option => option.trim().toLowerCase() === normalizedAnswer);
        if (selectedIndex >= 0) {
          return this.getOptionLabel(selectedIndex);
        }

        const normalizedLabel = answer.trim().toUpperCase();
        return ['A', 'B', 'C', 'D'].includes(normalizedLabel) ? normalizedLabel : null;
      })
      .filter((value): value is string => !!value);

    return this.normalizeSelectionLabels(resolvedLabels);
  }

  private parseStoredAnswers(answer: string | null | undefined): string[] {
    if (!answer?.trim()) {
      return [];
    }

    try {
      const parsedValue = JSON.parse(answer);
      if (Array.isArray(parsedValue)) {
        return parsedValue
          .map(value => typeof value === 'string' ? value.trim() : '')
          .filter((value): value is string => value.length > 0);
      }
    } catch {
      // Fall back to the legacy single-answer string format.
    }

    return [answer.trim()];
  }

  private getQuestionEditErrorMessage(error: HttpErrorResponse, action: 'load' | 'update'): string {
    const detail = error?.error?.detail;
    const message = error?.error?.message;
    const normalizedMessage = `${detail ?? ''} ${message ?? ''}`.toLowerCase();

    if (normalizedMessage.includes('you can only edit questions that you created') ||
        normalizedMessage.includes('only the user who created this question can update it')) {
      return this.theme === 'trainer'
        ? 'You can only edit questions that you created. Choose one of your own questions or contact your college admin if this question needs to be updated.'
        : 'This question can only be edited by its creator right now.';
    }

    if (normalizedMessage.includes('included in a live assessment')) {
      return 'This question is part of a live assessment, so editing is locked until that assessment is no longer live.';
    }

    return detail ||
      message ||
      (action === 'load'
        ? 'Unable to load this question right now.'
        : 'Unable to update the question right now.');
  }

  private applyEditorModeRules(): void {
    if (this.isCodingEditor) {
      this.streamControl.clearValidators();
      this.subjectControl.clearValidators();
      this.topicControl.clearValidators();
      this.questionTypeControl.setValue('coding', { emitEvent: false });
      this.questionTypeControl.disable({ emitEvent: false });
      this.negativeMarksControl.setValue(0, { emitEvent: false });
      this.negativeMarksControl.disable({ emitEvent: false });
      this.questionTitleControl.addValidators(Validators.required);
      this.problemStatementControl.addValidators(Validators.required);
      this.questionTextControl.clearValidators();
      this.applyQuestionTypeRules('coding');
      return;
    }

    this.questionTypeControl.enable({ emitEvent: false });
    this.negativeMarksControl.enable({ emitEvent: false });
    this.streamControl.setValidators([Validators.required, Validators.maxLength(100)]);
    this.subjectControl.setValidators([Validators.required, Validators.maxLength(100)]);
    this.topicControl.setValidators([Validators.required, Validators.maxLength(200)]);
    this.questionTitleControl.clearValidators();
    this.problemStatementControl.clearValidators();
    this.questionTypeControl.setValue(
      this.questionTypeControl.value === 'fill in the blanks' ? 'fill in the blanks' : 'mcq',
      { emitEvent: false }
    );
    this.applyQuestionTypeRules((this.questionTypeControl.value ?? 'mcq') as QuestionType);
  }

  private applyQuestionTypeRules(questionType: QuestionType): void {
    this.streamControl.updateValueAndValidity({ emitEvent: false });
    this.subjectControl.updateValueAndValidity({ emitEvent: false });
    this.topicControl.updateValueAndValidity({ emitEvent: false });
    this.questionTitleControl.updateValueAndValidity({ emitEvent: false });
    this.problemStatementControl.updateValueAndValidity({ emitEvent: false });

    if (questionType === 'coding') {
      this.syncCodingTestCaseValidators(true);
      this.optionsArray.controls.forEach(control => {
        control.clearValidators();
        control.updateValueAndValidity({ emitEvent: false });
      });
      this.answerControl.clearValidators();
      this.answerControl.updateValueAndValidity({ emitEvent: false });
      this.correctAnswersControl.clearValidators();
      this.correctAnswersControl.updateValueAndValidity({ emitEvent: false });
      this.defaultTimeLimitMsControl.addValidators([Validators.required, Validators.min(1)]);
      this.defaultMemoryLimitKbControl.addValidators([Validators.required, Validators.min(1)]);
      this.defaultMaxCodeSizeKbControl.addValidators([Validators.required, Validators.min(1)]);
      this.questionTextControl.updateValueAndValidity({ emitEvent: false });
      return;
    }

    this.syncCodingTestCaseValidators(false);
    this.defaultTimeLimitMsControl.clearValidators();
    this.defaultMemoryLimitKbControl.clearValidators();
    this.defaultMaxCodeSizeKbControl.clearValidators();

    if (questionType !== 'mcq' && this.allowMultipleAnswersControl.value) {
      this.allowMultipleAnswersControl.setValue(false, { emitEvent: false });
    }

    this.optionsArray.controls.forEach(control => {
      control.addValidators(Validators.required);
      control.updateValueAndValidity({ emitEvent: false });
    });

    const selectedLabels = this.normalizeSelectionLabels(this.correctAnswersControl.value ?? []);
    const currentAnswer = this.answerControl.value ?? '';
    const isMultiCorrectMcq = questionType === 'mcq' && !!this.allowMultipleAnswersControl.value;

    if (isMultiCorrectMcq) {
      this.answerControl.clearValidators();
      this.answerControl.updateValueAndValidity({ emitEvent: false });
      this.correctAnswersControl.setValidators([this.minSelectionCountValidator(1)]);
      this.correctAnswersControl.setValue(
        selectedLabels.length > 0
          ? selectedLabels
          : (['A', 'B', 'C', 'D'].includes(currentAnswer) ? [currentAnswer] : []),
        { emitEvent: false });
      this.correctAnswersControl.updateValueAndValidity({ emitEvent: false });
      this.questionTextControl.updateValueAndValidity({ emitEvent: false });
      return;
    }

    const nextAnswer = ['A', 'B', 'C', 'D'].includes(currentAnswer)
      ? currentAnswer
      : selectedLabels[0] ?? 'A';
    this.answerControl.setValue(nextAnswer, { emitEvent: false });
    this.answerControl.setValidators([Validators.required]);
    this.answerControl.updateValueAndValidity({ emitEvent: false });
    this.correctAnswersControl.clearValidators();
    this.correctAnswersControl.setValue([], { emitEvent: false });
    this.correctAnswersControl.updateValueAndValidity({ emitEvent: false });
    this.questionTextControl.setValidators([Validators.required, this.fillInTheBlankQuestionTextValidator()]);
    this.questionTextControl.updateValueAndValidity({ emitEvent: false });
  }

  private syncCodingTestCaseValidators(isCoding: boolean): void {
    this.testCasesArray.controls.forEach(group => {
      const inputFormatControl = group.get('inputFormat');
      const expectedOutputControl = group.get('expectedOutput');
      const comparisonModeControl = group.get('comparisonMode');

      if (!inputFormatControl || !expectedOutputControl || !comparisonModeControl) {
        return;
      }

      if (isCoding) {
        inputFormatControl.setValidators([Validators.required]);
        expectedOutputControl.setValidators([Validators.required]);
        comparisonModeControl.setValidators([Validators.required, Validators.min(1)]);
      } else {
        inputFormatControl.clearValidators();
        expectedOutputControl.clearValidators();
        comparisonModeControl.clearValidators();
      }

      inputFormatControl.updateValueAndValidity({ emitEvent: false });
      expectedOutputControl.updateValueAndValidity({ emitEvent: false });
      comparisonModeControl.updateValueAndValidity({ emitEvent: false });
    });
  }

  private buildPayload(): CreateQuestionRequest {
    if (this.isCodingEditor) {
      return this.buildCodingPayload();
    }

    const questionType = (this.questionTypeControl.value ?? 'mcq') as QuestionType;
    const correctAnswers = this.getCorrectAnswersPayloadValue(questionType);

    return {
      stream: this.streamControl.value?.trim() ?? '',
      subject: this.subjectControl.value?.trim() ?? '',
      topic: this.topicControl.value?.trim() ?? '',
      topicTag: this.parseTopicTags(this.topicTagControl.value),
      questionType,
      questionText: this.questionTextControl.value?.trim() ?? '',
      options: this.optionsArray.controls
        .map(control => control.value?.trim() ?? '')
        .filter(value => value.length > 0),
      answer: this.buildStoredAnswerPayload(correctAnswers),
      correctAnswers,
      explanation: this.explanationControl.value?.trim() || undefined,
      marks: Number(this.marksControl.value ?? 0),
      negativeMarks: Number(this.negativeMarksControl.value ?? 0),
      difficultyLevel: Number(this.difficultyLevelControl.value ?? 1)
    };
  }

  private buildCodingPayload(): CreateQuestionRequest {
    return {
      topicTag: this.parseTopicTags(this.topicTagControl.value),
      questionType: 'coding',
      questionTitle: this.questionTitleControl.value?.trim() ?? '',
      problemStatement: this.problemStatementControl.value?.trim() ?? '',
      questionText: this.problemStatementControl.value?.trim() ?? '',
      detailedDescription: this.detailedDescriptionControl.value?.trim() || undefined,
      inputFormat: this.inputFormatControl.value?.trim() || undefined,
      outputFormat: this.outputFormatControl.value?.trim() || undefined,
      constraintsText: this.constraintsTextControl.value?.trim() || undefined,
      explanation: this.explanationControl.value?.trim() || undefined,
      marks: Number(this.marksControl.value ?? 0),
      negativeMarks: 0,
      difficultyLevel: Number(this.difficultyLevelControl.value ?? 1),
      defaultLanguageCode: this.defaultLanguageCodeControl.value?.trim() || undefined,
      defaultTimeLimitMs: Number(this.defaultTimeLimitMsControl.value ?? 3000),
      defaultMemoryLimitKb: Number(this.defaultMemoryLimitKbControl.value ?? 262144),
      defaultMaxCodeSizeKb: Number(this.defaultMaxCodeSizeKbControl.value ?? 512),
      examples: this.examplesArray.controls
        .map(group => ({
          input: group.get('input')?.value?.trim() || undefined,
          output: group.get('output')?.value?.trim() || undefined,
          explanation: group.get('explanation')?.value?.trim() || undefined
        }))
        .filter(example => example.input || example.output || example.explanation),
      testCases: this.testCasesArray.controls
        .map(group => ({
          testCaseId: group.get('testCaseId')?.value || undefined,
          inputFormat: group.get('inputFormat')?.value?.trim() || 'stdin',
          inputData: group.get('inputData')?.value?.trim() || undefined,
          expectedOutput: group.get('expectedOutput')?.value?.trim() || undefined,
          comparisonMode: Number(group.get('comparisonMode')?.value ?? 2),
          numericTolerance: this.toNullableNumber(group.get('numericTolerance')?.value),
          isHidden: !!group.get('isHidden')?.value,
          isSample: !!group.get('isSample')?.value,
          timeLimitMs: this.toNullableNumber(group.get('timeLimitMs')?.value),
          memoryLimitKb: this.toNullableNumber(group.get('memoryLimitKb')?.value)
        }))
    };
  }

  private getCorrectAnswersPayloadValue(questionType: QuestionType): string[] {
    const selectedLabels = this.isMultiCorrectMcq
      ? this.correctAnswersControl.value ?? []
      : [this.answerControl.value ?? 'A'];

    return this.normalizeSelectionLabels(selectedLabels)
      .map(label => {
        const selectedIndex = label.charCodeAt(0) - 65;
        return this.getOptionControl(selectedIndex)?.value?.trim() ?? '';
      })
      .filter(value => value.length > 0);
  }

  private buildStoredAnswerPayload(correctAnswers: string[]): string | undefined {
    if (correctAnswers.length === 0) {
      return undefined;
    }

    return correctAnswers.length === 1
      ? correctAnswers[0]
      : JSON.stringify(correctAnswers);
  }

  private parseTopicTags(value: string | null): string[] {
    const normalizedTags = (value ?? '')
      .split(',')
      .map(tag => tag.trim())
      .filter(tag => tag.length > 0);

    return [...new Set(normalizedTags)];
  }

  private formatTopicTags(tags: string[] | null | undefined): string {
    return (tags ?? []).join(', ');
  }

  private syncClassificationSelections(): void {
    if (this.isCodingEditor) {
      return;
    }

    this.subjectOptions = [...this.catalogSubjectOptions];
    this.topicOptions = this.resolveTopicOptions();
    this.streamSelection = this.resolveSelectionValue(this.streamControl.value, this.streamOptions);
    this.subjectSelection = this.resolveSelectionValue(this.subjectControl.value, this.subjectOptions);
    this.topicSelection = this.resolveSelectionValue(this.topicControl.value, this.topicOptions);
  }

  private resolveTopicOptions(): string[] {
    const selectedSubject = this.subjectControl.value?.trim() ?? '';
    if (!selectedSubject) {
      return [];
    }

    return this.questionBankTopicsBySubject.get(selectedSubject) ?? [];
  }

  private resolveSelectionValue(value: string | null, options: string[]): string {
    const normalizedValue = value?.trim() ?? '';

    if (!normalizedValue) {
      return '';
    }

    return options.includes(normalizedValue)
      ? normalizedValue
      : this.addNewOptionValue;
  }

  private resetForm(): void {
    this.form.reset({
      stream: '',
      subject: '',
      topic: '',
      topicTag: '',
      difficultyLevel: 1,
      questionType: this.isCodingEditor ? 'coding' : 'mcq',
      allowMultipleAnswers: false,
      questionText: '',
      marks: this.isCodingEditor ? 100 : 1,
      negativeMarks: 0,
      answer: 'A',
      correctAnswers: [],
      explanation: '',
      questionTitle: '',
      problemStatement: '',
      detailedDescription: '',
      inputFormat: '',
      outputFormat: '',
      constraintsText: '',
      defaultLanguageCode: 'python',
      defaultTimeLimitMs: 3000,
      defaultMemoryLimitKb: 262144,
      defaultMaxCodeSizeKb: 512
    });

    this.optionsArray.controls.forEach(control => control.setValue(''));
    this.setArrayValues(this.examplesArray, [this.createExampleGroup()]);
    this.setArrayValues(this.testCasesArray, [this.createTestCaseGroup(true)]);
    this.applyEditorModeRules();
    this.syncClassificationSelections();
    this.isSaving = false;
    this.changeDetectorRef.detectChanges();
  }

  private fillInTheBlankQuestionTextValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const questionType = (this.form?.get('questionType')?.value ?? 'mcq') as QuestionType;
      if (questionType !== 'fill in the blanks') {
        return null;
      }

      const value = `${control.value ?? ''}`.trim();
      return QuestionEditorComponent.fillInTheBlankPlaceholderPattern.test(value)
        ? null
        : { fillInTheBlankPlaceholder: true };
    };
  }

  private minSelectionCountValidator(minimumCount: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const values = Array.isArray(control.value) ? control.value : [];
      return values.length >= minimumCount
        ? null
        : { minSelectionCount: { required: minimumCount, actual: values.length } };
    };
  }

  private normalizeSelectionLabels(values: string[]): string[] {
    return [...new Set(values
      .map(value => value?.trim().toUpperCase())
      .filter((value): value is string => ['A', 'B', 'C', 'D'].includes(value)))];
  }

  private loadQuestionBankOptions(): void {
    this.beginLoading();

    this.assessmentAdminService.searchQuestionBank({
      pageNumber: 1,
      pageSize: 100
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.applyQuestionBankValues(result);
          this.endLoading();
        },
        error: error => {
          console.error('Failed to load question bank bootstrap data.', error);
          this.streamOptions = [];
          this.syncClassificationSelections();
          this.endLoading();
          this.changeDetectorRef.detectChanges();
        }
      });
  }

  private loadQuestionClassificationCatalog(): void {
    this.beginLoading();

    this.assessmentAdminService.getQuestionClassificationCatalog()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: catalog => {
          this.applyQuestionClassificationCatalog(catalog);
          this.endLoading();
        },
        error: error => {
          console.error('Failed to load question classification catalog.', error);
          this.catalogSubjectOptions = [];
          this.questionBankTopicsBySubject = new Map<string, string[]>();
          this.syncClassificationSelections();
          this.endLoading();
          this.changeDetectorRef.detectChanges();
        }
      });
  }

  private getValidationErrorMessage(): string {
    if (!this.isCodingEditor && this.questionTextControl.hasError('fillInTheBlankPlaceholder')) {
      return 'Fill in the blanks questions must include a blank shown with underscore characters like ____ in the question text.';
    }

    if (this.isCodingEditor) {
      return 'Please complete the required coding question fields before saving.';
    }

    return 'Please complete the required fields before saving.';
  }

  private toNullableNumber(value: unknown): number | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const parsedValue = Number(value);
    return Number.isFinite(parsedValue) ? parsedValue : null;
  }

  private beginLoading(): void {
    this.pendingLoadCount += 1;
    this.isLoading = true;
  }

  private endLoading(): void {
    this.pendingLoadCount = Math.max(0, this.pendingLoadCount - 1);
    this.isLoading = this.pendingLoadCount > 0;
  }
}
