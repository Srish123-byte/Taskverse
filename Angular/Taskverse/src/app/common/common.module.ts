import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { PageNotFoundComponent } from './components/page-not-found/page-not-found.component';
import { HeaderComponent } from './components/header/header.component';
import { FooterComponent } from './components/footer/footer.component';
import { DashboardLoaderComponent } from './components/dashboard-loader/dashboard-loader.component';
import { DeleteActionButtonComponent } from './components/delete-action-button/delete-action-button.component';
import { LogoutConfirmationDialogComponent } from './components/logout-confirmation-dialog/logout-confirmation-dialog.component';
import { ProcessingOverlayComponent } from './components/processing-overlay/processing-overlay.component';
import { SessionInactivityWarningComponent } from './components/session-inactivity-warning/session-inactivity-warning.component';
import { QuestionBankComponent } from './components/question-bank/question-bank.component';

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
    DeleteActionButtonComponent,
    LogoutConfirmationDialogComponent,
    ProcessingOverlayComponent,
    SessionInactivityWarningComponent,
    QuestionBankComponent,
    FormatPhone,
    ToLowerCaseText,
    ToUpperCaseText
  ],
  exports: [
    PageNotFoundComponent,
    HeaderComponent,
    FooterComponent,
    DashboardLoaderComponent,
    DeleteActionButtonComponent,
    ProcessingOverlayComponent,
    SessionInactivityWarningComponent,
    QuestionBankComponent,
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
