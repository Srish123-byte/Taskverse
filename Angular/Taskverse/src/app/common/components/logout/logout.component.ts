import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { RouteAddress } from '../../constants/routes.constants';
import { AuthSessionService } from '../../services/session/auth-session.service';

@Component({
  selector: 'app-logout',
  standalone: true,
  template: ''
})
export class LogoutComponent implements OnInit {
  constructor(
    private readonly authSessionService: AuthSessionService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.authSessionService.confirmLogout(() => {
      void this.router.navigateByUrl(`/${RouteAddress.RoleDirector}`);
    });
  }
}
