import { Component, OnInit } from '@angular/core';
import { AuthSessionService } from '../../services/session/auth-session.service';

@Component({
  selector: 'app-logout',
  standalone: true,
  template: ''
})
export class LogoutComponent implements OnInit {
  constructor(private readonly authSessionService: AuthSessionService) {}

  ngOnInit(): void {
    this.authSessionService.logout();
  }
}
