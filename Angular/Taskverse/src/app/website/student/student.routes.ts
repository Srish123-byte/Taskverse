import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { TasksComponent } from './tasks/tasks.component';
import { ManageComponent } from './manage/manage.component';

const routes: Routes = [
  { path: '',          redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'courses',   component: CoursesComponent   },
  { path: 'tasks',     component: TasksComponent     },
  { path: 'manage',    component: ManageComponent    }
];

export const StudentRoutes = RouterModule.forChild(routes);
