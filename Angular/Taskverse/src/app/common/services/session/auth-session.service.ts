import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { EMPTY, Observable, of } from 'rxjs';
import { catchError, finalize, map, shareReplay, take } from 'rxjs/operators';
import { RouteAddress } from '../../constants/routes.constants';
import { AccountService } from '../api/account.service';
import { Session } from './session.service';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private isLoggingOut = false;
  private refreshRequest$: Observable<boolean> | null = null;

  constructor(
    private readonly accountService: AccountService,
    private readonly session: Session,
    private readonly router: Router
  ) {}

  logout(reason: 'manual' | 'timeout' | 'unauthorized' = 'manual'): void {
    if (this.isLoggingOut) {
      return;
    }

    this.isLoggingOut = true;
    const userId = this.session.userId;
    const refreshToken = this.session.refreshToken;
    const jwtToken = this.session.jwtToken;

    if (!jwtToken || !userId || !refreshToken) {
      this.finishLogout(reason);
      return;
    }

    this.accountService
      .logout({ userId, refreshToken })
      .pipe(
        take(1),
        catchError(() => EMPTY),
        finalize(() => this.finishLogout(reason))
      )
      .subscribe();
  }

  refreshSession(forceRotate = false): Observable<boolean> {
    if (this.refreshRequest$) {
      return this.refreshRequest$;
    }

    const refreshToken = this.session.refreshToken;
    if (!refreshToken) {
      this.logout('unauthorized');
      return of(false);
    }

    this.refreshRequest$ = this.accountService.refreshToken({
      refreshToken,
      accessToken: this.session.jwtToken || undefined,
      forceRotate
    }).pipe(
      take(1),
      map(response => {
        this.session.jwtToken = response.token;
        this.session.refreshToken = response.refreshToken;
        this.session.accessTokenExpiresAt = response.expiresAt;
        return true;
      }),
      catchError(() => {
        this.logout('timeout');
        return of(false);
      }),
      finalize(() => {
        this.refreshRequest$ = null;
      }),
      shareReplay(1)
    );

    return this.refreshRequest$;
  }

  ensureActiveAccessToken(forceRotate = false): Observable<boolean> {
    if (!this.session.isLoggedIn()) {
      return of(false);
    }

    if (forceRotate || this.shouldRefreshSoon()) {
      return this.refreshSession(forceRotate);
    }

    return of(true);
  }

  private shouldRefreshSoon(): boolean {
    const expiresAt = this.session.accessTokenExpiresAt;
    if (!expiresAt) {
      return true;
    }

    const expiryTime = new Date(expiresAt).getTime();
    if (Number.isNaN(expiryTime)) {
      return true;
    }

    return expiryTime - Date.now() <= 3 * 60 * 1000;
  }

  private finishLogout(reason: 'manual' | 'timeout' | 'unauthorized'): void {
    this.isLoggingOut = false;
    this.refreshRequest$ = null;
    this.session.clear();
    const targetRoute = reason === 'manual'
      ? `/${RouteAddress.Login}`
      : `/${RouteAddress.SessionTimeout}`;
    void this.router.navigateByUrl(targetRoute);
  }
}
