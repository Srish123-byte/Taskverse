import { RouterModule, Routes } from '@angular/router';
import { RoleType } from '../../common/enums/role-type.enum';
import { canActivateAuth } from '../../common/services/guards/can-activate-auth.guard';
import { canActivateRole } from '../../common/services/guards/can-activate-role.guard';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { StudentsComponent } from './students/students.component';
import { ManageComponent } from './manage/manage.component';

const routes: Routes = [
  {
    path: 'trainer',
    canActivate: [canActivateAuth, canActivateRole],
    data: { role: RoleType.Trainer },
    children: [
      { path: '',          redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'courses',   component: CoursesComponent   },
      { path: 'students',  component: StudentsComponent  },
      { path: 'manage',    component: ManageComponent    }
    ]
  }
];

export const TrainerRoutes = RouterModule.forChild(routes);
