import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppCommonModule } from '../../common/common.module';
import { SharedPagesRoutes } from './shared-pages.routes';
import { RoleDirectorComponent } from './role-director/role-director.component';
import { UnhandledErrorComponent } from './unhandled-error/unhandled-error.component';
import { SessionTimeoutComponent } from './session-timeout/session-timeout.component';
import { ApprovalStatusComponent } from './approval-status/approval-status.component';
import { ChangeTemporaryPasswordComponent } from './change-temporary-password/change-temporary-password.component';
import { ReactiveFormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    RoleDirectorComponent,
    UnhandledErrorComponent,
    SessionTimeoutComponent,
    ApprovalStatusComponent,
    ChangeTemporaryPasswordComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    AppCommonModule,
    SharedPagesRoutes
  ]
})
export class SharedPagesModule {}
