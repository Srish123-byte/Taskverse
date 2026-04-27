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

  canActivate(): boolean {
    if (this.session.isLoggedIn()) {
      return true;
    }
    this.router.navigate([RouteAddress.Login]);
    return false;
  }
}

export const canActivateAuth: CanActivateFn =
  (_route: ActivatedRouteSnapshot, _state: RouterStateSnapshot) =>
    inject(CanActivateAuthService).canActivate();
