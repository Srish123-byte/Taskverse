import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppCommonModule } from '../../common/common.module';
import { SuperAdminRoutes } from './super-admin.routes';
import { SuperAdminShellComponent } from './super-admin-shell/super-admin-shell.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CollegesComponent } from './colleges/colleges.component';
import { UsersComponent } from './users/users.component';
import { AnalyticsComponent } from './analytics/analytics.component';
import { AssessmentsComponent } from './assessments/assessments.component';
import { ReportsComponent } from './reports/reports.component';
import { SettingsComponent } from './settings/settings.component';

@NgModule({
  declarations: [
    SuperAdminShellComponent,
    DashboardComponent,
    CollegesComponent,
    UsersComponent,
    AnalyticsComponent,
    AssessmentsComponent,
    ReportsComponent,
    SettingsComponent
  ],
  imports: [
    CommonModule,
    AppCommonModule,
    SuperAdminRoutes
  ]
})
export class SuperAdminModule {}
