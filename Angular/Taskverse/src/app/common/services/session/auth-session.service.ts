import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { EMPTY } from 'rxjs';
import { catchError, finalize, take } from 'rxjs/operators';
import { RouteAddress } from '../../constants/routes.constants';
import { AccountService } from '../api/account.service';
import { Session } from './session.service';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  constructor(
    private readonly accountService: AccountService,
    private readonly session: Session,
    private readonly router: Router
  ) {}

  logout(): void {
    const userId = this.session.userId;
    const refreshToken = this.session.refreshToken;
    const jwtToken = this.session.jwtToken;

    if (!jwtToken || !userId || !refreshToken) {
      this.finishLogout();
      return;
    }

    this.accountService
      .logout({ userId, refreshToken })
      .pipe(
        take(1),
        catchError(() => EMPTY),
        finalize(() => this.finishLogout())
      )
      .subscribe();
  }

  private finishLogout(): void {
    this.session.clear();
    void this.router.navigateByUrl(`/${RouteAddress.Login}`);
  }
}
