import { RouterModule, Routes } from '@angular/router';
import { RoleType } from '../../common/enums/role-type.enum';
import { canActivateAuth } from '../../common/services/guards/can-activate-auth.guard';
import { canActivateRole } from '../../common/services/guards/can-activate-role.guard';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CollegesComponent } from './colleges/colleges.component';
import { TrainersComponent } from './trainers/trainers.component';
import { UsersComponent } from './users/users.component';
import { ManageComponent } from './manage/manage.component';

const routes: Routes = [
  {
    path: 'super-admin',
    canActivate: [canActivateAuth, canActivateRole],
    data: { role: RoleType.SuperAdmin },
    children: [
      { path: '',          redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'colleges',  component: CollegesComponent  },
      { path: 'trainers',  component: TrainersComponent  },
      { path: 'users',     component: UsersComponent     },
      { path: 'manage',    component: ManageComponent    }
    ]
  }
];

export const SuperAdminRoutes = RouterModule.forChild(routes);
