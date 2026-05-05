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

// Feature modules (eagerly loaded — needed before authentication)
import { LoginModule } from './website/login/login.module';
import { SharedPagesModule } from './website/shared/shared-pages.module';
// Role modules are lazy-loaded via loadChildren in app.routes.ts

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    AppCommonModule,
    MaterialModule,
    // Eagerly loaded feature modules (pre-auth pages)
    LoginModule,
    SharedPagesModule,
    // Root routes last — includes lazy-loaded role modules & wildcard 404
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
