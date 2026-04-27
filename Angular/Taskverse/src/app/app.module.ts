import { inject, NgModule, provideAppInitializer } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { AppConfig, loadConfigurationSettings } from './app.config';
import { AppComponent } from './app.component';
import { AppCommonModule } from './common/common.module';
import { AppRoutes } from './app.routes';
import { MaterialModule } from './material.module';
import { ErrorInterceptor } from './common/interceptors/error.interceptor';
import { LocationStrategyService } from './common/services/utilities/location-strategy.service';

// Feature modules
import { LoginModule } from './website/login/login.module';
import { SharedPagesModule } from './website/shared/shared-pages.module';
import { SuperAdminModule } from './website/super-admin/super-admin.module';
import { CollegeAdminModule } from './website/college-admin/college-admin.module';
import { TrainerModule } from './website/trainer/trainer.module';
import { StudentModule } from './website/student/student.module';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    AppCommonModule,
    MaterialModule,
    // Feature modules — order matters: shared & login before role modules
    LoginModule,
    SharedPagesModule,
    SuperAdminModule,
    CollegeAdminModule,
    TrainerModule,
    StudentModule,
    // Root routes last so the wildcard catches anything unmatched
    AppRoutes
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ErrorInterceptor,
      multi: true
    },
    AppConfig,
    LocationStrategyService,
    provideAppInitializer(() => loadConfigurationSettings(inject(AppConfig))),
    provideHttpClient(withInterceptorsFromDi())
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}
