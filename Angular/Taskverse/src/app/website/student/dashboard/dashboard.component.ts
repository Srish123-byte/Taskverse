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

  constructor(private readonly session: Session) {}

  ngOnInit(): void {
    const user = this.session.user;
    this.userName = user ? `${user.firstName} ${user.lastName}` : '';
  }
}
