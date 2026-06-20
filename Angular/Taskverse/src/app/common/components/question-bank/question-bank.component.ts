import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, HostBinding, Input, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import {
  AssessmentAdminService,
  CreateQuestionRequest,
  DeleteQuestionsResponse,
  PagedQuestionBankResult,
  QuestionBankItem,
  QuestionBankSearchRequest
} from '../../services/api/assessment-admin.service';
import {
  ParsedQuestionImportFile,
  QuestionImportParserService
} from '../../services/utilities/question-import-parser.service';

interface SelectOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-question-bank',
  standalone: false,
  templateUrl: './question-bank.component.html',
  styleUrl: './question-bank.component.scss'
})
export class QuestionBankComponent implements OnInit {
  private static readonly editErrorSnackBarConfig = {
    duration: 4500,
    horizontalPosition: 'center' as const,
    verticalPosition: 'top' as const,
    panelClass: ['question-bank-restriction-snackbar']
  };
  private static readonly deleteSuccessSnackBarConfig = {
    duration: 4000,
    horizontalPosition: 'center' as const,
    verticalPosition: 'top' as const,
    panelClass: ['question-editor-success-snackbar']
  };

  @Input() heroKicker = 'Shared Repository';
  @Input() pageTitle = 'Question Bank';
  @Input() pageWelcome = 'Manage and organize your institution\'s shared question repository.';
  @Input() theme: 'college-admin' | 'trainer' = 'college-admin';
  @Input() addQuestionRoute = '';
  @Input() editQuestionRoute = '';

  readonly pageSize = 10;
  readonly difficultyOptions: SelectOption[] = [
    { value: 'all', label: 'Difficulty' },
    { value: '1', label: 'Easy' },
    { value: '2', label: 'Medium' },
    { value: '3', label: 'Hard' }
  ];

  questions: QuestionBankItem[] = [];
  filteredQuestions: QuestionBankItem[] = [];
  availableSubjects: SelectOption[] = [];
  availableTopics: SelectOption[] = [];
  availableTypes: SelectOption[] = [{ value: 'all', label: 'Type' }];

  searchTerm = '';
  selectedSubject = 'all';
  selectedTopic = 'all';
  selectedDifficulty = 'all';
  selectedType = 'all';

  currentPage = 1;
  totalCount = 0;
  deletingQuestionId: string | null = null;

  isLoading = false;
  isUploading = false;
  errorMessage = '';
  infoMessage = '';
  uploadMessage = '';
  uploadErrorMessage = '';

  @HostBinding('class.theme-trainer')
  get isTrainerTheme(): boolean {
    return this.theme === 'trainer';
  }

  @HostBinding('class.theme-college-admin')
  get isCollegeAdminTheme(): boolean {
    return this.theme === 'college-admin';
  }

  get addQuestionRouteSegments(): string[] {
    return this.addQuestionRoute.split('/').filter(segment => segment.length > 0);
  }

  get editQuestionRouteSegments(): string[] {
    return this.editQuestionRoute.split('/').filter(segment => segment.length > 0);
  }

  constructor(
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly questionImportParserService: QuestionImportParserService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadQuestions();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get pageStart(): number {
    return this.totalCount === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalCount);
  }

  get pageNumbers(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.currentPage - 2);
    const end = Math.min(this.totalPages, this.currentPage + 2);

    for (let page = start; page <= end; page += 1) {
      pages.push(page);
    }

