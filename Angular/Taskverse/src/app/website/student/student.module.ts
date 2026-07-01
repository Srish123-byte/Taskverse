import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppCommonModule } from '../../common/common.module';
import { StudentRoutes } from './student.routes';
import { StudentShellComponent } from './student-shell/student-shell.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { MyAssessmentsComponent } from './my-assessments/my-assessments.component';
import { AssessmentRunnerComponent } from './assessment-runner/assessment-runner.component';
import { ResultsComponent } from './results/results.component';
import { HelpCenterComponent } from './help-center/help-center.component';
import { ReportsComponent } from './reports/reports.component';

@NgModule({
  declarations: [
    StudentShellComponent,
    DashboardComponent,
    MyAssessmentsComponent,
    AssessmentRunnerComponent,
    ResultsComponent,
    HelpCenterComponent,
    ReportsComponent
  ],
  imports: [
    CommonModule,
    AppCommonModule,
    StudentRoutes
  ]
})
export class StudentModule {}
