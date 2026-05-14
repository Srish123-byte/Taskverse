import { Component, OnInit } from '@angular/core';
import { Session } from '../../../common/services/session/session.service';

@Component({
  selector: 'app-student-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  userName = '';
  isLoading = false;

  constructor(private readonly session: Session) {}

  ngOnInit(): void {
    this.isLoading = true;
    const user = this.session.user;
    this.userName = user ? `${user.firstName} ${user.lastName}` : '';
    window.setTimeout(() => {
      this.isLoading = false;
    }, 400);
  }
}
