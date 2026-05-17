import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppCommonModule } from '../../common/common.module';
import { StudentRoutes } from './student.routes';
import { StudentShellComponent } from './student-shell/student-shell.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { TasksComponent } from './tasks/tasks.component';
import { ManageComponent } from './manage/manage.component';
import { HelpCenterComponent } from './help-center/help-center.component';

@NgModule({
  declarations: [
    StudentShellComponent,
    DashboardComponent,
    CoursesComponent,
    TasksComponent,
    ManageComponent,
    HelpCenterComponent
  ],
  imports: [
    CommonModule,
    AppCommonModule,
    StudentRoutes
  ]
})
export class StudentModule {}
