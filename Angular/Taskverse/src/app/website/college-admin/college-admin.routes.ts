import { RouterModule, Routes } from '@angular/router';
import { RoleType } from '../../common/enums/role-type.enum';
import { canActivateAuth } from '../../common/services/guards/can-activate-auth.guard';
import { canActivateRole } from '../../common/services/guards/can-activate-role.guard';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { TrainersComponent } from './trainers/trainers.component';
import { StudentsComponent } from './students/students.component';
import { ManageComponent } from './manage/manage.component';

const routes: Routes = [
  {
    path: 'college-admin',
    canActivate: [canActivateAuth, canActivateRole],
    data: { role: RoleType.CollegeAdmin },
    children: [
      { path: '',          redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'courses',   component: CoursesComponent   },
      { path: 'trainers',  component: TrainersComponent  },
      { path: 'students',  component: StudentsComponent  },
      { path: 'manage',    component: ManageComponent    }
    ]
  }
];

export const CollegeAdminRoutes = RouterModule.forChild(routes);
