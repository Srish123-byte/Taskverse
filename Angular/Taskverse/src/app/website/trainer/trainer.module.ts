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
import { AssessmentsManagementComponent } from './assessments-management/assessments-management.component';
import { NewAssessmentComponent } from './new-assessment/new-assessment.component';
import { ReportsComponent } from './reports/reports.component';
import { FormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    TrainerShellComponent,
    DashboardComponent,
    CoursesComponent,
    StudentsComponent,
    QuestionsManagementComponent,
    QuestionEditorPageComponent,
    AssessmentsManagementComponent,
    NewAssessmentComponent,
    ManageComponent,
    HelpCenterComponent,
    ReportsComponent
  ],
  imports: [
    CommonModule,
    AppCommonModule,
    TrainerRoutes,
    FormsModule
  ]
})
export class TrainerModule {}