    return pages;
  }

  trackByQuestionId(_: number, question: QuestionBankItem): string {
    return question.questionId;
  }

  onServerFilterChange(): void {
    this.currentPage = 1;
    this.loadQuestions();
  }

  onClientFilterChange(): void {
    this.applyClientFilters();
  }

  resetFilters(): void {
    this.searchTerm = '';
    this.selectedSubject = 'all';
    this.selectedTopic = 'all';
    this.selectedDifficulty = 'all';
    this.selectedType = 'all';
    this.currentPage = 1;
    this.loadQuestions();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }

    this.currentPage = page;
    this.loadQuestions();
  }

  prevPage(): void {
    this.goToPage(this.currentPage - 1);
  }

  nextPage(): void {
    this.goToPage(this.currentPage + 1);
  }

  openBulkUpload(fileInput: HTMLInputElement): void {
    if (this.isUploading) {
      return;
    }

    fileInput.click();
  }

  onBulkUploadSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    this.isUploading = true;
    this.uploadMessage = '';
    this.uploadErrorMessage = '';

    this.questionImportParserService.parse(file)
      .then(parsedFile => this.submitBulkUpload(parsedFile, input))
      .catch(error => {
        this.uploadErrorMessage = error instanceof Error
          ? error.message
          : 'The selected file could not be parsed. Please review the file and try again.';
        this.isUploading = false;
        input.value = '';
        this.changeDetectorRef.detectChanges();
      });
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

  getQuestionTypeLabel(questionType: string): string {
    return questionType
      .split(' ')
      .filter(segment => segment.length > 0)
      .map(segment => segment.charAt(0).toUpperCase() + segment.slice(1))
      .join(' ');
  }

  editQuestion(questionId: string): void {
    if (!questionId || this.editQuestionRouteSegments.length === 0) {
      return;
    }

    this.assessmentAdminService.getQuestion(questionId, true).subscribe({
      next: () => {
        if (this.editQuestionRouteSegments.length === 0) {
          return;
        }

        void this.router.navigate(
          ['/', ...this.editQuestionRouteSegments, questionId],
          {
            state: {
              returnUrl: this.router.url
            }
          });
      },
      error: error => {
        const message = this.getEditRestrictionMessage(error);
        this.snackBar.open(message, 'Close', QuestionBankComponent.editErrorSnackBarConfig);
      }
    });
  }

  deleteQuestion(question: QuestionBankItem): void {
    if (!question?.questionId || this.deletingQuestionId) {
      return;
    }

    this.deletingQuestionId = question.questionId;

    this.assessmentAdminService.deleteQuestions(
      { questionIds: [question.questionId] },
      true
    ).subscribe({
      next: response => {
        this.handleDeleteSuccess(question.questionId, response);
      },
      error: error => {
        this.deletingQuestionId = null;
        const message = this.getDeleteRestrictionMessage(error);
        this.snackBar.open(message, 'Close', QuestionBankComponent.editErrorSnackBarConfig);
      }
    });
  }

  private getEditRestrictionMessage(error: HttpErrorResponse): string {
    const detail = error?.error?.detail;
    const message = error?.error?.message;
    const normalizedMessage = `${detail ?? ''} ${message ?? ''}`.toLowerCase();

    if (normalizedMessage.includes('you can only edit questions that you created') ||
        normalizedMessage.includes('only the user who created this question can update it')) {
      return this.theme === 'trainer'
        ? 'You can only edit questions that you created.'
        : 'This question cannot be edited right now.';
    }

    if (normalizedMessage.includes('included in a live assessment')) {
      return 'This question is part of a live assessment, so it cannot be edited right now.';
    }

    return message || detail || 'Unable to open this question for editing right now.';
  }

  private getDeleteRestrictionMessage(error: HttpErrorResponse): string {
    const detail = error?.error?.detail;
    const message = error?.error?.message;
    const normalizedMessage = `${detail ?? ''} ${message ?? ''}`.toLowerCase();

    if (normalizedMessage.includes('scheduled assessment')) {
      return 'Delete the question from the scheduled assessment(s) and try again.';
    }

    if (normalizedMessage.includes('live/completed assessment')) {
      return 'Deleting a question in the Live/Completed assessment(s) isn\'t allowed';
    }

    if (normalizedMessage.includes('not authorized to delete this question') ||
        normalizedMessage.includes('only the user who created a question can delete it')) {
      return 'You\'re not authorized to delete this question. Please try deleting a question you\'ve created';
    }

    return message || detail || 'Unable to delete this question right now.';
  }

  private handleDeleteSuccess(questionId: string, response: DeleteQuestionsResponse): void {
    const deletedQuestionIds = response.deletedQuestionIds ?? [];
    if (!deletedQuestionIds.includes(questionId)) {
      this.deletingQuestionId = null;
      this.snackBar.open(
        'Unable to delete this question right now.',
        'Close',
        QuestionBankComponent.editErrorSnackBarConfig
      );
      return;
    }

    const removedCurrentPageLastVisibleRow = this.filteredQuestions.length === 1;

    this.questions = this.questions.filter(question => question.questionId !== questionId);
    this.totalCount = Math.max(0, this.totalCount - 1);
    this.buildDynamicOptions(this.questions);
    this.applyClientFilters();
    this.deletingQuestionId = null;
    this.changeDetectorRef.detectChanges();

    this.snackBar.open(
      'Question successfully removed from the question bank.',
      'Close',
      QuestionBankComponent.deleteSuccessSnackBarConfig
    );

    if (this.totalCount > 0 && removedCurrentPageLastVisibleRow) {
      if (this.currentPage > 1) {
        this.currentPage -= 1;
      }

      this.loadQuestions();
    }
  }

  private submitBulkUpload(parsedFile: ParsedQuestionImportFile, input: HTMLInputElement): void {
    const payload = parsedFile.questions as CreateQuestionRequest[];

    if (payload.length === 0) {
      this.uploadErrorMessage = 'The selected file does not contain any question rows to import.';
      this.isUploading = false;
      input.value = '';
      this.changeDetectorRef.detectChanges();
      return;
    }

    this.assessmentAdminService.createQuestions(payload).subscribe({
      next: createdQuestions => {
        const createdCount = createdQuestions.length;
        const duplicateCount = payload.length - createdCount;

        if (createdCount === 0 && duplicateCount > 0) {
          this.uploadMessage = `No new questions were imported from ${parsedFile.fileName}. All ${duplicateCount} row(s) already exist in the question bank.`;
        } else if (duplicateCount > 0) {
          this.uploadMessage = `${createdCount} question(s) were imported from ${parsedFile.fileName}. ${duplicateCount} exact duplicate row(s) were skipped.`;
        } else {
          this.uploadMessage = `${createdCount} question(s) were imported successfully from ${parsedFile.fileName}.`;
        }

        this.uploadErrorMessage = '';
        this.currentPage = 1;
        this.isUploading = false;
        input.value = '';
        this.loadQuestions();
      },
      error: error => {
        this.uploadErrorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          'The upload could not be completed. Please fix the highlighted row and try again.';
        this.isUploading = false;
        input.value = '';
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private loadQuestions(): void {
    if (this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const request: QuestionBankSearchRequest = {
      subject: this.selectedSubject === 'all' ? undefined : this.selectedSubject,
      topic: this.selectedTopic === 'all' ? undefined : this.selectedTopic,
      difficultyLevel: this.selectedDifficulty === 'all' ? undefined : Number(this.selectedDifficulty),
      pageNumber: this.currentPage,
      pageSize: this.pageSize
    };

    this.assessmentAdminService.searchQuestionBank(request).subscribe({
      next: result => {
        this.applyResult(result);
      },
      error: error => {
        this.errorMessage =
          error?.error?.detail ||
          error?.error?.message ||
          'Unable to load the question bank right now.';
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private applyResult(result: PagedQuestionBankResult): void {
    this.questions = result.items ?? [];
    this.totalCount = result.totalCount ?? 0;
    this.currentPage = result.pageNumber > 0 ? result.pageNumber : this.currentPage;

    this.buildDynamicOptions(this.questions);
    this.applyClientFilters();

    this.isLoading = false;
    this.changeDetectorRef.detectChanges();
  }

  private applyClientFilters(): void {
    const normalizedSearch = this.searchTerm.trim().toLowerCase();
    this.filteredQuestions = this.questions.filter(question => {
      const matchesSearch =
        normalizedSearch.length === 0 ||
        question.questionText.toLowerCase().includes(normalizedSearch) ||
        (question.subject ?? '').toLowerCase().includes(normalizedSearch) ||
        (question.topic ?? '').toLowerCase().includes(normalizedSearch);

      const matchesType =
        this.selectedType === 'all' ||
        question.questionType.toLowerCase() === this.selectedType.toLowerCase();

      return matchesSearch && matchesType;
    });

    if (this.totalCount === 0) {
      this.infoMessage = this.hasActiveServerFilters()
        ? 'No questions match the selected filters yet. Try adjusting the filters to broaden the results.'
        : 'No questions have been added to your question bank yet. Once questions are created, they will appear here.';
    } else if (this.filteredQuestions.length === 0) {
      this.infoMessage = 'No questions on this page match the current search or type filter.';
    } else {
      this.infoMessage = '';
    }
  }

  private buildDynamicOptions(questions: QuestionBankItem[]): void {
    this.availableSubjects = this.buildOptions(
      questions.map(question => question.subject),
      'Subject',
      undefined,
      this.selectedSubject
    );

    this.availableTopics = this.buildOptions(
      questions.map(question => question.topic),
      'Topic',
      undefined,
      this.selectedTopic
    );

    this.availableTypes = this.buildOptions(
      questions.map(question => question.questionType),
      'Type',
      value => this.getQuestionTypeLabel(value),
      this.selectedType
    );
  }

  private buildOptions(
    values: Array<string | null | undefined>,
    defaultLabel: string,
    labelResolver?: (value: string) => string,
    selectedValue?: string
  ): SelectOption[] {
    const normalizedValues = values
      .map(value => value?.trim())
      .filter((value): value is string => Boolean(value));

    if (selectedValue && selectedValue !== 'all') {
      normalizedValues.push(selectedValue);
    }

    const uniqueValues = [...new Set(normalizedValues)].sort((left, right) => left.localeCompare(right));

    return [
      { value: 'all', label: defaultLabel },
      ...uniqueValues.map(value => ({
        value,
        label: labelResolver ? labelResolver(value) : value
      }))
    ];
  }

  private hasActiveServerFilters(): boolean {
    return this.selectedSubject !== 'all' ||
      this.selectedTopic !== 'all' ||
      this.selectedDifficulty !== 'all';
  }
}
