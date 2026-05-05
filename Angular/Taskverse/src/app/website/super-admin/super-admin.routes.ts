import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CollegesComponent } from './colleges/colleges.component';
import { TrainersComponent } from './trainers/trainers.component';
import { UsersComponent } from './users/users.component';
import { ManageComponent } from './manage/manage.component';

const routes: Routes = [
  { path: '',          redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'colleges',  component: CollegesComponent  },
  { path: 'trainers',  component: TrainersComponent  },
  { path: 'users',     component: UsersComponent     },
  { path: 'manage',    component: ManageComponent    }
];

export const SuperAdminRoutes = RouterModule.forChild(routes);
