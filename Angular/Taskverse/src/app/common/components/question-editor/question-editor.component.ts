import { Location } from '@angular/common';
import { ChangeDetectorRef, Component, HostBinding, Input, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import {
  AssessmentAdminService,
  AssessmentSubjectTopicCatalog,
  CreateQuestionRequest,
  PagedQuestionBankResult,
  QuestionBankItem
} from '../../services/api/assessment-admin.service';

type QuestionType = 'mcq' | 'fill in the blanks';

@Component({
  selector: 'app-question-editor',
  standalone: false,
  templateUrl: './question-editor.component.html',
  styleUrl: './question-editor.component.scss'
})
export class QuestionEditorComponent implements OnInit {
  private static readonly addNewOptionValue = '__add_new__';
  private static readonly fillInTheBlankPlaceholderPattern = /_{3,}/;

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

  readonly form: FormGroup;

  subjectOptions: string[] = [];
  topicOptions: string[] = [];
  streamOptions: string[] = [];
  subjectCatalog: AssessmentSubjectTopicCatalog = { subjects: [] };
  private questionBankSubjectOptions: string[] = [];
  private questionBankTopicOptions: string[] = [];
  streamSelection = '';
  subjectSelection = '';
  topicSelection = '';

  isLoading = false;
  isSaving = false;
  isEditMode = false;
  successMessage = '';
  errorMessage = '';
  private pendingLoadCount = 0;
  private questionId = '';
  private fallbackReturnUrl = '';

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly location: Location,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
    this.form = this.formBuilder.group({
      stream: ['', [Validators.required, Validators.maxLength(100)]],
      subject: ['', [Validators.required, Validators.maxLength(100)]],
      topic: ['', [Validators.required, Validators.maxLength(200)]],
      topicTag: ['', [Validators.required, Validators.maxLength(500)]],
      difficultyLevel: [1, [Validators.required]],
      questionType: ['mcq' as QuestionType, [Validators.required]],
      questionText: ['', [Validators.required, this.fillInTheBlankQuestionTextValidator()]],
      marks: [1, [Validators.required, Validators.min(0)]],
      negativeMarks: [0, [Validators.required, Validators.min(0)]],
      options: this.formBuilder.array([
        this.createOptionControl(),
        this.createOptionControl(),
        this.createOptionControl(),
        this.createOptionControl()
      ]),
      answer: ['', [Validators.required]],
      explanation: ['', [Validators.maxLength(1000)]]
    });
  }

  ngOnInit(): void {
    this.questionId = this.route.snapshot.paramMap.get('id') ?? '';
    this.isEditMode = this.questionId.length > 0;
    this.fallbackReturnUrl = (history.state?.returnUrl as string | undefined) ?? '';
    this.loadExistingValues();
    if (this.isEditMode) {
      this.loadQuestionForEdit();
    }
    this.applyQuestionTypeRules(this.questionTypeControl.value ?? 'mcq');
    this.questionTypeControl.valueChanges.subscribe(value => {
      this.applyQuestionTypeRules(value ?? 'mcq');
    });
  }

  get optionsArray(): FormArray<FormControl<string | null>> {
    return this.form.get('options') as FormArray<FormControl<string | null>>;
  }

  get questionTypeControl(): FormControl<QuestionType | null> {
    return this.form.get('questionType') as FormControl<QuestionType | null>;
  }

  get answerControl(): FormControl<string | null> {
    return this.form.get('answer') as FormControl<string | null>;
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

  get isMcq(): boolean {
    return this.questionTypeControl.value === 'mcq';
  }

  get usesSelectableOptions(): boolean {
    const questionType = this.questionTypeControl.value;
    return questionType === 'mcq' || questionType === 'fill in the blanks';
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

  get canGoBack(): boolean {
    return window.history.length > 1;
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit Question' : 'Add New Question';
  }

  get pageDescription(): string {
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
      if (this.questionTextControl.hasError('fillInTheBlankPlaceholder')) {
        this.errorMessage = 'Fill in the blanks questions must include a blank shown with underscore characters like ____ in the question text.';
      } else {
        this.errorMessage = 'Please complete the required fields before saving.';
      }
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
          this.successMessage = 'Question updated successfully.';
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
        this.successMessage = 'Question saved successfully.';
        this.errorMessage = '';
        this.resetForm();
        this.loadExistingValues();
      },
      error: error => {
        this.errorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          'Unable to save the question to the repository right now.';
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

  private createOptionControl(): FormControl<string | null> {
    return this.formBuilder.control('', Validators.required);
  }

  private loadExistingValues(): void {
    this.loadQuestionBankOptions();
    this.loadSubjectTopicCatalog();
  }

  private loadQuestionForEdit(): void {
    this.beginLoading();
    this.errorMessage = '';

    this.assessmentAdminService.getQuestion(this.questionId, true).subscribe({
      next: question => {
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
    this.questionBankSubjectOptions = this.toDistinctSortedValues(result.items.map(item => item.subject));
    this.questionBankTopicOptions = this.toDistinctSortedValues(result.items.map(item => item.topic));
    this.syncClassificationSelections();
    this.changeDetectorRef.detectChanges();
  }

  private applySubjectCatalog(subjectCatalog: AssessmentSubjectTopicCatalog): void {
    this.subjectCatalog = subjectCatalog ?? { subjects: [] };
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
    const options = question.options ?? [];
    const answerLabel = this.usesOptionLabelAnswer(questionType)
      ? this.resolveAnswerLabel(options, question.answer)
      : (question.answer ?? '');

    this.form.patchValue({
      stream: question.stream ?? '',
      subject: question.subject ?? '',
      topic: question.topic ?? '',
      topicTag: this.formatTopicTags(question.topicTag),
      difficultyLevel: question.difficultyLevel ?? 1,
      questionType,
      questionText: question.questionText ?? '',
      marks: question.marks ?? 1,
      negativeMarks: question.negativeMarks ?? 0,
      answer: answerLabel,
      explanation: question.explanation ?? ''
    }, { emitEvent: false });

    this.optionsArray.controls.forEach((control, index) => {
      control.setValue(options[index] ?? '', { emitEvent: false });
    });

    this.syncClassificationSelections();
    this.applyQuestionTypeRules(questionType);
  }

  private resolveAnswerLabel(options: string[], answer: string | null | undefined): string {
    const normalizedAnswer = answer?.trim().toLowerCase();
    const selectedIndex = options.findIndex(option => option.trim().toLowerCase() === normalizedAnswer);
    return selectedIndex >= 0 ? this.getOptionLabel(selectedIndex) : 'A';
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

  private applyQuestionTypeRules(questionType: QuestionType): void {
    if (questionType === 'mcq' || questionType === 'fill in the blanks') {
      this.optionsArray.controls.forEach(control => {
        control.addValidators(Validators.required);
        control.updateValueAndValidity({ emitEvent: false });
      });

      if (!['A', 'B', 'C', 'D'].includes(this.answerControl.value ?? '')) {
        this.answerControl.setValue('A', { emitEvent: false });
      }

      this.answerControl.addValidators(Validators.required);
      this.answerControl.updateValueAndValidity({ emitEvent: false });
      this.questionTextControl.updateValueAndValidity({ emitEvent: false });
      return;
    }

    this.optionsArray.controls.forEach(control => {
      control.clearValidators();
      control.setValue('', { emitEvent: false });
      control.updateValueAndValidity({ emitEvent: false });
    });

    this.answerControl.setValue('', { emitEvent: false });
    this.answerControl.addValidators(Validators.required);
    this.answerControl.updateValueAndValidity({ emitEvent: false });
    this.questionTextControl.updateValueAndValidity({ emitEvent: false });
  }

  private buildPayload(): CreateQuestionRequest {
    const questionType = this.questionTypeControl.value ?? 'mcq';

    return {
      stream: this.streamControl.value?.trim() ?? '',
      subject: this.subjectControl.value?.trim() ?? '',
      topic: this.topicControl.value?.trim() ?? '',
      topicTag: this.parseTopicTags(this.topicTagControl.value),
      questionType,
      questionText: this.questionTextControl.value?.trim() ?? '',
      options: questionType === 'mcq' || questionType === 'fill in the blanks'
        ? this.optionsArray.controls
            .map(control => control.value?.trim() ?? '')
            .filter(value => value.length > 0)
        : undefined,
      answer: this.getAnswerPayloadValue(questionType),
      explanation: this.explanationControl.value?.trim() || undefined,
      marks: Number(this.marksControl.value ?? 0),
      negativeMarks: Number(this.negativeMarksControl.value ?? 0),
      difficultyLevel: Number(this.difficultyLevelControl.value ?? 1)
    };
  }

  private getAnswerPayloadValue(questionType: QuestionType): string {
    if (!this.usesOptionLabelAnswer(questionType)) {
      return this.answerControl.value?.trim() ?? '';
    }

    const selectedLabel = this.answerControl.value ?? 'A';
    const selectedIndex = selectedLabel.charCodeAt(0) - 65;
    return this.getOptionControl(selectedIndex)?.value?.trim() ?? '';
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
    const catalogSubjectOptions = this.subjectCatalog.subjects
      .map(subject => subject.subjectName?.trim())
      .filter((subjectName): subjectName is string => Boolean(subjectName));

    this.subjectOptions = this.theme === 'college-admin'
      ? this.toDistinctSortedValues([...catalogSubjectOptions, ...this.questionBankSubjectOptions])
      : catalogSubjectOptions.sort((left, right) => left.localeCompare(right));

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

    const subject = this.subjectCatalog.subjects.find(item => item.subjectName === selectedSubject);
    if (!subject) {
      return this.theme === 'college-admin'
        ? this.questionBankTopicOptions
        : [];
    }

    const catalogTopicOptions = subject.topics
      .map(topic => topic.topicName?.trim())
      .filter((topicName): topicName is string => Boolean(topicName))
      .sort((left, right) => left.localeCompare(right));

    if (this.theme !== 'college-admin') {
      return catalogTopicOptions;
    }

    return this.toDistinctSortedValues([...catalogTopicOptions, ...this.questionBankTopicOptions]);
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
      questionType: 'mcq',
      questionText: '',
      marks: 1,
      negativeMarks: 0,
      answer: 'A',
      explanation: ''
    });

    this.optionsArray.controls.forEach(control => control.setValue(''));
    this.applyQuestionTypeRules('mcq');
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

  private usesOptionLabelAnswer(questionType: QuestionType): boolean {
    return questionType === 'mcq' || questionType === 'fill in the blanks';
  }

  private loadQuestionBankOptions(): void {
    this.beginLoading();

    this.assessmentAdminService.searchQuestionBank({
      pageNumber: 1,
      pageSize: 100
    }).subscribe({
      next: result => {
        this.applyQuestionBankValues(result);
        this.endLoading();
      },
      error: error => {
        console.error('Failed to load question bank bootstrap data.', error);
        this.streamOptions = [];
        this.questionBankSubjectOptions = [];
        this.questionBankTopicOptions = [];
        this.syncClassificationSelections();
        this.endLoading();
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private loadSubjectTopicCatalog(): void {
    this.beginLoading();

    this.assessmentAdminService.getSubjectTopicCatalog().subscribe({
      next: subjectCatalog => {
        this.applySubjectCatalog(subjectCatalog);
        this.endLoading();
      },
      error: error => {
        console.error('Failed to load subject-topic catalog bootstrap data.', error);
        this.subjectCatalog = { subjects: [] };
        this.syncClassificationSelections();
        this.endLoading();
        this.changeDetectorRef.detectChanges();
      }
    });
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
