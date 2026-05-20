import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { PageNotFoundComponent } from './components/page-not-found/page-not-found.component';
import { HeaderComponent } from './components/header/header.component';
import { FooterComponent } from './components/footer/footer.component';
import { DashboardLoaderComponent } from './components/dashboard-loader/dashboard-loader.component';

import { FormatPhone } from './pipes/format-phone.pipe';
import { ToLowerCaseText } from './pipes/to-lower-case-text.pipe';
import { ToUpperCaseText } from './pipes/to-upper-case-text.pipe';

import { LogService } from './services/api/log.service';
import { MaterialModule } from '../material.module';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    ReactiveFormsModule,
    MaterialModule
  ],
  declarations: [
    PageNotFoundComponent,
    HeaderComponent,
    FooterComponent,
    DashboardLoaderComponent,
    FormatPhone,
    ToLowerCaseText,
    ToUpperCaseText
  ],
  exports: [
    PageNotFoundComponent,
    HeaderComponent,
    FooterComponent,
    DashboardLoaderComponent,
    FormatPhone,
    ToLowerCaseText,
    ToUpperCaseText,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MaterialModule
  ],
  providers: [
    LogService
  ]
})
export class AppCommonModule {}
