import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppCommonModule } from '../../common/common.module';
import { SuperAdminRoutes } from './super-admin.routes';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CollegesComponent } from './colleges/colleges.component';
import { TrainersComponent } from './trainers/trainers.component';
import { UsersComponent } from './users/users.component';
import { ManageComponent } from './manage/manage.component';

@NgModule({
  declarations: [
    DashboardComponent,
    CollegesComponent,
    TrainersComponent,
    UsersComponent,
    ManageComponent
  ],
  imports: [
    CommonModule,
    AppCommonModule,
    SuperAdminRoutes
  ]
})
export class SuperAdminModule {}
