import { Component } from '@angular/core';
import { AuthSessionService } from '../../../common/services/session/auth-session.service';

interface TrainerNavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-trainer-shell',
  standalone: false,
  templateUrl: './trainer-shell.component.html',
  styleUrl: './trainer-shell.component.scss'
})
export class TrainerShellComponent {
  readonly navItems: TrainerNavItem[] = [
    { label: 'Dashboard', route: 'dashboard', icon: 'dashboard' },
    { label: 'Courses', route: 'courses', icon: 'menu_book' },
    { label: 'Students', route: 'students', icon: 'groups' },
    { label: 'Manage', route: 'manage', icon: 'tune' }
  ];

  constructor(private readonly authSessionService: AuthSessionService) {}

  logout(): void {
    this.authSessionService.logout();
  }
}
