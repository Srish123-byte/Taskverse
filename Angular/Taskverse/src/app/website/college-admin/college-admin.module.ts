import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppCommonModule } from '../../common/common.module';
import { CollegeAdminRoutes } from './college-admin.routes';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { TrainersComponent } from './trainers/trainers.component';
import { StudentsComponent } from './students/students.component';
import { ManageComponent } from './manage/manage.component';

@NgModule({
  declarations: [
    DashboardComponent,
    CoursesComponent,
    TrainersComponent,
    StudentsComponent,
    ManageComponent
  ],
  imports: [
    CommonModule,
    AppCommonModule,
    CollegeAdminRoutes
  ]
})
export class CollegeAdminModule {}
