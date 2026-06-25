import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges
} from '@angular/core';
import type * as monaco from 'monaco-editor';
import { Subscription, timer } from 'rxjs';
import {
  CodingExecutionMode,
  CodingExecutionResponse,
  StudentAssessmentsService
} from '../../services/api/student-assessments.service';

export interface CodingLanguageOption {
  code: string;
  label: string;
}

const CODING_LANGUAGE_OPTIONS: CodingLanguageOption[] = [
  { code: 'c', label: 'C' },
  { code: 'cpp', label: 'C++' },
  { code: 'python', label: 'Python' },
  { code: 'java', label: 'Java' }
];

const TERMINAL_STATUSES = new Set(['completed', 'failed', 'cancelled', 'timeout', 'error']);
const FAILURE_STATUSES = new Set(['failed', 'cancelled', 'timeout', 'error']);
const POLL_INTERVAL_MS = 2000;
const MAX_POLL_ATTEMPTS = 90;

@Component({
  selector: 'app-coding',
  standalone: false,
  templateUrl: './coding.component.html',
  styleUrl: './coding.component.scss'
})
export class CodingComponent implements OnChanges, OnDestroy {
  @Input() title = 'Code Editor';
  @Input() description = 'Use the shared coding workspace to draft, review, and refine code.';
  @Input() helperText = 'Choose a language and keep your work in sync through a single reusable editor.';
  @Input() language = 'python';
  @Input() code = '';
  @Input() theme: 'vs' | 'vs-dark' | 'hc-black' = 'vs';
  @Input() readOnly = false;
  @Input() height = '420px';
  @Input() editorOptions: monaco.editor.IStandaloneEditorConstructionOptions = {};
  @Input() attemptId: string | null = null;
  @Input() questionId: string | null = null;
  @Input() isSubmitted = false;

  @Output() codeChange = new EventEmitter<string>();
  @Output() languageChange = new EventEmitter<string>();
  @Output() submitted = new EventEmitter<void>();

  readonly languageOptions = CODING_LANGUAGE_OPTIONS;

  isRunning = false;
  isSubmitting = false;
  executionStatus: string | null = null;
  stdout: string | null = null;
  stderr: string | null = null;
  executionError: string | null = null;

  private pollSubscription: Subscription | null = null;

  constructor(private readonly studentAssessmentsService: StudentAssessmentsService) {}

  get canExecute(): boolean {
    return !!this.attemptId && !!this.questionId;
  }

  get isBusy(): boolean {
    return this.isRunning || this.isSubmitting;
  }

  get selectedLanguageLabel(): string {
    return this.languageOptions.find(option => option.code === this.language)?.label ?? this.language;
  }

  get isFailureStatus(): boolean {
    return FAILURE_STATUSES.has((this.executionStatus ?? '').trim().toLowerCase());
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['questionId'] && !changes['questionId'].firstChange) {
      this.resetExecutionState();
    }
  }

  ngOnDestroy(): void {
    this.cancelPolling();
  }

  onCodeChange(value: string): void {
    this.code = value;
    this.codeChange.emit(value);
  }

  onLanguageSelect(value: string): void {
    if (this.isSubmitted || this.isBusy || value === this.language) {
      return;
    }

    this.language = value;
    this.languageChange.emit(value);
  }

  runCode(): void {
    if (!this.canExecute || this.isSubmitted || this.isBusy) {
      return;
    }

    this.isRunning = true;
    this.executeAndTrack('run');
  }

  submitCode(): void {
    if (!this.canExecute || this.isSubmitted || this.isBusy) {
      return;
    }

    this.isSubmitting = true;
    this.submitted.emit();
    this.executeAndTrack('submit');
  }

  private executeAndTrack(mode: CodingExecutionMode): void {
    this.executionError = null;
    this.executionStatus = null;
    this.stdout = null;
    this.stderr = null;

    this.studentAssessmentsService
      .executeCode(this.attemptId!, this.questionId!, { language: this.language, code: this.code, mode })
      .subscribe({
        next: response => this.handleExecutionResponse(response, mode),
        error: error => this.handleExecutionError(error, mode)
      });
  }

  private handleExecutionResponse(response: CodingExecutionResponse, mode: CodingExecutionMode): void {
    this.applyExecutionResponse(response);

    if (this.isTerminalStatus(response.status)) {
      this.finishExecution(mode);
      return;
    }

    this.pollExecution(response.id, mode, 0);
  }

  private pollExecution(executionId: string, mode: CodingExecutionMode, attempt: number): void {
    this.cancelPolling();

    if (attempt >= MAX_POLL_ATTEMPTS) {
      this.executionError = 'Execution is taking longer than expected. Please try again.';
      this.finishExecution(mode);
      return;
    }

    this.pollSubscription = timer(POLL_INTERVAL_MS).subscribe(() => {
      this.studentAssessmentsService
        .getCodeExecution(this.attemptId!, this.questionId!, executionId)
        .subscribe({
          next: response => {
            this.applyExecutionResponse(response);

            if (this.isTerminalStatus(response.status)) {
              this.finishExecution(mode);
              return;
            }

            this.pollExecution(executionId, mode, attempt + 1);
          },
          error: error => this.handleExecutionError(error, mode)
        });
    });
  }

  private applyExecutionResponse(response: CodingExecutionResponse): void {
    this.executionStatus = response.status;
    this.stdout = response.stdout ?? null;
    this.stderr = response.stderr ?? null;
  }

  private handleExecutionError(error: unknown, mode: CodingExecutionMode): void {
    console.error(`Failed to ${mode} code.`, error);
    this.executionError = (error as { error?: { message?: string } })?.error?.message
      || `Unable to ${mode} this code right now.`;
    this.finishExecution(mode);
  }

  private finishExecution(mode: CodingExecutionMode): void {
    this.cancelPolling();

    if (mode === 'run') {
      this.isRunning = false;
    } else {
      this.isSubmitting = false;
    }
  }

  private isTerminalStatus(status: string): boolean {
    return TERMINAL_STATUSES.has((status ?? '').trim().toLowerCase());
  }

  private cancelPolling(): void {
    this.pollSubscription?.unsubscribe();
    this.pollSubscription = null;
  }

  private resetExecutionState(): void {
    this.cancelPolling();
    this.isRunning = false;
    this.isSubmitting = false;
    this.executionStatus = null;
    this.stdout = null;
    this.stderr = null;
    this.executionError = null;
  }
}
