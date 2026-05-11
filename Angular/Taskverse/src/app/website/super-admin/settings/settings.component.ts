import { Component } from '@angular/core';

@Component({
  selector: 'app-super-admin-settings',
  standalone: false,
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent {
  readonly sections = [
    {
      title: 'Platform Policies',
      description: 'Manage registration rules, activation workflows, and default approval behavior.'
    },
    {
      title: 'Security Baselines',
      description: 'Review token lifetimes, privileged-role governance, and incident escalation settings.'
    },
    {
      title: 'Operational Preferences',
      description: 'Centralize notifications, audit retention expectations, and admin-facing defaults.'
    }
  ];
}
