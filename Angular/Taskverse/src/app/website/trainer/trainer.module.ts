import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppCommonModule } from '../../common/common.module';
import { TrainerRoutes } from './trainer.routes';
import { TrainerShellComponent } from './trainer-shell/trainer-shell.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { StudentsComponent } from './students/students.component';
import { ManageComponent } from './manage/manage.component';
import { HelpCenterComponent } from './help-center/help-center.component';
import { QuestionsManagementComponent } from './questions-management/questions-management.component';
import { QuestionEditorPageComponent } from './question-editor/question-editor.component';

@NgModule({
  declarations: [
    TrainerShellComponent,
    DashboardComponent,
    CoursesComponent,
    StudentsComponent,
    QuestionsManagementComponent,
    QuestionEditorPageComponent,
    ManageComponent,
    HelpCenterComponent
  ],
  imports: [
    CommonModule,
    AppCommonModule,
    TrainerRoutes
  ]
})
export class TrainerModule {}
