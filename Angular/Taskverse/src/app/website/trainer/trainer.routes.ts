import { RouterModule, Routes } from '@angular/router';
import { TrainerShellComponent } from './trainer-shell/trainer-shell.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CoursesComponent } from './courses/courses.component';
import { StudentsComponent } from './students/students.component';
import { ManageComponent } from './manage/manage.component';
import { HelpCenterComponent } from './help-center/help-center.component';
import { ReportsComponent } from './reports/reports.component';
import { QuestionsManagementComponent } from './questions-management/questions-management.component';
import { QuestionEditorPageComponent } from './question-editor/question-editor.component';
import { AssessmentsManagementComponent } from './assessments-management/assessments-management.component';
import { NewAssessmentComponent } from './new-assessment/new-assessment.component';

const routes: Routes = [
  {
    path: '',
    component: TrainerShellComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'courses', component: CoursesComponent },
      { path: 'students', component: StudentsComponent },
      { path: 'questions-management', component: QuestionsManagementComponent },
      { path: 'questions-management/new', component: QuestionEditorPageComponent },
      { path: 'questions-management/edit/:id', component: QuestionEditorPageComponent },
      { path: 'assessments-management', component: AssessmentsManagementComponent },
      { path: 'assessments-management/new-assessment', component: NewAssessmentComponent },
      { path: 'assessments-management/edit-assessment/:id', component: NewAssessmentComponent },
      { path: 'reports', component: ReportsComponent },
      { path: 'manage', component: ManageComponent },
      { path: 'help-center', component: HelpCenterComponent }
    ]
  }
];

export const TrainerRoutes = RouterModule.forChild(routes);
