import { Location } from '@angular/common';
import { ChangeDetectorRef, Component, HostBinding, Input, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import {
  AssessmentAdminService,
  CreateQuestionRequest,
  PagedQuestionBankResult
} from '../../services/api/assessment-admin.service';

type QuestionType = 'mcq' | 'fill in the blanks';

@Component({
  selector: 'app-question-editor',
  standalone: false,
  templateUrl: './question-editor.component.html',
  styleUrl: './question-editor.component.scss'
})
export class QuestionEditorComponent implements OnInit {
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

  isLoading = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly location: Location,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly snackBar: MatSnackBar
  ) {
    this.form = this.formBuilder.group({
      stream: ['', [Validators.required, Validators.maxLength(100)]],
      subject: ['', [Validators.required, Validators.maxLength(100)]],
      topic: ['', [Validators.required, Validators.maxLength(200)]],
      topicTag: ['', [Validators.required, Validators.maxLength(200)]],
      difficultyLevel: [1, [Validators.required]],
      questionType: ['mcq' as QuestionType, [Validators.required]],
      questionText: ['', [Validators.required]],
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
    this.loadExistingValues();
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

  get canGoBack(): boolean {
    return window.history.length > 1;
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

    this.goToQuestionBank();
  }

  saveToRepository(): void {
    if (this.isSaving) {
      return;
    }

    this.successMessage = '';
    this.errorMessage = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage = 'Please complete the required fields before saving.';
      return;
    }

    const payload = this.buildPayload();
    this.isSaving = true;

    this.assessmentAdminService.createQuestions([payload]).subscribe({
      next: () => {
        this.successMessage = '';
        this.errorMessage = '';
        this.snackBar.open('Question saved successfully.', 'Close', {
          duration: 3500,
          horizontalPosition: 'right',
          verticalPosition: 'top'
        });
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
    if (this.isLoading) {
      return;
    }

    this.isLoading = true;

    this.assessmentAdminService.searchQuestionBank({
      pageNumber: 1,
      pageSize: 100
    }).subscribe({
      next: result => {
        this.applyExistingValues(result);
      },
      error: () => {
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private applyExistingValues(result: PagedQuestionBankResult): void {
    this.streamOptions = this.toDistinctSortedValues(result.items.map(item => item.stream));
    this.subjectOptions = this.toDistinctSortedValues(result.items.map(item => item.subject));
    this.topicOptions = this.toDistinctSortedValues(result.items.map(item => item.topic));
    this.isLoading = false;
    this.changeDetectorRef.detectChanges();
  }

  private toDistinctSortedValues(values: Array<string | null | undefined>): string[] {
    return [...new Set(values
      .map(value => value?.trim())
      .filter((value): value is string => Boolean(value)))]
      .sort((left, right) => left.localeCompare(right));
  }

  private applyQuestionTypeRules(questionType: QuestionType): void {
    if (questionType === 'mcq') {
      this.optionsArray.controls.forEach(control => {
        control.addValidators(Validators.required);
        control.updateValueAndValidity({ emitEvent: false });
      });

      if (!['A', 'B', 'C', 'D'].includes(this.answerControl.value ?? '')) {
        this.answerControl.setValue('A', { emitEvent: false });
      }

      this.answerControl.addValidators(Validators.required);
      this.answerControl.updateValueAndValidity({ emitEvent: false });
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
  }

  private buildPayload(): CreateQuestionRequest {
    const questionType = this.questionTypeControl.value ?? 'mcq';

    return {
      stream: this.streamControl.value?.trim() ?? '',
      subject: this.subjectControl.value?.trim() ?? '',
      topic: this.topicControl.value?.trim() ?? '',
      topicTag: this.topicTagControl.value?.trim() ?? '',
      questionType,
      questionText: this.questionTextControl.value?.trim() ?? '',
      options: questionType === 'mcq'
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
    if (questionType !== 'mcq') {
      return this.answerControl.value?.trim() ?? '';
    }

    const selectedLabel = this.answerControl.value ?? 'A';
    const selectedIndex = selectedLabel.charCodeAt(0) - 65;
    return this.getOptionControl(selectedIndex)?.value?.trim() ?? '';
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
    this.isSaving = false;
    this.changeDetectorRef.detectChanges();
  }
}
