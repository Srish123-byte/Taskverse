import { Component } from '@angular/core';

@Component({
  selector: 'app-super-admin-users',
  standalone: false,
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent {
  readonly sections = [
    {
      title: 'Student Oversight',
      description: 'Monitor student growth, approval status, and account health across all active colleges.',
      icon: 'school'
    },
    {
      title: 'Role Governance',
      description: 'Review privileged roles, spot account drift, and prepare escalation paths for admin changes.',
      icon: 'admin_panel_settings'
    },
    {
      title: 'Audit Visibility',
      description: 'Trace key user actions and maintain accountability across the platform lifecycle.',
      icon: 'visibility'
    }
  ];
}
