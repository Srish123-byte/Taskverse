import { Injectable, inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Session } from '../session/session.service';
import { RoleType } from '../../enums/role-type.enum';
import { RouteAddress } from '../../constants/routes.constants';

@Injectable({ providedIn: 'root' })
export class CanActivateRoleService {
  constructor(
    private readonly session: Session,
    private readonly router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const expectedRole: RoleType = route.data?.['role'];

    if (!expectedRole) {
      // No role restriction defined on this route — allow through
      return true;
    }

    if (this.session.role === expectedRole) {
      return true;
    }

    // Role mismatch — send to error page
    this.router.navigate([RouteAddress.Error]);
    return false;
  }
}

export const canActivateRole: CanActivateFn =
  (route: ActivatedRouteSnapshot, _state: RouterStateSnapshot) =>
    inject(CanActivateRoleService).canActivate(route);
