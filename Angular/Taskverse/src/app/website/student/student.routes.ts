import { RouterModule, Routes } from '@angular/router';
import { StudentShellComponent } from './student-shell/student-shell.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { TasksComponent } from './tasks/tasks.component';
import { ManageComponent } from './manage/manage.component';
import { HelpCenterComponent } from './help-center/help-center.component';

const routes: Routes = [
  {
    path: '',
    component: StudentShellComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'courses', component: CoursesComponent },
      { path: 'tasks', component: TasksComponent },
      { path: 'manage', component: ManageComponent },
      { path: 'help-center', component: HelpCenterComponent }
    ]
  }
];

export const StudentRoutes = RouterModule.forChild(routes);
