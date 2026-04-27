import { RouterModule, Routes } from '@angular/router';
import { RouteAddress } from '../../common/constants/routes.constants';
import { LoginComponent } from './login/login.component';

const routes: Routes = [
  {
    path: RouteAddress.Login,
    component: LoginComponent
  }
];

export const LoginRoutes = RouterModule.forChild(routes);
