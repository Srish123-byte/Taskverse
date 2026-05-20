import { Component } from '@angular/core';

interface AssessmentRow {
  title: string;
  type: string;
  schedule: string;
  status: 'Upcoming' | 'Ready' | 'Completed';
}

@Component({
  selector: 'app-student-my-assessments',
  standalone: false,
  templateUrl: './my-assessments.component.html',
  styleUrl: './my-assessments.component.scss'
})
export class MyAssessmentsComponent {
  readonly assessments: AssessmentRow[] = [
    { title: 'Python Basics', type: 'Final Assessment', schedule: 'Today, 10:00 AM', status: 'Upcoming' },
    { title: 'Data Structures Midterm', type: 'Midterm Exam', schedule: 'Oct 24, 2:00 PM', status: 'Ready' },
    { title: 'Advanced Mathematics', type: 'Weekly Quiz', schedule: 'Oct 26, 9:00 AM', status: 'Ready' },
    { title: 'SQL Queries Expert', type: 'Practice Test', schedule: 'Oct 18, 9:00 AM', status: 'Completed' }
  ];
}
