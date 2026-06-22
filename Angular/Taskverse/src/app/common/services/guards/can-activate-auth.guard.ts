import { Injectable, inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Session } from '../session/session.service';
import { RouteAddress } from '../../constants/routes.constants';

@Injectable({ providedIn: 'root' })
export class CanActivateAuthService {
  constructor(
    private readonly session: Session,
    private readonly router: Router
  ) {}

  canActivate(state: RouterStateSnapshot): boolean {
    if (this.session.isLoggedIn()) {
      if (this.session.mustChangePassword &&
          !state.url.includes(`/${RouteAddress.ChangeTemporaryPassword}`) &&
          !state.url.includes(`/${RouteAddress.Logout}`)) {
        void this.router.navigateByUrl(`/${RouteAddress.ChangeTemporaryPassword}`);
        return false;
      }

      return true;
    }
    void this.router.navigateByUrl(`/${RouteAddress.Login}`);
    return false;
  }
}

export const canActivateAuth: CanActivateFn =
  (_route: ActivatedRouteSnapshot, state: RouterStateSnapshot) =>
    inject(CanActivateAuthService).canActivate(state);
