import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { TrainersComponent } from './trainers/trainers.component';
import { StudentsComponent } from './students/students.component';
import { ManageComponent } from './manage/manage.component';

const routes: Routes = [
  { path: '',          redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'courses',   component: CoursesComponent   },
  { path: 'trainers',  component: TrainersComponent  },
  { path: 'students',  component: StudentsComponent  },
  { path: 'manage',    component: ManageComponent    }
];

export const CollegeAdminRoutes = RouterModule.forChild(routes);
