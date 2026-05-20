import { RouterModule, Routes } from '@angular/router';
import { TrainerShellComponent } from './trainer-shell/trainer-shell.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { StudentsComponent } from './students/students.component';
import { ManageComponent } from './manage/manage.component';
import { HelpCenterComponent } from './help-center/help-center.component';

const routes: Routes = [
  {
    path: '',
    component: TrainerShellComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'courses', component: CoursesComponent },
      { path: 'students', component: StudentsComponent },
      { path: 'manage', component: ManageComponent },
      { path: 'help-center', component: HelpCenterComponent }
    ]
  }
];

export const TrainerRoutes = RouterModule.forChild(routes);
