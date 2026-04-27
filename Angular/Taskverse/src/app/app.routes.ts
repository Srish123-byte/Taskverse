import { RouterModule, Routes } from '@angular/router';
import { PageNotFoundComponent } from './common/components/page-not-found/page-not-found.component';
import { RouteAddress } from './common/constants/routes.constants';

const routes: Routes = [
  // Default root redirect — send unauthenticated users to login
  {
    path: '',
    redirectTo: RouteAddress.Login,
    pathMatch: 'full'
  },
  // Catch-all 404
  {
    path: '**',
    component: PageNotFoundComponent
  }
];

export const AppRoutes = RouterModule.forRoot(routes, {});
