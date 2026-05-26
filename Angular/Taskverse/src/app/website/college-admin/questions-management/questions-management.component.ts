import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import {
  AssessmentAdminService,
  PagedQuestionBankResult,
  QuestionBankItem,
  QuestionBankSearchRequest
} from '../../../common/services/api/assessment-admin.service';

interface SelectOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-college-admin-questions-management',
  standalone: false,
  templateUrl: './questions-management.component.html',
  styleUrl: './questions-management.component.scss'
})
export class QuestionsManagementComponent implements OnInit {
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

  isLoading = false;
  errorMessage = '';
  infoMessage = '';

  constructor(
    private readonly assessmentAdminService: AssessmentAdminService,
    private readonly changeDetectorRef: ChangeDetectorRef
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

  private loadQuestions(): void {
    if (this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.infoMessage = '';

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
