import { Component, HostListener, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { Session } from '../../services/session/session.service';
import { AuthSessionService } from '../../services/session/auth-session.service';
import { RoleType } from '../../enums/role-type.enum';
import { ChangePasswordDialogComponent } from '../change-password-dialog/change-password-dialog.component';

@Component({
  selector: 'app-header',
  standalone: false,
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit {
  profileMenuOpen = false;
  displayName = '';
  displayEmail = '';
  displayRole = '';
  homeRoute = '/';

  constructor(
    private router: Router,
    private session: Session,
    private authSessionService: AuthSessionService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.session.user$.subscribe(user => {
      if (user) {
        const fullName = [user.firstName, user.lastName].filter(Boolean).join(' ').trim();
        this.displayName = fullName || user.email || 'User';
        this.displayEmail = user.email || '';
      } else {
        this.displayName = this.session.userEmail || 'My Account';
        this.displayEmail = this.session.userEmail || '';
      }

      this.updateRoleDisplay(user?.role ?? this.session.role);
    });
  }

  private updateRoleDisplay(role: RoleType | null | undefined): void {
    switch (role) {
      case RoleType.SuperAdmin:
        this.displayRole = 'Super Admin';
        this.homeRoute = '/super-admin/dashboard';
        break;
      case RoleType.CollegeAdmin:
        this.displayRole = 'College Admin';
        this.homeRoute = '/college-admin/dashboard';
        break;
      case RoleType.Trainer:
        this.displayRole = 'Trainer';
        this.homeRoute = '/trainer/dashboard';
        break;
      case RoleType.Student:
        this.displayRole = 'Student';
        this.homeRoute = '/student/dashboard';
        break;
      default:
        this.displayRole = '';
        this.homeRoute = '/';
    }
  }

  get isLoginPage(): boolean {
    return this.router.url === '/login' || this.router.url === '/';
  }

  get avatarInitials(): string {
    if (!this.displayName || this.displayName === 'My Account') return 'TV';
    const parts = this.displayName.trim().split(' ');
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return this.displayName.substring(0, 2).toUpperCase();
  }

  navigateHome(): void {
    this.router.navigate([this.homeRoute]);
  }

  logout(): void {
    this.profileMenuOpen = false;
    this.authSessionService.confirmLogout();
  }

  openChangePassword(): void {
    this.profileMenuOpen = false;
    this.dialog.open(ChangePasswordDialogComponent, {
      autoFocus: false,
      restoreFocus: true,
      disableClose: false,
      panelClass: 'logout-confirmation-overlay',
      backdropClass: 'logout-confirmation-backdrop'
    });
  }

  toggleProfileMenu(): void {
    this.profileMenuOpen = !this.profileMenuOpen;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.profile-menu-wrapper') && !target.closest('.header-logo-btn')) {
      this.profileMenuOpen = false;
    }
  }
}
