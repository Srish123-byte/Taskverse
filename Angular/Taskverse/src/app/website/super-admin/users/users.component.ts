import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { PendingUser } from '../../../common/models/super-admin.model';
import { SuperAdminService } from '../../../common/services/api/super-admin.service';

@Component({
  selector: 'app-super-admin-users',
  standalone: false,
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit, OnDestroy {
  pendingUsers: PendingUser[] = [];
  selectedRole = 'all';
  selectedInstitution = 'all';
  searchTerm = '';
  isLoading = false;
  activeUserId: string | null = null;
  errorMessage = '';
  actionMessage = '';
  private routeSubscription?: Subscription;

  constructor(
    private readonly superAdminService: SuperAdminService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.routeSubscription = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => {
        if (event.urlAfterRedirects.endsWith('/super-admin/users') && (this.pendingUsers.length === 0 || this.errorMessage)) {
          this.loadPendingUsers();
        }
      });

    if (this.router.url.endsWith('/super-admin/users')) {
      this.loadPendingUsers();
    }
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  trackByUserId(_: number, user: PendingUser): string {
    return user.userId;
  }

  getRoleLabel(role: string): string {
    return role.replace(/([a-z])([A-Z])/g, '$1 $2').toUpperCase();
  }

  getRoleClass(role: string): string {
    return role
      .replace(/([a-z])([A-Z])/g, '$1-$2')
      .replace(/\s+/g, '-')
      .toLowerCase();
  }

  get filteredUsers(): PendingUser[] {
    return this.pendingUsers.filter(user => {
      const matchesRole = this.selectedRole === 'all' || user.role === this.selectedRole;
      const institutionName = user.institutionName?.trim() || 'Global System Access';
      const matchesInstitution =
        this.selectedInstitution === 'all' || institutionName === this.selectedInstitution;
      const normalizedSearch = this.searchTerm.trim().toLowerCase();
      const matchesSearch =
        normalizedSearch.length === 0 ||
        user.fullName.toLowerCase().includes(normalizedSearch) ||
        user.email.toLowerCase().includes(normalizedSearch);

      return matchesRole && matchesInstitution && matchesSearch;
    });
  }

  get availableRoles(): string[] {
    return [...new Set(this.pendingUsers.map(user => user.role))].sort((left, right) => left.localeCompare(right));
  }

  get availableInstitutions(): string[] {
    return [...new Set(this.pendingUsers.map(user => user.institutionName?.trim() || 'Global System Access'))]
      .sort((left, right) => left.localeCompare(right));
  }

  resetFilters(): void {
    this.selectedRole = 'all';
    this.selectedInstitution = 'all';
    this.searchTerm = '';
  }

  isActingOn(userId: string): boolean {
    return this.activeUserId === userId;
  }

  approveUser(user: PendingUser): void {
    if (this.activeUserId) {
      return;
    }

    this.activeUserId = user.userId;
    this.errorMessage = '';
    this.actionMessage = '';

    this.superAdminService.approveUser(user.userId).subscribe({
      next: () => {
        this.pendingUsers = this.pendingUsers.filter(item => item.userId !== user.userId);
        this.actionMessage = `${user.fullName} has been approved.`;
        this.activeUserId = null;
        this.changeDetectorRef.detectChanges();
      },
      error: err => {
        this.errorMessage =
          err?.error?.detail ||
          err?.error?.message ||
          'Unable to approve this user right now.';
        this.activeUserId = null;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  rejectUser(user: PendingUser): void {
    if (this.activeUserId) {
      return;
    }

    this.activeUserId = user.userId;
    this.errorMessage = '';
    this.actionMessage = '';

    this.superAdminService.rejectUser(user.userId).subscribe({
      next: () => {
        this.pendingUsers = this.pendingUsers.filter(item => item.userId !== user.userId);
        this.actionMessage = `${user.fullName} has been rejected.`;
        this.activeUserId = null;
        this.changeDetectorRef.detectChanges();
      },
      error: err => {
        this.errorMessage =
          err?.error?.detail ||
          err?.error?.message ||
          'Unable to reject this user right now.';
        this.activeUserId = null;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private loadPendingUsers(): void {
    if (this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.actionMessage = '';

    this.superAdminService.getPendingUsers().subscribe({
      next: users => {
        this.pendingUsers = users;
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Unable to load pending users right now.';
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }
}
