import { Component } from '@angular/core';
import { AppConfig } from '../../../app.config';

@Component({
  selector: 'app-college-admin-help-center',
  standalone: false,
  templateUrl: './help-center.component.html',
  styleUrl: './help-center.component.scss'
})
export class HelpCenterComponent {
  readonly supportPhone: string;
  readonly supportEmail: string;

  constructor(private readonly appConfig: AppConfig) {
    this.supportPhone = this.appConfig.supportPhone;
    this.supportEmail = this.appConfig.supportEmail;
  }
}
