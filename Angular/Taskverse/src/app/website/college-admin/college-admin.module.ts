import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppCommonModule } from '../../common/common.module';
import { CollegeAdminRoutes } from './college-admin.routes';
import { CollegeAdminShellComponent } from './college-admin-shell/college-admin-shell.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { UserApprovalsComponent } from './user-approvals/user-approvals.component';
import { UserManagementComponent } from './user-management/user-management.component';
import { AcademicStructureComponent } from './academic-structure/academic-structure.component';
import { AssessmentBuilderComponent } from './assessment-builder/assessment-builder.component';
import { QuestionsManagementComponent } from './questions-management/questions-management.component';
import { ReportsComponent } from './reports/reports.component';
import { SettingsComponent } from './settings/settings.component';
import { HelpCenterComponent } from './help-center/help-center.component';
import { QuestionEditorPageComponent } from './question-editor/question-editor.component';

@NgModule({
  declarations: [
    CollegeAdminShellComponent,
    DashboardComponent,
    UserApprovalsComponent,
    UserManagementComponent,
    AcademicStructureComponent,
    AssessmentBuilderComponent,
    QuestionsManagementComponent,
    QuestionEditorPageComponent,
    ReportsComponent,
    SettingsComponent,
    HelpCenterComponent
  ],
  imports: [
    CommonModule,
    AppCommonModule,
    CollegeAdminRoutes
  ]
})
export class CollegeAdminModule {}
