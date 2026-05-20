import { Component } from '@angular/core';

interface ResultRow {
  assessment: string;
  date: string;
  score: string;
  rank: string;
}

@Component({
  selector: 'app-student-results',
  standalone: false,
  templateUrl: './results.component.html',
  styleUrl: './results.component.scss'
})
export class ResultsComponent {
  readonly results: ResultRow[] = [
    { assessment: 'SQL Queries Expert', date: 'Oct 18, 2023', score: '92 / 100', rank: '2nd / 42' },
    { assessment: 'Operating Systems', date: 'Oct 15, 2023', score: '88 / 100', rank: '5th / 42' },
    { assessment: 'Statistics Checkpoint', date: 'Oct 12, 2023', score: '95 / 100', rank: '1st / 42' },
    { assessment: 'Java Fundamentals', date: 'Oct 09, 2023', score: '84 / 100', rank: '7th / 42' }
  ];
}
