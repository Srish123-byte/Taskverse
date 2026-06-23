import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-dashboard-loader',
  standalone: false,
  templateUrl: './dashboard-loader.component.html',
  styleUrl: './dashboard-loader.component.scss'
})
export class DashboardLoaderComponent {
  @Input() title = 'Loading dashboard';
  @Input() copy = 'Preparing the latest dashboard view.';
  @Input() tone: 'light' | 'dark' = 'light';
  @Input() diameter = 46;
}
