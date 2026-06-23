import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import {
  ProctorEventBatchRequest,
  ProctorEventBatchResponse,
  ProctorEventBatchRequestItem,
  StudentAssessmentsService
} from '../api/student-assessments.service';

export type ProctorEventType =
  | 'TAB_SWITCHED'
  | 'FULLSCREEN_EXITED'
  | 'COPY_ATTEMPTED'
  | 'PASTE_ATTEMPTED'
  | 'CUT_ATTEMPTED'
  | 'CONTEXT_MENU_ATTEMPTED'
  | 'BLOCKED_KEYBOARD_SHORTCUT'
  | 'POSSIBLE_DEVTOOLS_OPENED'
  | 'NETWORK_DISCONNECTED';

export interface ProctoringViolationEvent {
  eventType: ProctorEventType;
  severity: 'Warning' | 'High';
  countsTowardViolation: boolean;
  questionId?: string | null;
  metadata?: Record<string, unknown> | null;
}

export interface ProctoringMonitorConfig {
  getCurrentQuestionId: () => string | null;
  onViolation: (event: ProctoringViolationEvent) => void;
}

@Injectable({ providedIn: 'root' })
export class AssessmentProctoringService {
  private readonly cleanupCallbacks: Array<() => void> = [];
  private readonly pendingEvents: ProctorEventBatchRequestItem[] = [];
  private isFlushInProgress = false;

  constructor(
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly studentAssessmentsService: StudentAssessmentsService
  ) {}

  async enterFullscreen(targetElement?: HTMLElement | null): Promise<boolean> {
    if (this.document.fullscreenElement) {
      return true;
    }

    const target = targetElement ?? this.document.documentElement;
    const requestFullscreen = target.requestFullscreen?.bind(target);
    if (!requestFullscreen) {
      return false;
    }

    try {
      await requestFullscreen();
      return !!this.document.fullscreenElement;
    } catch {
      return false;
    }
  }

  async exitFullscreen(): Promise<void> {
    if (!this.document.fullscreenElement || !this.document.exitFullscreen) {
      return;
    }

    try {
      await this.document.exitFullscreen();
    } catch {
      // Ignore failures during teardown.
    }
  }

