import { RouterModule, Routes } from '@angular/router';
import { StudentShellComponent } from './student-shell/student-shell.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { MyAssessmentsComponent } from './my-assessments/my-assessments.component';
import { AssessmentRunnerComponent } from './assessment-runner/assessment-runner.component';
import { ResultsComponent } from './results/results.component';
import { HelpCenterComponent } from './help-center/help-center.component';

const routes: Routes = [
  {
    path: '',
    component: StudentShellComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'my-assessments', component: MyAssessmentsComponent },
      { path: 'my-assessments/attempts/:attemptId/run', component: AssessmentRunnerComponent },
      { path: 'results/attempts/:attemptId', component: ResultsComponent },
      { path: 'results', component: ResultsComponent },
      { path: 'help-center', component: HelpCenterComponent }
    ]
  }
];

export const StudentRoutes = RouterModule.forChild(routes);
