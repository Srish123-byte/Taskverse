import { Component } from '@angular/core';

@Component({
  selector: 'app-super-admin-assessments',
  standalone: false,
  templateUrl: './assessments.component.html',
  styleUrl: './assessments.component.scss'
})
export class AssessmentsComponent {
  readonly spotlight = [
    { title: 'Assessment Catalog', body: 'Review platform-wide assessment definitions, ownership, and rollout readiness.' },
    { title: 'Completion Monitoring', body: 'Track active sessions, bottlenecks, and drop-off trends across colleges.' },
    { title: 'Policy Controls', body: 'Define approval, archival, and visibility rules before publishing new assessments.' }
  ];
}