  startMonitoring(config: ProctoringMonitorConfig): void {
    this.stopMonitoring();

    this.listen(this.document, 'visibilitychange', () => {
      if (this.document.visibilityState === 'hidden') {
        config.onViolation({
          eventType: 'TAB_SWITCHED',
          severity: 'Warning',
          countsTowardViolation: true,
          questionId: config.getCurrentQuestionId(),
          metadata: { source: 'visibilitychange' }
        });
      }
    });

    this.listen(window, 'blur', () => {
      config.onViolation({
        eventType: 'TAB_SWITCHED',
        severity: 'Warning',
        countsTowardViolation: true,
        questionId: config.getCurrentQuestionId(),
        metadata: { source: 'window.blur' }
      });
    });

    this.listen(this.document, 'fullscreenchange', () => {
      if (!this.document.fullscreenElement) {
        config.onViolation({
          eventType: 'FULLSCREEN_EXITED',
          severity: 'High',
          countsTowardViolation: true,
          questionId: config.getCurrentQuestionId(),
          metadata: { source: 'fullscreenchange' }
        });
      }
    });

    this.listen(window, 'offline', () => {
      config.onViolation({
        eventType: 'NETWORK_DISCONNECTED',
        severity: 'High',
        countsTowardViolation: true,
        questionId: config.getCurrentQuestionId(),
        metadata: { source: 'offline' }
      });
    });

    this.listen(this.document, 'copy', event => {
      event.preventDefault();
      config.onViolation({
        eventType: 'COPY_ATTEMPTED',
        severity: 'Warning',
        countsTowardViolation: true,
        questionId: config.getCurrentQuestionId()
      });
    });

    this.listen(this.document, 'paste', event => {
      event.preventDefault();
      config.onViolation({
        eventType: 'PASTE_ATTEMPTED',
        severity: 'Warning',
        countsTowardViolation: true,
        questionId: config.getCurrentQuestionId()
      });
    });

    this.listen(this.document, 'cut', event => {
      event.preventDefault();
      config.onViolation({
        eventType: 'CUT_ATTEMPTED',
        severity: 'Warning',
        countsTowardViolation: true,
        questionId: config.getCurrentQuestionId()
      });
    });

    this.listen(this.document, 'contextmenu', event => {
      event.preventDefault();
      config.onViolation({
        eventType: 'CONTEXT_MENU_ATTEMPTED',
        severity: 'Warning',
        countsTowardViolation: true,
        questionId: config.getCurrentQuestionId()
      });
    });

    this.listen(this.document, 'keydown', event => {
      const normalizedKey = event.key.toLowerCase();
      const attemptsDevtools =
        normalizedKey === 'f12' ||
        (event.ctrlKey && event.shiftKey && ['i', 'j', 'c'].includes(normalizedKey));
      const blockedShortcut =
        (event.ctrlKey && ['u', 's', 'p', 'r'].includes(normalizedKey)) ||
        (event.altKey && normalizedKey === 'arrowleft') ||
        normalizedKey === 'f5';

      if (!attemptsDevtools && !blockedShortcut) {
        return;
      }

      event.preventDefault();
      config.onViolation({
        eventType: attemptsDevtools ? 'POSSIBLE_DEVTOOLS_OPENED' : 'BLOCKED_KEYBOARD_SHORTCUT',
        severity: attemptsDevtools ? 'High' : 'Warning',
        countsTowardViolation: true,
        questionId: config.getCurrentQuestionId(),
        metadata: {
          key: event.key,
          ctrlKey: event.ctrlKey,
          shiftKey: event.shiftKey,
          altKey: event.altKey,
          metaKey: event.metaKey
        }
      });
    });
  }

  stopMonitoring(): void {
    while (this.cleanupCallbacks.length > 0) {
      const cleanup = this.cleanupCallbacks.pop();
      cleanup?.();
    }
  }

  queueEvent(
    attemptId: string,
    event: ProctoringViolationEvent,
    clientTimestamp = new Date().toISOString()
  ): void {
    this.pendingEvents.push({
      attemptId,
      eventType: event.eventType,
      severity: event.severity,
      clientTimestamp,
      questionId: event.questionId ?? null,
      metadata: event.metadata ?? null
    });
  }

  flushQueuedEvents(sessionId: string): Observable<ProctorEventBatchResponse | null> {
    if (!sessionId || this.isFlushInProgress || this.pendingEvents.length === 0) {
      return of(null);
    }

    const snapshot = [...this.pendingEvents];
    const request: ProctorEventBatchRequest = { events: snapshot };
    this.isFlushInProgress = true;

    return new Observable<ProctorEventBatchResponse>(subscriber => {
      const subscription = this.studentAssessmentsService.recordProctorEvents(sessionId, request).subscribe({
        next: response => {
          this.pendingEvents.splice(0, snapshot.length);
          this.isFlushInProgress = false;
          subscriber.next(response);
          subscriber.complete();
        },
        error: error => {
          this.isFlushInProgress = false;
          subscriber.error(error);
        }
      });

      return () => subscription.unsubscribe();
    });
  }

  clearQueuedEvents(): void {
    this.pendingEvents.splice(0, this.pendingEvents.length);
  }

  private listen<K extends keyof DocumentEventMap>(
    target: Document,
    eventName: K,
    listener: (event: DocumentEventMap[K]) => void
  ): void;
  private listen<K extends keyof WindowEventMap>(
    target: Window,
    eventName: K,
    listener: (event: WindowEventMap[K]) => void
  ): void;
  private listen(
    target: Document | Window,
    eventName: string,
    listener: EventListenerOrEventListenerObject
  ): void {
    target.addEventListener(eventName, listener);
    this.cleanupCallbacks.push(() => target.removeEventListener(eventName, listener));
  }
}
