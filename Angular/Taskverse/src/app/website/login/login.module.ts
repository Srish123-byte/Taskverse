import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { AppCommonModule } from '../../common/common.module';
import { LoginRoutes } from './login.routes';
import { LoginComponent } from './login/login.component';

@NgModule({
  declarations: [
    LoginComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    AppCommonModule,
    LoginRoutes
  ]
})
export class LoginModule {}
