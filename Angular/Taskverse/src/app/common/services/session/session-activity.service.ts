import { DOCUMENT } from '@angular/common';
import { Inject, Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Subscription, fromEvent, merge } from 'rxjs';
import { throttleTime } from 'rxjs/operators';
import { AuthSessionService } from './auth-session.service';
import { Session } from './session.service';

@Injectable({ providedIn: 'root' })
export class SessionActivityService implements OnDestroy {
  private static readonly warningThresholdMs = 25 * 60 * 1000;
  private static readonly inactivityTimeoutMs = 30 * 60 * 1000;
  private initialized = false;
  private activitySubscription?: Subscription;
  private warningTimerId: ReturnType<typeof setTimeout> | null = null;
  private logoutTimerId: ReturnType<typeof setTimeout> | null = null;

  readonly isWarningVisible$ = new BehaviorSubject<boolean>(false);

  constructor(
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly session: Session,
    private readonly authSessionService: AuthSessionService
  ) {}

  init(): void {
    if (this.initialized) {
      this.syncTimers();
      return;
    }

    this.initialized = true;
    this.activitySubscription = merge(
      fromEvent(this.document, 'click'),
      fromEvent(this.document, 'keydown'),
      fromEvent(this.document, 'scroll'),
      fromEvent(this.document, 'touchstart')
    )
      .pipe(throttleTime(1000, undefined, { leading: true, trailing: false }))
      .subscribe(() => {
        this.registerActivity();
      });

    this.syncTimers();
  }

  ngOnDestroy(): void {
    this.activitySubscription?.unsubscribe();
    this.clearTimers();
  }

  registerActivity(forceRotate = false): void {
    if (!this.session.isLoggedIn()) {
      this.isWarningVisible$.next(false);
      this.clearTimers();
      return;
    }

    this.session.lastActivityAt = new Date().toISOString();
    this.isWarningVisible$.next(false);
    this.syncTimers();
    this.authSessionService.ensureActiveAccessToken(forceRotate).subscribe();
  }

  continueSession(): void {
    this.registerActivity(true);
  }

  logoutNow(): void {
    this.authSessionService.logout('manual');
  }

  syncTimers(): void {
    this.clearTimers();

    if (!this.session.isLoggedIn()) {
      this.isWarningVisible$.next(false);
      return;
    }

    const lastActivity = this.resolveLastActivityTime();
    const now = Date.now();
    const warningDelay = Math.max(0, lastActivity + SessionActivityService.warningThresholdMs - now);
    const logoutDelay = Math.max(0, lastActivity + SessionActivityService.inactivityTimeoutMs - now);

    this.warningTimerId = setTimeout(() => {
      if (this.session.isLoggedIn()) {
        this.isWarningVisible$.next(true);
      }
    }, warningDelay);

    this.logoutTimerId = setTimeout(() => {
      if (this.session.isLoggedIn()) {
        this.isWarningVisible$.next(false);
        this.authSessionService.logout('timeout');
      }
    }, logoutDelay);
  }

  private resolveLastActivityTime(): number {
    const storedLastActivity = this.session.lastActivityAt;
    const parsed = storedLastActivity ? new Date(storedLastActivity).getTime() : Number.NaN;
    if (!Number.isNaN(parsed)) {
      return parsed;
    }

    const fallback = Date.now();
    this.session.lastActivityAt = new Date(fallback).toISOString();
    return fallback;
  }

  private clearTimers(): void {
    if (this.warningTimerId) {
      clearTimeout(this.warningTimerId);
      this.warningTimerId = null;
    }

    if (this.logoutTimerId) {
      clearTimeout(this.logoutTimerId);
      this.logoutTimerId = null;
    }
  }
}
