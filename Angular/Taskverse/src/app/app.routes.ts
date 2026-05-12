import { RouterModule, Routes } from '@angular/router';
import { PageNotFoundComponent } from './common/components/page-not-found/page-not-found.component';
import { RouteAddress } from './common/constants/routes.constants';
import { RoleType } from './common/enums/role-type.enum';
import { canActivateAuth } from './common/services/guards/can-activate-auth.guard';
import { canActivateRole } from './common/services/guards/can-activate-role.guard';

const routes: Routes = [
  // Default root redirect — send unauthenticated users to login
  {
    path: '',
    redirectTo: RouteAddress.Login,
    pathMatch: 'full'
  },
  // Lazy-loaded role modules
  {
    path: 'super-admin',
    canActivate: [canActivateAuth, canActivateRole],
    data: { role: RoleType.SuperAdmin },
    loadChildren: () =>
      import('./website/super-admin/super-admin.module').then(m => m.SuperAdminModule)
  },
  {
    path: 'college-admin',
    canActivate: [canActivateAuth, canActivateRole],
    data: { role: RoleType.CollegeAdmin },
    loadChildren: () =>
      import('./website/college-admin/college-admin.module').then(m => m.CollegeAdminModule)
  },
  {
    path: 'trainer',
    canActivate: [canActivateAuth, canActivateRole],
    data: { role: RoleType.Trainer },
    loadChildren: () =>
      import('./website/trainer/trainer.module').then(m => m.TrainerModule)
  },
  {
    path: 'student',
    canActivate: [canActivateAuth, canActivateRole],
    data: { role: RoleType.Student },
    loadChildren: () =>
      import('./website/student/student.module').then(m => m.StudentModule)
  },
  // Catch-all 404
  {
    path: '**',
    component: PageNotFoundComponent
  }
];

export const AppRoutes = RouterModule.forRoot(routes, {});
