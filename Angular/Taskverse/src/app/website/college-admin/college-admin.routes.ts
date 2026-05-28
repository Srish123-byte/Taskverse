import { RouterModule, Routes } from '@angular/router';
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

const routes: Routes = [
  {
    path: '',
    component: CollegeAdminShellComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'approvals', component: UserApprovalsComponent },
      { path: 'users', component: UserManagementComponent },
      { path: 'classes-management', component: AcademicStructureComponent },
      { path: 'questions-management', component: QuestionsManagementComponent },
      { path: 'questions-management/new', component: QuestionEditorPageComponent },
      { path: 'questions-management/edit/:id', component: QuestionEditorPageComponent },
      { path: 'assessments-management', component: AssessmentBuilderComponent },
      { path: 'reports', component: ReportsComponent },
      { path: 'help-center', component: HelpCenterComponent },
      { path: 'settings', component: SettingsComponent }
    ]
  }
];

export const CollegeAdminRoutes = RouterModule.forChild(routes);
