import { RouterModule, Routes } from '@angular/router';
import { RouteAddress } from '../../common/constants/routes.constants';
import { RoleDirectorComponent } from './role-director/role-director.component';
import { UnhandledErrorComponent } from './unhandled-error/unhandled-error.component';
import { SessionTimeoutComponent } from './session-timeout/session-timeout.component';
import { canActivateAuth } from '../../common/services/guards/can-activate-auth.guard';

const routes: Routes = [
  {
    path: RouteAddress.RoleDirector,
    component: RoleDirectorComponent,
    canActivate: [canActivateAuth]
  },
  {
    path: RouteAddress.Error,
    component: UnhandledErrorComponent
  },
  {
    path: 'session-timeout',
    component: SessionTimeoutComponent
  }
];

export const SharedPagesRoutes = RouterModule.forChild(routes);
