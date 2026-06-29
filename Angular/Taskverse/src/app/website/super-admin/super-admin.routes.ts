import { RouterModule, Routes } from '@angular/router';
import { SuperAdminShellComponent } from './super-admin-shell/super-admin-shell.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CollegesComponent } from './colleges/colleges.component';
import { UsersComponent } from './users/users.component';
import { AnalyticsComponent } from './analytics/analytics.component';
import { AssessmentsComponent } from './assessments/assessments.component';
import { SettingsComponent } from './settings/settings.component';
import { ReportsComponent } from './reports/reports.component';

const routes: Routes = [
  {
    path: '',
    component: SuperAdminShellComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'colleges', component: CollegesComponent },
      { path: 'users', component: UsersComponent },
      { path: 'analytics', component: AnalyticsComponent },
      { path: 'assessments', component: AssessmentsComponent },
      { path: 'reports', component: ReportsComponent },
      { path: 'settings', component: SettingsComponent }
    ]
  }
];

export const SuperAdminRoutes = RouterModule.forChild(routes);
