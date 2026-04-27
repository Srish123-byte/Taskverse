import { RouterModule, Routes } from '@angular/router';
import { RoleType } from '../../common/enums/role-type.enum';
import { canActivateAuth } from '../../common/services/guards/can-activate-auth.guard';
import { canActivateRole } from '../../common/services/guards/can-activate-role.guard';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { TasksComponent } from './tasks/tasks.component';
import { ManageComponent } from './manage/manage.component';

const routes: Routes = [
  {
    path: 'student',
    canActivate: [canActivateAuth, canActivateRole],
    data: { role: RoleType.Student },
    children: [
      { path: '',          redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'courses',   component: CoursesComponent   },
      { path: 'tasks',     component: TasksComponent     },
      { path: 'manage',    component: ManageComponent    }
    ]
  }
];

export const StudentRoutes = RouterModule.forChild(routes);
